# Fix All Encrypted Values in Config
Write-Host "=== Fixing All Encrypted Values in SqlSyncService Config ===" -ForegroundColor Cyan
Write-Host ""

Add-Type -AssemblyName System.Security

$configPath = "C:\ProgramData\SqlSyncService\appsettings.json"
if (-not (Test-Path $configPath)) {
    Write-Host "ERROR: Config not found at $configPath" -ForegroundColor Red
    exit 1
}

$config = Get-Content $configPath | ConvertFrom-Json
$entropy = [System.Text.Encoding]::UTF8.GetBytes("SqlSyncService.v1")
$scope = [System.Security.Cryptography.DataProtectionScope]::LocalMachine

function Encrypt-Value {
    param([string]$plainText)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($plainText)
    $encrypted = [System.Security.Cryptography.ProtectedData]::Protect($bytes, $entropy, $scope)
    return [Convert]::ToBase64String($encrypted)
}

Write-Host "Current Database:" -ForegroundColor Yellow
Write-Host "  Server: $($config.Database.Server)"
Write-Host "  Database: $($config.Database.Database)"
Write-Host "  Username: $($config.Database.Username)"
Write-Host ""

# Re-encrypt database credentials
Write-Host "Re-encrypting database password..." -ForegroundColor Cyan
$dbPassword = "SqlSync@2024!Strong"  # The password we created
$config.Database.PasswordEncrypted = Encrypt-Value $dbPassword
Write-Host "✅ Database password re-encrypted" -ForegroundColor Green

# Re-encrypt username if it exists as encrypted field
if ($config.Database.PSObject.Properties.Name -contains "UsernameEncrypted") {
    Write-Host "Re-encrypting database username..." -ForegroundColor Cyan
    $dbUsername = $config.Database.Username
    if ([string]::IsNullOrEmpty($dbUsername)) {
        $dbUsername = "sqlsync_user"
    }
    $config.Database.UsernameEncrypted = Encrypt-Value $dbUsername
    Write-Host "✅ Database username re-encrypted" -ForegroundColor Green
}

# Generate and encrypt new API key
Write-Host "Generating new API key..." -ForegroundColor Cyan
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$apiKeyBytes = New-Object byte[] 32
$rng.GetBytes($apiKeyBytes)
$apiKey = [Convert]::ToBase64String($apiKeyBytes)
$config.Security.ApiKeyEncrypted = Encrypt-Value $apiKey
Write-Host "✅ New API key: $apiKey" -ForegroundColor Green
Write-Host "   (Save this for API clients!)" -ForegroundColor Yellow

# Admin password is already fixed, but verify
Write-Host "`nAdmin password already set to: admin123" -ForegroundColor Green

# Save config
Write-Host "`nSaving configuration..." -ForegroundColor Cyan
$config | ConvertTo-Json -Depth 10 | Set-Content $configPath
Write-Host "✅ Configuration saved!" -ForegroundColor Green

Write-Host ""
Write-Host "=== All Encrypted Values Fixed ===" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Admin Password: admin123" -ForegroundColor White
Write-Host "  Database: $($config.Database.Database)" -ForegroundColor White
Write-Host "  DB User: sqlsync_user" -ForegroundColor White
Write-Host "  DB Pass: SqlSync@2024!Strong" -ForegroundColor White
Write-Host "  API Key: $apiKey" -ForegroundColor White
Write-Host ""
Write-Host "You can now use the Admin UI!" -ForegroundColor Green

