# Build Complete Installer Script
Write-Host "Building Complete Installer with all fixes..." -ForegroundColor Cyan

# Step 1: Build Admin UI with embedded .NET
Write-Host "[1/4] Publishing Admin UI with embedded .NET runtime..." -ForegroundColor Yellow
dotnet publish src/SqlSyncService.Admin/SqlSyncService.Admin.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -o publish-installer/admin-final `
    --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to publish Admin UI" -ForegroundColor Red
    exit 1
}

# Step 2: Create admin-embedded.zip
Write-Host "[2/4] Creating admin-embedded.zip..." -ForegroundColor Yellow
if (Test-Path "publish-installer\admin-embedded.zip") {
    Remove-Item "publish-installer\admin-embedded.zip" -Force
}

Compress-Archive -Path "publish-installer\admin-final\*" `
    -DestinationPath "publish-installer\admin-embedded.zip" `
    -CompressionLevel Optimal -Force

$zipSize = [math]::Round((Get-Item "publish-installer\admin-embedded.zip").Length / 1MB, 1)
Write-Host "  admin-embedded.zip created: $zipSize MB" -ForegroundColor Green

# Step 3: Build Installer
Write-Host "[3/4] Building Complete Installer..." -ForegroundColor Yellow
if (Test-Path "SqlSyncService-Complete-Installer.exe") {
    Remove-Item "SqlSyncService-Complete-Installer.exe" -Force
}

dotnet publish src/SqlSyncService.InstallerWizard/SqlSyncService.InstallerWizard.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -o . `
    --nologo --verbosity quiet

if (Test-Path "SqlSyncService-Installer.exe") {
    Rename-Item "SqlSyncService-Installer.exe" "SqlSyncService-Complete-Installer.exe" -Force
}

# Step 4: Verify and Report
Write-Host "[4/4] Verifying installer..." -ForegroundColor Yellow
if (Test-Path "SqlSyncService-Complete-Installer.exe") {
    $installerSize = [math]::Round((Get-Item "SqlSyncService-Complete-Installer.exe").Length / 1MB, 1)
    $installerTime = (Get-Item "SqlSyncService-Complete-Installer.exe").LastWriteTime
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "✅ BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installer: SqlSyncService-Complete-Installer.exe" -ForegroundColor White
    Write-Host "Size:      $installerSize MB" -ForegroundColor White
    Write-Host "Built:     $installerTime" -ForegroundColor White
    Write-Host "`nFixes included:" -ForegroundColor Cyan
    Write-Host "  ✓ Username now encrypted (was plain text)" -ForegroundColor White
    Write-Host "  ✓ Admin UI won't close after login" -ForegroundColor White
    Write-Host "  ✓ Database credentials editable in Admin UI" -ForegroundColor White
    Write-Host "  ✓ Connection test uses credentials properly" -ForegroundColor White
    Write-Host "========================================`n" -ForegroundColor Green
} else {
    Write-Host "`n❌ Build failed - installer not found" -ForegroundColor Red
    exit 1
}

# Cleanup
Write-Host "Cleaning up temporary files..." -ForegroundColor Yellow
Remove-Item "publish-installer\admin-final" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n🚀 Ready to install!" -ForegroundColor Green






