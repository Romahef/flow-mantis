# Quick Start from macOS

You're developing on macOS but need to test on Windows. Here's how:

## What You Can Do on macOS

### 1. Build and Test Code (Verify it compiles)

```bash
cd /Users/roman/Sites/mantis-flow/flow-mantis

# Restore dependencies
dotnet restore

# Build everything
dotnet build -c Release

# Run all tests
dotnet test
```

### 2. Publish for Windows Deployment

```bash
# Create Windows deployment package
dotnet publish src/SqlSyncService/SqlSyncService.csproj \
    -c Release -r win-x64 --self-contained false \
    -o ./publish-windows/service

# Package includes everything needed except .NET 8 runtime
```

### 3. Create Deployment ZIP

```bash
# Create a zip file to transfer to Windows
cd publish-windows
zip -r SqlSyncService-Deploy.zip service/

# Also include config samples and scripts
cd ..
zip -r SqlSyncService-Complete.zip \
    publish-windows/ \
    config-samples/ \
    scripts/ \
    README.md \
    SECURITY.md
```

---

## What You Need Windows For

### Option A: Use a Windows VM (Recommended for Testing)

1. **Install Windows VM** (Parallels, VMware, or VirtualBox)
2. **Install .NET 8 Runtime** on Windows VM
3. **Copy** `SqlSyncService-Complete.zip` to Windows VM
4. **Run** PowerShell installation script

### Option B: Use a Windows Machine/Server

1. **Copy project** to Windows machine
2. **Install Prerequisites**:
   - .NET 8 Runtime (https://dotnet.microsoft.com/download/dotnet/8.0)
   - SQL Server (for testing)
3. **Install using PowerShell script**

---

## Testing on Windows (After Transfer)

### Method 1: PowerShell Installation Script (Easiest)

On your Windows machine:

```powershell
# Navigate to project directory
cd C:\path\to\flow-mantis

# Install with test configuration
.\scripts\install-service.ps1 `
    -DbServer "localhost" `
    -DbPort 1433 `
    -DbName "TestDB" `
    -DbUser "testuser" `
    -DbPassword "TestPass123!" `
    -IpAllowList @("127.0.0.1") `
    -AdminPassphrase "admin123"

# The script will display the generated API key - save it!
```

### Method 2: Manual Run (No Installation)

For quick testing without installing as a service:

```powershell
# Set environment variable for config location
$env:SQLSYNC_CONFIG_DIR = "C:\Temp\SqlSyncTest"

# Create directory and copy sample configs
New-Item -ItemType Directory -Path $env:SQLSYNC_CONFIG_DIR -Force
Copy-Item config-samples\* $env:SQLSYNC_CONFIG_DIR\

# Edit appsettings.json with your settings (in Notepad)
notepad $env:SQLSYNC_CONFIG_DIR\appsettings.json

# Run the service directly (console mode, not as Windows Service)
cd publish-windows\service
.\SqlSyncService.exe

# You should see: "SqlSyncService starting on https://0.0.0.0:8443"
```

In another PowerShell window:
```powershell
# Test the API
curl -k https://localhost:8443/health
```

---

## Building Windows Installer (MSI)

⚠️ **This ONLY works on Windows** (requires WiX Toolset)

On a Windows machine:

```powershell
# Install prerequisites
# 1. .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
# 2. WiX Toolset: https://wixtoolset.org/releases/

# Build solution
dotnet build -c Release

# Build installer (if you have WiX)
cd installer
candle Product.wxs
light -ext WixUIExtension -ext WixUtilExtension Product.wixobj -out SqlSyncService-1.0.0.msi
```

---

## Recommended Workflow

### Phase 1: Development on macOS ✅ (You are here)
- ✅ Write code
- ✅ Build solution
- ✅ Run tests
- ✅ Commit to git

### Phase 2: Testing on Windows 🔄 (Next step)
- Transfer code to Windows VM/machine
- Install using PowerShell script
- Test API endpoints
- Test Admin UI
- Verify security features

### Phase 3: Production Deployment 🚀
- Build MSI installer on Windows
- Deploy to production Windows Server
- Configure with production credentials
- Monitor logs and performance

---

## Immediate Action Items

**Right now on your Mac:**

```bash
# 1. Verify everything compiles
cd /Users/roman/Sites/mantis-flow/flow-mantis
dotnet build -c Release

# 2. Run tests
dotnet test

# 3. Create deployment package
dotnet publish src/SqlSyncService/SqlSyncService.csproj \
    -c Release -r win-x64 -o ./publish

# 4. Create transfer zip
zip -r SqlSyncService.zip publish/ config-samples/ scripts/ README.md

# 5. Copy SqlSyncService.zip to your Windows test machine
```

**Then on Windows:**

```powershell
# Extract zip
Expand-Archive SqlSyncService.zip -DestinationPath C:\SqlSyncTest

# Run installation script
cd C:\SqlSyncTest
.\scripts\install-service.ps1 `
    -ServicePath "C:\SqlSyncTest\publish\SqlSyncService.exe" `
    -DbServer "your-sql-server" `
    -DbName "your-database" `
    -DbUser "sql-user" `
    -DbPassword "sql-password"
```

---

## Alternative: Use GitHub Actions

If you push to GitHub, you can use GitHub Actions to build on Windows automatically:

```yaml
# .github/workflows/build.yml
name: Build Windows Package

on: [push]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build -c Release
      - run: dotnet test
      - run: dotnet publish -c Release -r win-x64 -o publish
      - uses: actions/upload-artifact@v3
        with:
          name: windows-package
          path: publish/
```

Then download the artifact from GitHub Actions!

---

## Questions?

- **Can't access Windows machine?** Consider Azure/AWS Windows VM
- **Need MSI immediately?** Use PowerShell script instead (no MSI needed)
- **Testing database?** Use SQL Server Express (free)
- **Certificate for testing?** Script can generate self-signed cert
