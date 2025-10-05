# Build Instructions

## Building on Windows

### Prerequisites

1. **Install .NET 8 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version` should show 8.0.x

2. **Install WiX Toolset v3.11+**
   - Download: https://wixtoolset.org/releases/
   - Install: `WiX311.exe` (or later)
   - Add to PATH: `C:\Program Files (x86)\WiX Toolset v3.11\bin`

3. **Install Visual Studio 2022** (optional, but recommended)
   - With ".NET desktop development" workload
   - With "WiX Toolset Visual Studio Extension"

### Build Steps

#### Method 1: Visual Studio (Easy)

1. Open `SqlSyncService.sln` in Visual Studio 2022
2. Set build configuration to **Release**
3. Right-click `SqlSyncService.Installer` project → **Build**
4. MSI will be at: `installer\bin\Release\SqlSyncService-1.0.0.msi`

#### Method 2: Command Line

```powershell
# Navigate to solution directory
cd C:\path\to\flow-mantis

# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Build installer (requires WiX)
cd installer
candle Product.wxs
light -ext WixUIExtension -ext WixUtilExtension Product.wixobj -out SqlSyncService-1.0.0.msi
```

### Build Output

After successful build:
- **Service EXE**: `src\SqlSyncService\bin\Release\net8.0\win-x64\SqlSyncService.exe`
- **Admin UI EXE**: `src\SqlSyncService.Admin\bin\Release\net8.0\SqlSyncService.Admin.exe`
- **MSI Installer**: `installer\bin\Release\SqlSyncService-1.0.0.msi`

---

## Building on macOS/Linux (Testing Only)

You **cannot** build the Windows installer on macOS, but you can:

### 1. Build and Test the Service Code

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Run tests
dotnet test

# Publish for Windows (creates deployment package)
dotnet publish -c Release -r win-x64 --self-contained false \
    -p:PublishSingleFile=false -o ./publish

# The publish folder can be copied to Windows for testing
```

### 2. Use PowerShell Script for Installation (Recommended)

The PowerShell installation script doesn't require an MSI:

**On your Windows test machine:**

```powershell
# Copy the entire project to Windows machine

# Run installation script
.\scripts\install-service.ps1 `
    -ServicePath "C:\path\to\publish\SqlSyncService.exe" `
    -DbServer "localhost" `
    -DbPort 1433 `
    -DbName "TestDB" `
    -DbUser "testuser" `
    -DbPassword "testpass" `
    -CertPath "C:\certs\test.pfx" `
    -CertPassword "certpass" `
    -IpAllowList @("127.0.0.1") `
    -AdminPassphrase "admin123"
```

---

## Quick Test Without Installation

For development/testing without installing as a Windows Service:

### On Windows:

```powershell
# Set config directory
$env:SQLSYNC_CONFIG_DIR = "C:\Temp\SqlSyncConfig"

# Create config files manually or copy from config-samples/
New-Item -ItemType Directory -Path $env:SQLSYNC_CONFIG_DIR -Force
Copy-Item config-samples\* $env:SQLSYNC_CONFIG_DIR\

# Run service directly (not as Windows Service)
cd src\SqlSyncService\bin\Release\net8.0
.\SqlSyncService.exe

# In another terminal, run Admin UI
cd src\SqlSyncService.Admin\bin\Release\net8.0
.\SqlSyncService.Admin.exe
```

Then access:
- **API**: https://localhost:8443/health
- **Admin UI**: https://localhost:9443/admin

---

## Publishing for Manual Installation

If you don't want to use the MSI installer:

```powershell
# Publish service
dotnet publish src\SqlSyncService\SqlSyncService.csproj `
    -c Release -r win-x64 --self-contained false `
    -o publish\service

# Publish admin UI
dotnet publish src\SqlSyncService.Admin\SqlSyncService.Admin.csproj `
    -c Release -r win-x64 --self-contained false `
    -o publish\admin

# Copy to target machine and use PowerShell script for installation
```

---

## Troubleshooting Build Issues

### "WiX Toolset not found"
- Install WiX Toolset: https://wixtoolset.org/releases/
- Add to PATH: `C:\Program Files (x86)\WiX Toolset v3.11\bin`
- Restart Visual Studio/Terminal

### "Cannot build installer on macOS/Linux"
- **Solution**: Use PowerShell installation script instead
- Or: Set up Windows VM or use Windows machine for MSI build

### ".NET 8 SDK not found"
- Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- Verify installation: `dotnet --version`

### "Project reference not found"
- Run: `dotnet restore` from solution directory
- Check that all .csproj files exist

---

## CI/CD Pipeline (Optional)

For automated builds, you can use GitHub Actions:

```yaml
name: Build MSI

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build -c Release
    
    - name: Test
      run: dotnet test --no-build -c Release
    
    - name: Install WiX
      run: |
        choco install wixtoolset -y
        echo "C:\Program Files (x86)\WiX Toolset v3.11\bin" >> $GITHUB_PATH
    
    - name: Build Installer
      run: |
        cd installer
        candle Product.wxs
        light -ext WixUIExtension Product.wixobj
    
    - name: Upload MSI
      uses: actions/upload-artifact@v3
      with:
        name: SqlSyncService-MSI
        path: installer/*.msi
```

---

## Next Steps

1. **For Testing on macOS**: Use `dotnet publish` and copy to Windows
2. **For Production**: Build MSI on Windows machine with WiX
3. **For Quick Install**: Use PowerShell script (no MSI needed)
