# Complete Rebuild and Reinstall Script
# This includes ALL fixes: Admin app bug fix, Postman collection, API guide

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "SqlSyncService - Complete Rebuild" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Stop services and apps
Write-Host "[1/10] Stopping services and apps..." -ForegroundColor Yellow
Stop-Service SqlSyncService -ErrorAction SilentlyContinue
Get-Process -Name "SqlSyncService.Admin" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# Clean old builds
Write-Host "[2/10] Cleaning old builds..." -ForegroundColor Yellow
Remove-Item "src\SqlSyncService\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src\SqlSyncService\obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src\SqlSyncService.Admin\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src\SqlSyncService.Admin\obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src\SqlSyncService.InstallerWizard\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "src\SqlSyncService.InstallerWizard\obj" -Recurse -Force -ErrorAction SilentlyContinue

# Build Service
Write-Host "[3/10] Building SqlSyncService..." -ForegroundColor Yellow
dotnet build src\SqlSyncService\SqlSyncService.csproj -c Release
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Service build failed!" -ForegroundColor Red
    exit 1
}

# Build Admin App
Write-Host "[4/10] Building Admin App (with fixes)..." -ForegroundColor Yellow
dotnet build src\SqlSyncService.Admin\SqlSyncService.Admin.csproj -c Release
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Admin build failed!" -ForegroundColor Red
    exit 1
}

# Build Installer
Write-Host "[5/10] Building Installer..." -ForegroundColor Yellow
dotnet build src\SqlSyncService.InstallerWizard\SqlSyncService.InstallerWizard.csproj -c Release
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Installer build failed!" -ForegroundColor Red
    exit 1
}

# Publish Service
Write-Host "[6/10] Publishing Service..." -ForegroundColor Yellow
dotnet publish src\SqlSyncService\SqlSyncService.csproj -c Release -o "publish-installer\service" --self-contained false

# Publish Admin
Write-Host "[7/10] Publishing Admin App..." -ForegroundColor Yellow
dotnet publish src\SqlSyncService.Admin\SqlSyncService.Admin.csproj -c Release -o "publish-installer\admin" --self-contained false

# Create embedded zips
Write-Host "[8/10] Creating embedded archives..." -ForegroundColor Yellow
if (Test-Path "publish-installer\service-embedded.zip") { Remove-Item "publish-installer\service-embedded.zip" -Force }
if (Test-Path "publish-installer\admin-embedded.zip") { Remove-Item "publish-installer\admin-embedded.zip" -Force }

Compress-Archive -Path "publish-installer\service\*" -DestinationPath "publish-installer\service-embedded.zip" -Force
Compress-Archive -Path "publish-installer\admin\*" -DestinationPath "publish-installer\admin-embedded.zip" -Force

# Copy config samples and integration
Write-Host "[9/10] Copying config files..." -ForegroundColor Yellow
Copy-Item "config-samples\appsettings.json" "publish-installer\service\" -Force
Copy-Item "config-samples\queries.json" "publish-installer\service\" -Force
Copy-Item "config-samples\mapping.json" "publish-installer\service\" -Force
Copy-Item "src\SqlSyncService\integration.json" "publish-installer\service\" -Force

# Build final installer
Write-Host "[10/10] Building final installer with embedded files..." -ForegroundColor Yellow
dotnet publish src\SqlSyncService.InstallerWizard\SqlSyncService.InstallerWizard.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "final-installer"

if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Final installer build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "✓ BUILD COMPLETE!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installer created at:" -ForegroundColor Cyan
Write-Host "  final-installer\SqlSyncService.InstallerWizard.exe" -ForegroundColor White
Write-Host ""
Write-Host "Additional files created:" -ForegroundColor Cyan
Write-Host "  - SqlSyncService-Postman-Collection.json" -ForegroundColor White
Write-Host "  - POSTMAN-API-GUIDE.md" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Uninstall current version (keep config)" -ForegroundColor White
Write-Host "  2. Run: final-installer\SqlSyncService.InstallerWizard.exe" -ForegroundColor White
Write-Host "  3. Configure database credentials in Admin app" -ForegroundColor White
Write-Host "  4. Import Postman collection and test APIs" -ForegroundColor White
Write-Host ""

