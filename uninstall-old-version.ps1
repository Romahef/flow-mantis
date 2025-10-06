# Uninstall SqlSyncService - Previous Installation
# Run as Administrator

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SqlSyncService Uninstaller" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 1: Stop the service
Write-Host "[1/5] Stopping service..." -ForegroundColor Yellow
$service = Get-Service -Name SqlSyncService -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -eq 'Running') {
        Stop-Service -Name SqlSyncService -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "  Service stopped" -ForegroundColor Green
    } else {
        Write-Host "  Service not running" -ForegroundColor Gray
    }
} else {
    Write-Host "  Service not found" -ForegroundColor Gray
}

# Step 2: Delete the service
Write-Host "[2/5] Removing service..." -ForegroundColor Yellow
sc.exe delete SqlSyncService 2>&1 | Out-Null
Start-Sleep -Seconds 2
$service = Get-Service -Name SqlSyncService -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "  Service removed successfully" -ForegroundColor Green
} else {
    Write-Host "  Warning: Service still exists" -ForegroundColor Yellow
}

# Step 3: Remove firewall rule
Write-Host "[3/5] Removing firewall rule..." -ForegroundColor Yellow
netsh advfirewall firewall delete rule name="SqlSyncService HTTPS" 2>&1 | Out-Null
Write-Host "  Firewall rule removed" -ForegroundColor Green

# Step 4: Remove shortcuts
Write-Host "[4/5] Removing shortcuts..." -ForegroundColor Yellow
$desktopPath = [Environment]::GetFolderPath("Desktop")
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\SqlSyncService"

# Desktop shortcuts
$shortcuts = @(
    "$desktopPath\SqlSyncService Admin.lnk",
    "$desktopPath\SqlSyncService Admin.bat"
)

foreach ($shortcut in $shortcuts) {
    if (Test-Path $shortcut) {
        Remove-Item $shortcut -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed: $(Split-Path $shortcut -Leaf)" -ForegroundColor Gray
    }
}

# Start Menu
if (Test-Path $startMenuPath) {
    Remove-Item $startMenuPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Removed Start Menu folder" -ForegroundColor Gray
}

Write-Host "  Shortcuts removed" -ForegroundColor Green

# Step 5: Ask about removing files
Write-Host "[5/5] Removing files..." -ForegroundColor Yellow
Write-Host ""
Write-Host "  Do you want to remove the installed files?" -ForegroundColor Cyan
Write-Host "  Service: C:\Program Files\SqlSyncService" -ForegroundColor Gray
Write-Host "  Config:  C:\ProgramData\SqlSyncService" -ForegroundColor Gray
Write-Host ""
$removeFiles = Read-Host "  Remove files? (Y/N)"

if ($removeFiles -eq 'Y' -or $removeFiles -eq 'y') {
    # Remove service files
    if (Test-Path "C:\Program Files\SqlSyncService") {
        Remove-Item "C:\Program Files\SqlSyncService" -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "  Removed service files" -ForegroundColor Green
    }
    
    # Ask about config files
    Write-Host ""
    $removeConfig = Read-Host "  Remove configuration files too? (Y/N)"
    if ($removeConfig -eq 'Y' -or $removeConfig -eq 'y') {
        if (Test-Path "C:\ProgramData\SqlSyncService") {
            Remove-Item "C:\ProgramData\SqlSyncService" -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  Removed configuration files" -ForegroundColor Green
        }
    } else {
        Write-Host "  Configuration files kept (can reuse for new installation)" -ForegroundColor Yellow
    }
} else {
    Write-Host "  Files kept" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Uninstallation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "What was removed:" -ForegroundColor Cyan
Write-Host "  [x] Windows Service (SqlSyncService)" -ForegroundColor Gray
Write-Host "  [x] Firewall rule" -ForegroundColor Gray
Write-Host "  [x] Desktop shortcuts" -ForegroundColor Gray
Write-Host "  [x] Start Menu shortcuts" -ForegroundColor Gray
if ($removeFiles -eq 'Y' -or $removeFiles -eq 'y') {
    Write-Host "  [x] Service files" -ForegroundColor Gray
    if ($removeConfig -eq 'Y' -or $removeConfig -eq 'y') {
        Write-Host "  [x] Configuration files" -ForegroundColor Gray
    }
}
Write-Host ""
Write-Host "You can now install the new version!" -ForegroundColor Green
Write-Host "Run: SqlSyncService-Complete-Installer.exe" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"

