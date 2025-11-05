# Reset Admin Password (Fixed for LocalMachine scope with Entropy)
param(
    [Parameter(Mandatory=$false)]
    [string]$NewPassword
)

Write-Host "=== Reset SqlSyncService Admin Password ===" -ForegroundColor Cyan
Write-Host ""

# Load required assembly
Add-Type -AssemblyName System.Security

# Read config
$configPath = "C:\ProgramData\SqlSyncService\appsettings.json"
if (-not (Test-Path $configPath)) {
    Write-Host "ERROR: Config file not found at $configPath" -ForegroundColor Red
    exit 1
}

# Get new password if not provided
if ([string]::IsNullOrEmpty($NewPassword)) {
    $NewPassword = Read-Host "Enter new admin password"
}

if ([string]::IsNullOrEmpty($NewPassword)) {
    Write-Host "ERROR: Password cannot be empty" -ForegroundColor Red
    exit 1
}

Write-Host "Setting new password: $NewPassword" -ForegroundColor Yellow
Write-Host ""

# Hash the password (SHA256)
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$bytes = [System.Text.Encoding]::UTF8.GetBytes($NewPassword)
$hash = $sha256.ComputeHash($bytes)
$passwordHash = [Convert]::ToBase64String($hash)

Write-Host "Password hash: $passwordHash" -ForegroundColor Gray
Write-Host ""

# Encrypt the hash (DPAPI - LocalMachine scope with Entropy, matching SecretsProtector)
$entropy = [System.Text.Encoding]::UTF8.GetBytes("SqlSyncService.v1")
$encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
    [System.Text.Encoding]::UTF8.GetBytes($passwordHash),
    $entropy,
    [System.Security.Cryptography.DataProtectionScope]::LocalMachine
)
$encryptedHash = [Convert]::ToBase64String($encryptedBytes)

Write-Host "Encrypted: $($encryptedHash.Substring(0, 50))..." -ForegroundColor Gray
Write-Host ""

# Update config
try {
    $config = Get-Content $configPath | ConvertFrom-Json
    $config.Admin.PassphraseEncrypted = $encryptedHash
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
    
    Write-Host "✅ Password updated successfully in config!" -ForegroundColor Green
    Write-Host ""
    Write-Host "New admin password: $NewPassword" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "You can now login to the Admin UI with this password." -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to update config: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Reset Complete ===" -ForegroundColor Cyan