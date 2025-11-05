# Complete Reinstallation Guide

This guide will help you create a fresh installation with ALL fixes included.

## ✅ What's Fixed in This Build:

1. **Admin App Database Connection** - Fixed `ConnectTimeout` bug
2. **Installer API Key Display** - Added copyable TextBox with Copy button
3. **Postman Collection** - Complete API collection for all 8 endpoints
4. **API Documentation** - Full guide with examples and troubleshooting

---

## 📋 Step-by-Step Instructions

### Step 1: Build the Complete Installer

Open **PowerShell as Administrator** in the project directory and run:

```powershell
.\rebuild-and-reinstall.ps1
```

This will:
- Build all components (Service, Admin, Installer)
- Create embedded archives
- Generate the final installer: `final-installer\SqlSyncService.InstallerWizard.exe`

**Expected time:** 2-3 minutes

---

### Step 2: Uninstall Current Version

```powershell
.\uninstall-service.ps1
```

This will:
- Stop the service
- Remove program files
- **KEEP** your configuration at `C:\ProgramData\SqlSyncService\`

---

### Step 3: Run the New Installer

```powershell
.\final-installer\SqlSyncService.InstallerWizard.exe
```

During installation:
1. **Database Page:**
   - Server: `localhost\SQLEXPRESS` (or your SQL Server)
   - Database: `Galil`
   - Username: (your SQL username)
   - Password: (your SQL password)
   - Click **Test Connection** (should work now!)

2. **Security Page:**
   - Choose **HTTP** (for local/internal network)
   - Port: `8088`
   - API Key: Auto-generated (will be shown on completion)
   - IP Allow List: `127.0.0.1,94.159.129.230` (add your external IP)

3. **Completion Page:**
   - **COPY** the API key (use the Copy button!)
   - Save it securely
   - Check "Start SqlSyncService now"

---

### Step 4: Verify Installation

**Check service is running:**
```powershell
Get-Service SqlSyncService
# Should show: Running
```

**Check logs are being created:**
```powershell
Get-ChildItem "C:\ProgramData\SqlSyncService\logs\"
# Should show log files
```

**Test local endpoint:**
```powershell
$apiKey = "YOUR_API_KEY_HERE"
Invoke-WebRequest -Uri "http://localhost:8088/api/LogisticSites" -Headers @{"X-API-Key"=$apiKey}
```

---

### Step 5: Configure Admin App (if needed)

Run: `C:\Program Files\SqlSyncService\Admin\SqlSyncService.Admin.exe`

1. Go to **Security** tab
2. Add external IP to allow list: `94.159.129.230`
3. Click **Save Configuration**
4. Restart service: `Restart-Service SqlSyncService`

---

### Step 6: Test with Postman

1. **Import** `SqlSyncService-Postman-Collection.json` into Postman
2. **Set variables:**
   - `baseUrl`: `http://94.159.129.230:8088`
   - `apiKey`: (your API key from installation)
3. **Test** any endpoint, starting with **LogisticSites**

---

## 🔧 Troubleshooting

### Service Won't Start

**Check logs:**
```powershell
Get-Content "C:\ProgramData\SqlSyncService\logs\sqlsync-*.log" -Tail 50
```

**Common issues:**
- Database connection failed → Check credentials in Admin app
- Port already in use → Change port in Admin app
- TCP/IP disabled → Run `.\enable-sql-tcpip.ps1`

### 404 Error in Postman

**Check:**
1. Service is running: `Get-Service SqlSyncService`
2. Correct URL: `http://94.159.129.230:8088/api/LogisticSites`
3. API Key header: `X-API-Key: YOUR_KEY`
4. IP in allow list

### 401 Unauthorized

**Check:**
1. API Key is correct
2. Header name is `X-API-Key` (with dash, not underscore)
3. IP address is in allow list

### Connection Test Hangs

**Enable TCP/IP for SQL Server:**
```powershell
.\enable-sql-tcpip.ps1
Restart-Service MSSQL$SQLEXPRESS
```

---

## 📊 Quick Reference

### Service Control
```powershell
Start-Service SqlSyncService
Stop-Service SqlSyncService
Restart-Service SqlSyncService
Get-Service SqlSyncService
```

### View Logs
```powershell
# Latest log
Get-Content "C:\ProgramData\SqlSyncService\logs\sqlsync-*.log" -Tail 50

# Watch in real-time
Get-Content "C:\ProgramData\SqlSyncService\logs\sqlsync-*.log" -Wait -Tail 20
```

### Configuration Files
- **Service Config:** `C:\ProgramData\SqlSyncService\appsettings.json`
- **Queries:** `C:\ProgramData\SqlSyncService\queries.json`
- **Mapping:** `C:\ProgramData\SqlSyncService\mapping.json`
- **Integration Schema:** `C:\Program Files\SqlSyncService\integration.json`

### Port Forwarding
- **Router:** Forward external port `8088` → `THIS_PC_IP:8088`
- **Firewall:** Automatically configured by installer
- **Test:** `http://94.159.129.230:8088/api/LogisticSites`

---

## ✅ Success Indicators

You'll know everything is working when:

1. ✓ Service status shows **"Running"**
2. ✓ Log files appear in `C:\ProgramData\SqlSyncService\logs\`
3. ✓ Admin app can test database connection successfully
4. ✓ Postman returns data (not 404, 401, or timeout)
5. ✓ You can copy the API key from the installer completion screen

---

## 📞 Support

If you encounter issues:

1. Check logs: `C:\ProgramData\SqlSyncService\logs\`
2. Check Event Viewer: **Windows Logs** → **Application** → Filter by "SqlSyncService"
3. Test database connection in SSMS: `localhost\SQLEXPRESS`
4. Verify port forwarding: `netstat -an | findstr :8088`

---

Good luck! 🚀

