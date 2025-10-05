# SqlSyncService Installation Script
# Run as Administrator

param(
    [string]$ServicePath = "C:\Program Files\SqlSyncService\SqlSyncService.exe",
    [string]$ConfigDir = "C:\ProgramData\SqlSyncService",
    [string]$DbServer = "localhost",
    [int]$DbPort = 1433,
    [string]$DbName = "",
    [string]$DbUser = "",
    [string]$DbPassword = "",
    [string]$CertPath = "",
    [string]$CertPassword = "",
    [string]$ApiKey = "",
    [string[]]$IpAllowList = @(),
    [string]$AdminPassphrase = ""
)

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

Write-Host "SqlSyncService Installation" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green

# Create directories
Write-Host "`nCreating directories..."
$directories = @(
    $ConfigDir,
    "$ConfigDir\certs",
    "$ConfigDir\logs"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# Generate API key if not provided
if ([string]::IsNullOrEmpty($ApiKey)) {
    Write-Host "`nGenerating API key..."
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $ApiKey = [Convert]::ToBase64String($bytes)
    Write-Host "  Generated API Key: $ApiKey" -ForegroundColor Yellow
    Write-Host "  SAVE THIS KEY SECURELY!" -ForegroundColor Red
}

# Generate admin passphrase if not provided
if ([string]::IsNullOrEmpty($AdminPassphrase)) {
    Write-Host "`nGenerating admin passphrase..."
    $bytes = New-Object byte[] 16
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $AdminPassphrase = [Convert]::ToBase64String($bytes)
    Write-Host "  Generated Passphrase: $AdminPassphrase" -ForegroundColor Yellow
    Write-Host "  SAVE THIS PASSPHRASE SECURELY!" -ForegroundColor Red
}

# Encrypt secrets using DPAPI
Write-Host "`nEncrypting secrets..."
$encryptApiKey = Protect-CmsMessage -To "CN=LocalMachine" -Content $ApiKey
$encryptDbUser = Protect-CmsMessage -To "CN=LocalMachine" -Content $DbUser
$encryptDbPassword = Protect-CmsMessage -To "CN=LocalMachine" -Content $DbPassword
$encryptCertPassword = Protect-CmsMessage -To "CN=LocalMachine" -Content $CertPassword
$encryptAdminPassphrase = Protect-CmsMessage -To "CN=LocalMachine" -Content $AdminPassphrase

# Create configuration files
Write-Host "`nCreating configuration files..."

$appSettings = @{
    Service = @{
        ListenUrl = "https://0.0.0.0:8443"
    }
    Security = @{
        RequireApiKey = $true
        IpAllowList = $IpAllowList
        EnableHttps = $true
        Certificate = @{
            Path = $CertPath
            PasswordEncrypted = $encryptCertPassword
        }
        ApiKeyEncrypted = $encryptApiKey
    }
    Database = @{
        Server = $DbServer
        Instance = ""
        Port = $DbPort
        Database = $DbName
        UsernameEncrypted = $encryptDbUser
        PasswordEncrypted = $encryptDbPassword
        CommandTimeoutSeconds = 60
    }
    Logging = @{
        Level = "Information"
        Directory = "$ConfigDir\logs"
    }
    Admin = @{
        ListenUrl = "https://localhost:9443"
        PassphraseEncrypted = $encryptAdminPassphrase
    }
}

$appSettingsJson = $appSettings | ConvertTo-Json -Depth 10
Set-Content -Path "$ConfigDir\appsettings.json" -Value $appSettingsJson
Write-Host "  Created appsettings.json" -ForegroundColor Gray

# Copy sample query and mapping files if they don't exist
if (-not (Test-Path "$ConfigDir\queries.json")) {
    Copy-Item -Path ".\config-samples\queries.json" -Destination "$ConfigDir\queries.json"
    Write-Host "  Created queries.json" -ForegroundColor Gray
}

if (-not (Test-Path "$ConfigDir\mapping.json")) {
    Copy-Item -Path ".\config-samples\mapping.json" -Destination "$ConfigDir\mapping.json"
    Write-Host "  Created mapping.json" -ForegroundColor Gray
}

# Configure firewall
Write-Host "`nConfiguring firewall..."
$firewallRule = Get-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound" -ErrorAction SilentlyContinue
if ($null -eq $firewallRule) {
    New-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound" -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow | Out-Null
    Write-Host "  Firewall rule created" -ForegroundColor Gray
} else {
    Write-Host "  Firewall rule already exists" -ForegroundColor Gray
}

# Install service
Write-Host "`nInstalling service..."
$service = Get-Service -Name "SqlSyncService" -ErrorAction SilentlyContinue
if ($null -ne $service) {
    Write-Host "  Service already exists, stopping..." -ForegroundColor Yellow
    Stop-Service -Name "SqlSyncService" -Force
    sc.exe delete SqlSyncService | Out-Null
    Start-Sleep -Seconds 2
}

sc.exe create SqlSyncService binPath= $ServicePath start= auto | Out-Null
sc.exe description SqlSyncService "Provides secure HTTPS API access to SQL Server data" | Out-Null
Write-Host "  Service installed" -ForegroundColor Gray

# Start service
Write-Host "`nStarting service..."
Start-Service -Name "SqlSyncService"
$serviceStatus = Get-Service -Name "SqlSyncService"

if ($serviceStatus.Status -eq "Running") {
    Write-Host "  Service started successfully" -ForegroundColor Green
} else {
    Write-Host "  Failed to start service. Check logs at $ConfigDir\logs" -ForegroundColor Red
}

Write-Host "`nInstallation complete!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "  1. Access Admin UI: https://localhost:9443/admin"
Write-Host "  2. Use admin passphrase: $AdminPassphrase"
Write-Host "  3. Configure queries and mappings"
Write-Host "  4. Test API: https://your-server:8443/health"
Write-Host "`nAPI Key for external calls: $ApiKey" -ForegroundColor Yellow
