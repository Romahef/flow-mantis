# SqlSyncService - Release Notes

## Version 1.0 - Port Configuration Update
**Release Date**: October 2025

---

## 🎉 What's New

### Dynamic API Port Configuration
Users can now select and change the API listen port for maximum flexibility!

#### ✨ Features

1. **Custom Port Selection During Installation**
   - Choose any port between 1024-65535
   - Smart defaults: 8080 (HTTP) or 8443 (HTTPS)
   - Real-time validation
   - Automatic Windows Firewall configuration

2. **Port Management in Admin UI**
   - View current port in Security tab
   - Change port without reinstallation
   - Preserves HTTP/HTTPS protocol
   - Clear restart instructions

3. **Automatic Configuration**
   - Firewall rules created automatically
   - Configuration saved to `appsettings.json`
   - Service configured for selected port

---

## 📦 Installer: SqlSyncService-Installer.exe

### What It Includes
✅ Complete SqlSyncService (Windows Service)  
✅ Admin UI (Desktop Application)  
✅ .NET 8 Runtime (embedded)  
✅ Sample Configuration Files  
✅ Pre-configured Database Queries  

### Size
📊 **84.23 MB** (single executable)

### Requirements
- Windows 10 or later
- SQL Server (any edition, including Express)
- Administrator privileges

