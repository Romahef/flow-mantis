# Uninstall SqlSyncService (keeps configuration)
# Run as Administrator

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "SqlSyncService - Uninstall" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "WARNING: Configuration files will be PRESERVED" -ForegroundColor Yellow
Write-Host "Location: C:\ProgramData\SqlSyncService\" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Continue with uninstall? (Y/N)"
if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Uninstall cancelled." -ForegroundColor Yellow
    exit
}

Write-Host ""

# Stop service
Write-Host "[1/5] Stopping service..." -ForegroundColor Yellow
try {
    Stop-Service SqlSyncService -ErrorAction Stop
    Write-Host "  ✓ Service stopped" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ Service not running" -ForegroundColor Yellow
}
Start-Sleep -Seconds 2

# Delete service
Write-Host "[2/5] Removing service..." -ForegroundColor Yellow
try {
    sc.exe delete SqlSyncService | Out-Null
    Write-Host "  ✓ Service removed" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ Service not found" -ForegroundColor Yellow
}
Start-Sleep -Seconds 2

# Stop Admin app
Write-Host "[3/5] Stopping Admin app..." -ForegroundColor Yellow
Get-Process -Name "SqlSyncService.Admin" -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host "  ✓ Admin app stopped" -ForegroundColor Green

# Remove firewall rule
Write-Host "[4/5] Removing firewall rules..." -ForegroundColor Yellow
netsh advfirewall firewall delete rule name="SqlSyncService API" 2>$null | Out-Null
netsh advfirewall firewall delete rule name="SqlSyncService HTTPS" 2>$null | Out-Null
Write-Host "  ✓ Firewall rules removed" -ForegroundColor Green

# Remove program files (but keep config)
Write-Host "[5/5] Removing program files..." -ForegroundColor Yellow
if (Test-Path "C:\Program Files\SqlSyncService") {
    Remove-Item "C:\Program Files\SqlSyncService" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Program files removed" -ForegroundColor Green
} else {
    Write-Host "  ⚠ Program files not found" -ForegroundColor Yellow
}

# Remove shortcuts
Write-Host "[6/6] Removing shortcuts..." -ForegroundColor Yellow
$desktop = [Environment]::GetFolderPath("Desktop")
$startMenu = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\SqlSyncService"

if (Test-Path "$desktop\SqlSyncService Admin.lnk") { Remove-Item "$desktop\SqlSyncService Admin.lnk" -Force }
if (Test-Path "$desktop\SqlSyncService Admin.bat") { Remove-Item "$desktop\SqlSyncService Admin.bat" -Force }
if (Test-Path $startMenu) { Remove-Item $startMenu -Recurse -Force }
Write-Host "  ✓ Shortcuts removed" -ForegroundColor Green

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "✓ UNINSTALL COMPLETE" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Configuration preserved at:" -ForegroundColor Cyan
Write-Host "  C:\ProgramData\SqlSyncService\" -ForegroundColor White
Write-Host ""
Write-Host "You can now run the new installer." -ForegroundColor Yellow
Write-Host ""

