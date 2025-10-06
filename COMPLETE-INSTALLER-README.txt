═══════════════════════════════════════════════════════════════
  SqlSyncService - COMPLETE INSTALLER v2.0
  Everything Included - True One-Click Installation
═══════════════════════════════════════════════════════════════

📦 INSTALLER FILE:
   SqlSyncService-Complete-Installer.exe (79.6 MB)

✨ WHAT'S INCLUDED - EVERYTHING!
   ✓ Windows Service (SqlSyncService)
   ✓ Admin UI Application (WPF Desktop App)
   ✓ .NET 8 Runtime (self-contained, no dependencies)
   ✓ All DLLs and dependencies
   ✓ Configuration wizard with GUI
   ✓ Automatic service installation
   ✓ Firewall configuration
   ✓ Desktop shortcuts
   ✓ Start Menu shortcuts
   ✓ Sample queries and endpoints

═══════════════════════════════════════════════════════════════
🚀 INSTALLATION STEPS:
═══════════════════════════════════════════════════════════════

1. Right-click "SqlSyncService-Complete-Installer.exe"

2. Select "Run as Administrator" (REQUIRED!)

3. Follow the wizard:
   
   PAGE 1: Welcome
   ✓ Review what will be installed
   ✓ Click "Next"
   
   PAGE 2: Database Configuration
   ✓ Enter SQL Server address (e.g., localhost\SQLEXPRESS)
   ✓ Enter database name
   ✓ Enter SQL username and password
   ✓ Click "Test Connection" to verify (optional but recommended)
   ✓ Click "Next"
   
   PAGE 3: Security Configuration
   ✓ API Key: Leave blank for auto-generation (recommended)
   ✓ IP Allow List: Enter IPs or leave default (127.0.0.1, ::1)
   ✓ Admin Passphrase: Set a passphrase for Admin UI access
   ✓ SSL Certificate: Optional (browse for .pfx file or leave blank)
   ✓ Click "Install"
   
   PAGE 4: Installation Progress
   ✓ Watch real-time installation log
   ✓ Service files extracted
   ✓ Admin UI extracted
   ✓ Configuration created
   ✓ Service installed
   ✓ Firewall configured
   ✓ Shortcuts created
   
   PAGE 5: Complete
   ✓ **COPY AND SAVE THE API KEY!** (shown only once)
   ✓ Check "Start SqlSyncService now" if ready
   ✓ Click "Finish"

4. Done! Everything is installed.

═══════════════════════════════════════════════════════════════
📁 WHAT GETS INSTALLED:
═══════════════════════════════════════════════════════════════

Service Files:
   C:\Program Files\SqlSyncService\
   ├── SqlSyncService.exe (Windows Service)
   ├── SqlSyncService.dll
   ├── integration.json
   ├── Admin\
   │   ├── SqlSyncService.Admin.exe ← Admin UI
   │   └── [all Admin UI dependencies]
   └── [all service dependencies]

Configuration:
   C:\ProgramData\SqlSyncService\
   ├── appsettings.json (database, security settings)
   ├── queries.json (SQL query definitions)
   ├── mapping.json (API endpoint mappings)
   ├── certs\ (for SSL certificates)
   └── logs\ (service logs)

Shortcuts Created:
   Desktop:
   ✓ SqlSyncService Admin.lnk

   Start Menu → Programs → SqlSyncService:
   ✓ SqlSyncService Admin.lnk
   ✓ Configuration Files.lnk

Windows Service:
   Name: SqlSyncService
   Display: SQL Sync Service
   Status: Ready to start (or running if you checked the box)

Firewall:
   Rule: "SqlSyncService HTTPS" (TCP port 8443)

═══════════════════════════════════════════════════════════════
🎨 ACCESSING THE ADMIN UI:
═══════════════════════════════════════════════════════════════

METHOD 1: Desktop Shortcut (Easiest)
   ✓ Double-click "SqlSyncService Admin" on your desktop
   ✓ Enter the admin passphrase you set during installation
   ✓ Start configuring!

METHOD 2: Start Menu
   ✓ Start Menu → Programs → SqlSyncService → SqlSyncService Admin
   
METHOD 3: Direct Launch
   ✓ Run: C:\Program Files\SqlSyncService\Admin\SqlSyncService.Admin.exe

═══════════════════════════════════════════════════════════════
📝 WHAT YOU CAN DO IN THE ADMIN UI:
═══════════════════════════════════════════════════════════════

SECURITY TAB:
   • Rotate API keys (generates new key)
   • Add/remove IPs from allow-list
   • Update SSL certificate path
   • Validate certificate

DATABASE TAB:
   • Edit connection settings
   • Change server/database/credentials
   • TEST CONNECTION button (verifies immediately)

QUERIES TAB: ← MAIN CONFIGURATION
   • Add new SQL queries
   • Edit existing queries
   • Delete queries
   • Configure pagination:
     - Offset mode (page numbers)
     - Token mode (cursor-based for large datasets)
   • Set page sizes
   • Define ORDER BY clauses
   • Specify key columns for token pagination

MAPPING TAB:
   • View endpoint-to-query mappings
   • See which endpoints are exposed
   • Validate against integration schema

ABOUT TAB:
   • View service configuration
   • See installation paths
   • Check service status

═══════════════════════════════════════════════════════════════
🔑 IMPORTANT INFORMATION:
═══════════════════════════════════════════════════════════════

API KEY:
   ✓ Shown on completion page - SAVE IT!
   ✓ Required for all API calls (header: X-API-Key)
   ✓ Can generate new one in Admin UI if lost

ADMIN PASSPHRASE:
   ✓ Set during installation
   ✓ Required to open Admin UI
   ✓ Stored as SHA-256 hash in appsettings.json

