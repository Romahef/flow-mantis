# Get and decrypt the API key
Write-Host "Reading encrypted API key..." -ForegroundColor Cyan

try {
    # Read the config file
    $configPath = "C:\ProgramData\SqlSyncService\appsettings.json"
    $config = Get-Content $configPath | ConvertFrom-Json
    
    # Get the encrypted API key
    $encryptedKey = $config.Security.ApiKeyEncrypted
    
    # Decrypt using DPAPI (same method the app uses)
    Add-Type -AssemblyName System.Security
    $encryptedBytes = [Convert]::FromBase64String($encryptedKey)
    $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $encryptedBytes,
        $null,
        [System.Security.Cryptography.DataProtectionScope]::LocalMachine
    )
    $apiKey = [System.Text.Encoding]::UTF8.GetString($decryptedBytes)
    
    Write-Host "`n================================" -ForegroundColor Green
    Write-Host "YOUR API KEY:" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Green
    Write-Host $apiKey -ForegroundColor Yellow
    Write-Host "================================" -ForegroundColor Green
    Write-Host "`nCopy this key and save it securely!" -ForegroundColor Cyan
    Write-Host "`nPress any key to exit..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`nNote: This script must be run with the same user account that installed the service." -ForegroundColor Yellow
}




