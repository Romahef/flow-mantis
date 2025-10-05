# SqlSyncService Uninstallation Script
# Run as Administrator

param(
    [switch]$RemoveConfig = $false
)

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

Write-Host "SqlSyncService Uninstallation" -ForegroundColor Red
Write-Host "==============================" -ForegroundColor Red

# Stop service
Write-Host "`nStopping service..."
$service = Get-Service -Name "SqlSyncService" -ErrorAction SilentlyContinue
if ($null -ne $service) {
    Stop-Service -Name "SqlSyncService" -Force -ErrorAction SilentlyContinue
    Write-Host "  Service stopped" -ForegroundColor Gray
}

# Uninstall service
Write-Host "`nUninstalling service..."
if ($null -ne $service) {
    sc.exe delete SqlSyncService | Out-Null
    Write-Host "  Service uninstalled" -ForegroundColor Gray
}

# Remove firewall rule
Write-Host "`nRemoving firewall rule..."
$firewallRule = Get-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound" -ErrorAction SilentlyContinue
if ($null -ne $firewallRule) {
    Remove-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound"
    Write-Host "  Firewall rule removed" -ForegroundColor Gray
}

# Remove configuration (optional)
if ($RemoveConfig) {
    Write-Host "`nRemoving configuration..."
    $configDir = "C:\ProgramData\SqlSyncService"
    if (Test-Path $configDir) {
        Remove-Item -Path $configDir -Recurse -Force
        Write-Host "  Configuration removed" -ForegroundColor Gray
    }
}

Write-Host "`nUninstallation complete!" -ForegroundColor Green
if (-not $RemoveConfig) {
    Write-Host "Configuration preserved at C:\ProgramData\SqlSyncService" -ForegroundColor Yellow
    Write-Host "Use -RemoveConfig flag to remove configuration" -ForegroundColor Yellow
}
