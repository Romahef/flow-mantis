# Restart SQL Server to enable Mixed Mode Authentication
# Run this as Administrator

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Restarting SQL Server (SQLEXPRESS)" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator"
    Write-Host "`nRight-click this file and select 'Run as Administrator'`n" -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host "[1/3] Stopping SQL Server..." -ForegroundColor Yellow
Stop-Service -Name "MSSQL`$SQLEXPRESS" -Force
Start-Sleep -Seconds 3

Write-Host "[2/3] Starting SQL Server..." -ForegroundColor Yellow
Start-Service -Name "MSSQL`$SQLEXPRESS"
Start-Sleep -Seconds 3

Write-Host "[3/3] Verifying service status..." -ForegroundColor Yellow
$service = Get-Service -Name "MSSQL`$SQLEXPRESS"

if ($service.Status -eq "Running") {
    Write-Host "`n✓ SQL Server restarted successfully!" -ForegroundColor Green
    Write-Host "✓ Mixed Mode Authentication is now enabled" -ForegroundColor Green
    Write-Host "`nYou can now use SQL Server logins:" -ForegroundColor Cyan
    Write-Host "  Username: sqlsync_user" -ForegroundColor White
    Write-Host "  Password: SqlSync@2024!Strong" -ForegroundColor White
    Write-Host "`nPress any key to test the connection...`n"
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    # Test connection
    Write-Host "Testing connection..." -ForegroundColor Cyan
    $result = sqlcmd -S "localhost\SQLEXPRESS" -d "SqlSyncTest" -U "sqlsync_user" -P "SqlSync@2024!Strong" -Q "SELECT 'Connection Successful!' AS Result" -W -h -1 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ Connection test PASSED!" -ForegroundColor Green
        Write-Host "You can now proceed with the installer!`n" -ForegroundColor Green
    } else {
        Write-Host "`n✗ Connection test FAILED" -ForegroundColor Red
        Write-Host "Error: $result`n" -ForegroundColor Red
    }
} else {
    Write-Host "`n✗ Failed to start SQL Server" -ForegroundColor Red
    Write-Host "Status: $($service.Status)`n" -ForegroundColor Red
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
