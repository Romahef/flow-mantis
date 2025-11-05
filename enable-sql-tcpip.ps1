# Enable TCP/IP for SQL Server Express
Write-Host "=== Enabling TCP/IP for SQL Server Express ===" -ForegroundColor Cyan
Write-Host ""

# Load SQL Server WMI provider
$wmi = new-object ('Microsoft.SqlServer.Management.Smo.Wmi.ManagedComputer')

# Get the instance
$instance = "SQLEXPRESS"
Write-Host "Configuring SQL Server instance: $instance" -ForegroundColor Yellow

# Enable TCP/IP
$uri = "ManagedComputer[@Name='$env:COMPUTERNAME']/ServerInstance[@Name='$instance']/ServerProtocol[@Name='Tcp']"
$Tcp = $wmi.GetSmoObject($uri)

if ($Tcp.IsEnabled -eq $false) {
    Write-Host "Enabling TCP/IP..." -ForegroundColor Cyan
    $Tcp.IsEnabled = $true
    $Tcp.Alter()
    Write-Host "✅ TCP/IP enabled!" -ForegroundColor Green
} else {
    Write-Host "✅ TCP/IP already enabled" -ForegroundColor Green
}

# Enable Named Pipes (as backup)
$uri = "ManagedComputer[@Name='$env:COMPUTERNAME']/ServerInstance[@Name='$instance']/ServerProtocol[@Name='Np']"
$Np = $wmi.GetSmoObject($uri)

if ($Np.IsEnabled -eq $false) {
    Write-Host "Enabling Named Pipes..." -ForegroundColor Cyan
    $Np.IsEnabled = $true
    $Np.Alter()
    Write-Host "✅ Named Pipes enabled!" -ForegroundColor Green
} else {
    Write-Host "✅ Named Pipes already enabled" -ForegroundColor Green
}

# Restart SQL Server
Write-Host "`nRestarting SQL Server..." -ForegroundColor Yellow
try {
    Restart-Service "MSSQL`$$instance" -Force
    Write-Host "✅ SQL Server restarted!" -ForegroundColor Green
    
    # Wait a moment for service to fully start
    Start-Sleep -Seconds 3
    
    # Check service status
    $svc = Get-Service "MSSQL`$$instance"
    if ($svc.Status -eq "Running") {
        Write-Host "✅ SQL Server is running" -ForegroundColor Green
    } else {
        Write-Host "⚠️ SQL Server status: $($svc.Status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Failed to restart SQL Server: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Please restart manually: Restart-Service MSSQL`$SQLEXPRESS" -ForegroundColor Yellow
}

Write-Host "`n=== Configuration Complete ===" -ForegroundColor Green
Write-Host "SQL Server should now accept TCP/IP and Named Pipes connections" -ForegroundColor Cyan