### Features
- 🖥️ Full GUI installation wizard
- 🔐 Multiple security options (HTTP, HTTPS Self-Signed, Let's Encrypt, Custom Cert)
- 🔌 **Dynamic port configuration**
- 💾 Automatic Windows Service installation
- 🛡️ Windows Firewall auto-configuration
- 📝 Sample queries included
- 🎯 Desktop shortcuts created
- 🔄 Auto-detects and removes previous installations

---

## 🚀 Quick Start

### 1. Run Installer
```cmd
SqlSyncService-Installer.exe
```

### 2. Configure During Installation
- Database connection (SQL Server credentials)
- **API Listen Port** (default: 8080 or 8443)
- Security mode (HTTP or HTTPS)
- Admin passphrase
- IP whitelist

### 3. Installation Completes
- Service installed: `SqlSyncService`
- Admin UI: Desktop shortcut created
- Configuration: `C:\ProgramData\SqlSyncService\`
- Firewall: Rule created for your port

### 4. Start Using
```powershell
# Check service status
Get-Service SqlSyncService

# Test API
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
```

---

## 🎛️ Port Configuration Guide

### Set Port During Installation
1. On Security Configuration page
2. Enter desired port in "API Listen Port" field
3. Port auto-suggests based on HTTP/HTTPS selection
4. Validates port range (1024-65535)

### Change Port After Installation
1. Open **SqlSyncService Admin** (desktop shortcut)
2. Login with admin passphrase
3. Go to **Security** tab (🔒)
4. Find **API Listen Port** section
5. Enter new port
6. Click **"💾 Update Port"**
7. Restart service:
   ```powershell
   Restart-Service SqlSyncService
   ```

### Network Configuration
**Router Port Forwarding**:
- External Port: Your chosen port
- Internal IP: Server's local IP
- Internal Port: Same port
- Protocol: TCP

---

## 🔧 What's Fixed

### Admin UI Improvements
- ✅ Removed all debug code and popups
- ✅ Cleaner authentication flow
- ✅ Better error handling
- ✅ Improved password validation

### Installer Improvements
- ✅ Better uninstall process (kills processes, retries deletions)
- ✅ Dynamic firewall rules based on port
- ✅ Proper configuration file structure
- ✅ Protocol preservation (HTTP vs HTTPS)

---

## 📁 What's Included

### Core Service
- **Location**: `C:\Program Files\SqlSyncService\`
- **Service Name**: `SqlSyncService`
- **Display Name**: "SQL Sync Service"

### Admin UI
- **Location**: `C:\Program Files\SqlSyncService\Admin\`
- **Shortcut**: Desktop + Start Menu
- **Features**: 
  - API key rotation
  - **Port configuration**
  - IP whitelist management
  - Database connection testing
  - Query management
  - Mapping configuration

### Configuration
- **Location**: `C:\ProgramData\SqlSyncService\`
- **Files**:
  - `appsettings.json` - Main configuration
  - `queries.json` - SQL query definitions
  - `mapping.json` - API endpoint mappings
  - `logs\` - Application logs

### Sample Data
Pre-configured with 5 sample queries:
- Customers
- Products
- Orders
- Order Details
- Customer Orders (with JOIN)

---

## 🔐 Security Features

1. **API Key Authentication**
   - Auto-generated secure keys
   - Encrypted storage
   - Easy rotation via Admin UI

2. **IP Whitelisting**
   - Restrict access by IP address
   - Manage via Admin UI
   - Supports IPv4 and IPv6

3. **HTTPS Support**
   - Self-signed certificates (auto-generated)
   - Let's Encrypt (free SSL)
   - Custom certificates (.pfx)

4. **Admin Passphrase**
   - SHA-256 hashed
   - DPAPI encrypted storage
   - Required for Admin UI access

5. **Encrypted Credentials**
   - Database passwords encrypted with DPAPI
   - Machine-specific encryption
   - Secure storage in configuration

---

## 📊 Configuration File Structure

### appsettings.json
```json
{
  "Service": {
    "ListenUrl": "https://0.0.0.0:8443"  // ← Your custom port here
  },
  "Security": {
    "RequireApiKey": true,
    "ApiKeyEncrypted": "...",
    "IpAllowList": ["127.0.0.1", "::1"],
    "EnableHttps": true,
    "Certificate": {
      "Path": "...",
      "PasswordEncrypted": "..."
    }
  },
  "Database": {
    "Server": "localhost",
    "Instance": "SQLEXPRESS",
    "Port": 1433,
    "Database": "YourDatabase",
    "UsernameEncrypted": "...",
    "PasswordEncrypted": "...",
    "TrustServerCertificate": true,
    "CommandTimeoutSeconds": 60
  },
  "Admin": {
    "PassphraseEncrypted": "..."
  }
}
```

---

## 🐛 Known Issues

### None at this time
All previously identified issues have been resolved.

---

## 🔄 Upgrade from Previous Version

### Automatic Upgrade
The installer automatically:
1. Detects existing installations
2. Stops the service
3. Kills Admin UI processes
4. Removes old files (with retries)
5. Installs new version
6. Preserves configuration (if desired)

### Manual Upgrade
If automatic upgrade fails:
1. Stop SqlSyncService:
   ```powershell
   Stop-Service SqlSyncService
   ```
2. Backup configuration:
   ```powershell
   Copy-Item "C:\ProgramData\SqlSyncService" -Destination "C:\Backup\SqlSyncService" -Recurse
   ```
3. Run new installer
4. Restore configuration if needed

---

## 📚 Documentation

- `README.md` - Project overview
- `PORT-CONFIGURATION-FEATURE.md` - Detailed port configuration documentation
- `QUICK-START-PORT-CONFIG.md` - Quick reference guide
- `BUILD_INSTRUCTIONS.md` - Build from source
- `PROJECT_SUMMARY.md` - Architecture and design

---

## 🆘 Support & Troubleshooting

### Logs Location
```
C:\ProgramData\SqlSyncService\logs\
```

### Common Issues

**Service won't start**:
- Check Event Viewer (Windows Logs → Application)
- Verify port is not in use: `netstat -ano | findstr :8080`
- Try different port in Admin UI

**Can't connect externally**:
- Verify firewall rule exists
- Configure router port forwarding
- Test from internal network first

**Admin UI password not working**:
- Check configuration: `C:\ProgramData\SqlSyncService\appsettings.json`
- Ensure `Admin.PassphraseEncrypted` exists
- Check logs for errors

### Event Viewer
```
eventvwr.msc → Windows Logs → Application
Filter: Source = "SqlSyncService"
```

---

## 👥 Contributing

This is a production-ready release. For feature requests or bug reports, please document:
1. Steps to reproduce
2. Expected behavior
3. Actual behavior
4. Log files
5. Configuration (sanitized)

---

## 📋 Version History

### v1.0 (Current)
- ✨ Dynamic port configuration
- ✅ Clean code (removed debug artifacts)
- ✅ Improved uninstall process
- ✅ Better Admin UI
- ✅ Comprehensive documentation

---

## 🎯 Roadmap (Future Enhancements)

Potential future features:
- Port availability check before applying
- Service auto-restart after port change
- Multiple listen URLs (bind to multiple ports)
- IPv4/IPv6 interface selection
- Port range for load balancing
- Docker containerization
- Linux/macOS support

---

## ✅ Production Ready

This release has been:
- ✅ Fully tested on Windows 10/11
- ✅ Tested with SQL Server 2019/2022/Express
- ✅ Validated with HTTP and HTTPS
- ✅ Tested with custom ports (8080-9999)
- ✅ Verified firewall configuration
- ✅ Tested Admin UI functionality
- ✅ Validated upgrade process

---

## 📜 License

See LICENSE file for details.

---

## 🙏 Acknowledgments

Built with:
- .NET 8
- WPF (Windows Presentation Foundation)
- Microsoft.Data.SqlClient
- ASP.NET Core Minimal APIs
- System.Security.Cryptography (DPAPI)

---

**Ready to Deploy! 🚀**

Your production-ready installer is available:
- **File**: `SqlSyncService-Installer.exe`
- **Size**: 84.23 MB
- **Platform**: Windows 10+ (x64)
- **Dependencies**: None (fully self-contained)

Simply run the installer on any target server and you're good to go!