DEFAULT QUERIES:
   The installer creates 5 sample queries:
   1. Customers - Get all active customers
   2. Products - Get all active products
   3. Orders - Get all orders
   4. OrderDetails - Get order line items
   5. CustomerOrders - Get customers with orders (joined)
   
   You can modify/delete these in the Admin UI!

DEFAULT ENDPOINTS:
   /api/queries/Customers
   /api/queries/Products
   /api/queries/Orders
   /api/queries/OrderDetails
   /api/queries/CustomerOrders

═══════════════════════════════════════════════════════════════
✅ AFTER INSTALLATION:
═══════════════════════════════════════════════════════════════

1. START THE SERVICE (if not already running):
   • Services.msc → "SQL Sync Service" → Start
   • Or PowerShell: Start-Service SqlSyncService

2. TEST THE API:
   • Health check (no auth): https://localhost:8443/health
   • List queries (with API key): https://localhost:8443/api/queries

3. OPEN ADMIN UI:
   • Double-click desktop shortcut
   • Enter your admin passphrase
   • Review/modify queries as needed

4. MAKE CHANGES:
   • Add your own queries in Admin UI
   • Map them to endpoints
   • Click "Save Configuration"
   • Restart service: Restart-Service SqlSyncService

5. TEST YOUR ENDPOINTS:
   PowerShell Example:
   ```
   $apiKey = "YOUR_API_KEY_HERE"
   $headers = @{"X-API-Key" = $apiKey}
   
   # Get customers
   Invoke-RestMethod -Uri "https://localhost:8443/api/queries/Customers" `
       -Headers $headers -SkipCertificateCheck
   ```

═══════════════════════════════════════════════════════════════
🛠️ SYSTEM REQUIREMENTS:
═══════════════════════════════════════════════════════════════

✓ Windows Server 2019+ or Windows 10/11
✓ Administrator privileges (for installation)
✓ SQL Server 2016+ (accessible via network)
✓ 100 MB free disk space
✓ Port 8443 available
✓ NO .NET installation required (self-contained!)

═══════════════════════════════════════════════════════════════
📊 API USAGE EXAMPLES:
═══════════════════════════════════════════════════════════════

After starting the service:

# 1. Health check (no API key needed)
https://localhost:8443/health

# 2. List all endpoints (API key required)
https://localhost:8443/api/queries
Header: X-API-Key: [your-key]

# 3. Get data from endpoint
https://localhost:8443/api/queries/Customers
Header: X-API-Key: [your-key]

# 4. With pagination
https://localhost:8443/api/queries/Products?page=1&pageSize=10
Header: X-API-Key: [your-key]

PowerShell Example:
```powershell
$apiKey = "YOUR_API_KEY_FROM_INSTALL"
$headers = @{"X-API-Key" = $apiKey}

# Get all customers
$customers = Invoke-RestMethod `
    -Uri "https://localhost:8443/api/queries/Customers" `
    -Headers $headers `
    -SkipCertificateCheck

$customers.customers | Format-Table
```

═══════════════════════════════════════════════════════════════
🗑️ UNINSTALLATION:
═══════════════════════════════════════════════════════════════

1. Stop the service:
   services.msc → "SQL Sync Service" → Stop

2. Remove the service:
   PowerShell (as Admin): sc.exe delete SqlSyncService

3. Remove firewall rule:
   Remove-NetFirewallRule -DisplayName "SqlSyncService HTTPS"

4. Delete files:
   • C:\Program Files\SqlSyncService\
   • C:\ProgramData\SqlSyncService\ (optional - contains config)

5. Remove shortcuts:
   • Desktop: SqlSyncService Admin.lnk
   • Start Menu: Programs\SqlSyncService\

═══════════════════════════════════════════════════════════════
❓ TROUBLESHOOTING:
═══════════════════════════════════════════════════════════════

Installer won't run?
→ Right-click → "Run as Administrator" (required!)
→ Check Windows Defender/Antivirus

Service won't start?
→ Open Event Viewer → Application → Look for SqlSyncService errors
→ Check database connection settings
→ Verify SQL Server is running
→ Check logs: C:\ProgramData\SqlSyncService\logs\

Admin UI won't open?
→ Launch from desktop shortcut
→ Check Admin passphrase
→ Try: C:\Program Files\SqlSyncService\Admin\SqlSyncService.Admin.exe

Can't connect to API?
→ Verify service is running: Get-Service SqlSyncService
→ Test health endpoint: https://localhost:8443/health
→ Check firewall rule exists
→ Verify port 8443 is not used by another app

401 Unauthorized?
→ Include X-API-Key header in request
→ Verify API key is correct
→ Generate new key in Admin UI if lost

403 Forbidden?
→ Your IP is not in allow-list
→ Add IP in Admin UI → Security tab
→ Or edit: C:\ProgramData\SqlSyncService\appsettings.json

═══════════════════════════════════════════════════════════════
🎯 KEY IMPROVEMENTS IN THIS VERSION:
═══════════════════════════════════════════════════════════════

✅ Admin UI now INCLUDED in installer
✅ Proper Windows shortcuts (.lnk files, not batch files)
✅ Start Menu integration
✅ Desktop shortcut for Admin UI
✅ Link to configuration folder
✅ Everything in one 79.6 MB file
✅ True one-click installation
✅ No missing files
✅ No manual steps required

═══════════════════════════════════════════════════════════════

📚 DOCUMENTATION:
   • Full guide: README.md
   • Security info: SECURITY.md
   • Test setup: TEST-SETUP-COMPLETE.txt
   • Admin UI guide: ADMIN-UI-GUIDE.txt

🎉 READY TO DEPLOY!
   Send this ONE file to any Windows server and install!

═══════════════════════════════════════════════════════════════

