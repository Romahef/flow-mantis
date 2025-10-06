═══════════════════════════════════════════════════════════════
  SqlSyncService - COMPLETE INSTALLER v3.0
  Full-Featured One-Click Installation with Multiple Security Options
═══════════════════════════════════════════════════════════════

📦 INSTALLER FILE:
   SqlSyncService-Complete-Installer.exe (72.3 MB)

✨ WHAT'S INCLUDED - EVERYTHING!
   ✓ Windows Service (SqlSyncService)
   ✓ Admin UI Application (WPF Desktop App)
   ✓ .NET 8 Runtime (self-contained, no dependencies)
   ✓ All DLLs and dependencies
   ✓ Configuration wizard with GUI
   ✓ Automatic service installation
   ✓ Firewall configuration
   ✓ Desktop & Start Menu shortcuts
   ✓ Sample queries and endpoints
   ✓ Existing installation detection and auto-uninstall

🔐 SECURITY OPTIONS (Choose during installation):
   
   [1] HTTP (No Encryption) - Port 8080
       ✓ Best for internal networks
       ✓ Works with IP addresses
       ✓ No certificate required
       ✓ Simple and fast
   
   [2] HTTPS with Self-Signed Certificate - Port 8443
       ✓ Encrypted connection
       ✓ Works with IP addresses  
       ✓ Auto-generated during installation
       ✓ Clients must accept self-signed certificate
   
   [3] HTTPS with Let's Encrypt (FREE SSL) - Port 8443 ⭐ NEW!
       ✓ Trusted SSL certificate
       ✓ Requires a public domain name
       ✓ Domain must point to this server
       ✓ Port 80 must be accessible
       ✓ Automatic 90-day renewal configured
       ✓ Production-ready
   
   [4] HTTPS with Custom Certificate - Port 8443
       ✓ Use your own .pfx certificate file
       ✓ Enterprise-grade security
       ✓ Full trust chain

═══════════════════════════════════════════════════════════════
🚀 INSTALLATION STEPS:
═══════════════════════════════════════════════════════════════

1. Right-click "SqlSyncService-Complete-Installer.exe"

2. Select "Run as Administrator" (REQUIRED!)
   ⚠ The installer will automatically request admin privileges

3. If existing installation detected:
   ✓ Installer will offer to uninstall previous version
   ✓ Configuration files are preserved
   ✓ One-click removal and fresh install

4. Follow the wizard:
   
   PAGE 1: Welcome
   ✓ Review what will be installed
   ✓ Click "Next"
   
   PAGE 2: Database Configuration
   ✓ Enter SQL Server address (e.g., localhost\SQLEXPRESS)
   ✓ Enter database name
   ✓ Enter SQL username and password
   ✓ Click "Test Connection" to verify (recommended)
   ✓ Click "Next"
   
   PAGE 3: Security Configuration
   ✓ Enter API key (or leave blank to auto-generate)
   ✓ Configure IP Allow List (comma-separated IPs)
   ✓ Enter Admin Passphrase for Admin UI
   
   ✓ Choose Security Mode:
     
     Option 1: HTTP (No Encryption)
     ✓ Perfect for: Internal networks, private VLANs
     ✓ Access via: http://server-ip:8080
     
     Option 2: HTTPS with Self-Signed
     ✓ Perfect for: IP-based access with encryption
     ✓ Access via: https://server-ip:8443
     ✓ Note: Browser will show certificate warning
     
     Option 3: HTTPS with Let's Encrypt ⭐
     ✓ Perfect for: Production with domain names
     ✓ Enter your domain name (e.g., api.example.com)
     ✓ Enter contact email for renewal notices
     ✓ Accept Let's Encrypt Terms of Service
     ✓ Access via: https://yourdomain.com:8443
     ✓ Requirements:
        - Domain must point to this server's public IP
        - Port 80 must be open and accessible
        - Server must be reachable from internet
     
     Option 4: Custom Certificate
     ✓ Perfect for: Enterprise with existing certs
     ✓ Browse to your .pfx certificate file
     ✓ Enter certificate password (if any)
   
   ✓ Click "Next"
   
   PAGE 4: Installing
   ✓ Watch real-time installation progress
   ✓ All steps automated:
      [1/8] Extracting service files
      [2/8] Extracting Admin UI
      [3/8] Creating directories
      [4/8] Generating API key
      [5/8] Creating configuration files
      [6/8] Installing Windows Service
      [7/8] Configuring firewall
      [8/8] Creating shortcuts
   
   PAGE 5: Complete!
   ✓ Copy the displayed API key (SAVE IT SECURELY!)
   ✓ Optionally start the service immediately
   ✓ Click "Finish"

5. DONE! 🎉

═══════════════════════════════════════════════════════════════
📋 WHAT WAS INSTALLED:
═══════════════════════════════════════════════════════════════

Service Files:
   C:\Program Files\SqlSyncService\
   ├── SqlSyncService.exe
   ├── SqlSyncService.Admin.exe (via Admin\ subfolder)
   └── All required DLLs

Configuration Files:
   C:\ProgramData\SqlSyncService\
   ├── appsettings.json  (service configuration)
   ├── queries.json      (5 sample queries included)
   ├── mapping.json      (5 API endpoints mapped)
   └── logs\             (service logs directory)

Shortcuts:
   Desktop:
      └── SqlSyncService Admin.lnk
   
   Start Menu:
      Programs\SqlSyncService\
      ├── SqlSyncService Admin.lnk
      └── Configuration Files.lnk

Windows Service:
   Name: SqlSyncService
   Display Name: SQL Sync Service
   Description: Provides secure HTTPS API access to SQL Server data
   Startup Type: Manual (start on demand)

Firewall Rule:
   Name: SqlSyncService HTTPS (or HTTP)
   Port: 8443 (HTTPS) or 8080 (HTTP)
   Direction: Inbound
   Action: Allow

Let's Encrypt (if selected):
   Certificate: C:\ProgramData\SqlSyncService\letsencrypt-cert.pfx
   Renewal Task: Scheduled daily at 3 AM
   Task Name: SqlSyncService-CertRenewal

═══════════════════════════════════════════════════════════════
🔧 POST-INSTALLATION:
═══════════════════════════════════════════════════════════════

1. Start the Service:
   Option A: Check "Start service now" on completion page
   Option B: Run as Admin: net start SqlSyncService
   Option C: Services.msc → Find "SQL Sync Service" → Start

2. Verify Service is Running:
   - Open Services (services.msc)
   - Find "SQL Sync Service"
   - Status should be "Running"

3. Test API Access:
   HTTP Mode:
      curl http://localhost:8080/api/customers -H "X-API-Key: YOUR_API_KEY"
   
   HTTPS Self-Signed:
      curl https://localhost:8443/api/customers -H "X-API-Key: YOUR_API_KEY" -k
   
   HTTPS Let's Encrypt or Custom:
      curl https://yourdomain.com:8443/api/customers -H "X-API-Key: YOUR_API_KEY"

4. Configure Queries via Admin UI:
   - Double-click "SqlSyncService Admin" shortcut on desktop
   - Enter your admin passphrase
   - Add/edit queries and API endpoints
   - Click "Apply Configuration"
   - Restart service for changes to take effect

═══════════════════════════════════════════════════════════════
📁 PRE-CONFIGURED SAMPLE ENDPOINTS:
═══════════════════════════════════════════════════════════════

The installer includes 5 ready-to-use sample endpoints:

1. GET /api/customers
   - Returns paginated list of customers
   - Supports: ?page=1&pageSize=20

2. GET /api/products  
   - Returns paginated list of products
   - Supports: ?page=1&pageSize=20

3. GET /api/orders
   - Returns paginated list of orders
   - Supports: ?page=1&pageSize=20

4. GET /api/orderdetails
   - Returns order line items
   - Supports: ?page=1&pageSize=20

5. GET /api/customerorders
   - Returns customers with their orders (joined query)
   - Supports: ?page=1&pageSize=20

All endpoints require X-API-Key header with your generated API key.

═══════════════════════════════════════════════════════════════
🛡️ SECURITY RECOMMENDATIONS:
═══════════════════════════════════════════════════════════════

INTERNAL USE (Private Network):
   ✓ Use HTTP mode
   ✓ Configure IP whitelist to allow only trusted IPs
   ✓ Keep API key secure
   ✓ Example: Office network, VPN, private VLAN

EXTERNAL USE (Internet-Facing):
   ✓ Use HTTPS with Let's Encrypt or Custom Certificate
   ✓ Use strong API key (auto-generated)
   ✓ Configure strict IP whitelist
   ✓ Monitor logs regularly
   ✓ Keep service updated

IP-BASED ACCESS (No Domain):
   ✓ Use HTTPS with Self-Signed Certificate
   ✓ Document certificate acceptance for clients
   ✓ Or use HTTP if on trusted network

PRODUCTION DOMAINS:
   ✓ Use Let's Encrypt for free trusted SSL
   ✓ Ensure automatic renewal is configured
   ✓ Monitor renewal emails

═══════════════════════════════════════════════════════════════
🔄 UPDATING/REINSTALLING:
═══════════════════════════════════════════════════════════════

The installer automatically detects existing installations!

When you run the installer and an existing version is detected:
   1. Installer shows a dialog
   2. Choose "Yes" to auto-uninstall previous version
   3. Configuration files are preserved in C:\ProgramData\SqlSyncService
   4. Continue with fresh installation
   5. Reuse existing configuration or update as needed

═══════════════════════════════════════════════════════════════
❌ MANUAL UNINSTALLATION:
═══════════════════════════════════════════════════════════════

If needed, to completely remove SqlSyncService:

1. Stop the service:
   net stop SqlSyncService

2. Remove the service:
   sc delete SqlSyncService

3. Remove firewall rule:
   netsh advfirewall firewall delete rule name="SqlSyncService HTTPS"

4. Delete files:
   rmdir /s "C:\Program Files\SqlSyncService"
   rmdir /s "C:\ProgramData\SqlSyncService"  (⚠ removes config!)

5. Remove shortcuts:
   - Delete from Desktop
   - Delete from Start Menu\Programs\SqlSyncService

6. Remove scheduled task (if Let's Encrypt was used):
   schtasks /delete /tn "SqlSyncService-CertRenewal" /f

═══════════════════════════════════════════════════════════════
📞 TROUBLESHOOTING:
═══════════════════════════════════════════════════════════════

Service Won't Start:
   ✓ Check logs: C:\ProgramData\SqlSyncService\logs\
   ✓ Verify database connection in appsettings.json
   ✓ Ensure SQL Server is running
   ✓ Check Windows Event Viewer → Application logs

Certificate Errors (Let's Encrypt):
   ✓ Verify domain points to this server (nslookup yourdomain.com)
   ✓ Check port 80 is open (netstat -an | findstr :80)
   ✓ Test domain accessibility from internet
   ✓ Check firewall allows port 80 inbound
   ✓ Review installation log for ACME challenge errors

API Returns 401 Unauthorized:
   ✓ Verify X-API-Key header is included
   ✓ Check API key matches configuration
   ✓ Ensure IP is in whitelist (or whitelist is empty for localhost only)

Can't Access Admin UI:
   ✓ Verify correct admin passphrase
   ✓ Check Admin UI exists: C:\Program Files\SqlSyncService\Admin\
   ✓ Run as Administrator if needed

Self-Signed Certificate Warnings:
   ✓ This is normal for self-signed certificates
   ✓ Use -k flag with curl
   ✓ Accept certificate in browser
   ✓ Or import certificate to Trusted Root store

═══════════════════════════════════════════════════════════════
✨ FEATURES BY VERSION:
═══════════════════════════════════════════════════════════════

v3.0 (Current):
   ✓ HTTP mode for internal networks
   ✓ Self-signed certificate auto-generation
   ✓ Let's Encrypt integration with auto-renewal
   ✓ Custom certificate support
   ✓ Existing installation detection
   ✓ One-click uninstall before reinstall
   ✓ Improved security options UI

v2.0:
   ✓ Complete GUI installer
   ✓ Embedded Admin UI
   ✓ Sample queries and endpoints
   ✓ Desktop and Start Menu shortcuts
   ✓ Database connection testing

v1.0:
   ✓ Basic MSI installer
   ✓ Manual configuration required

═══════════════════════════════════════════════════════════════
📧 SUPPORT & DOCUMENTATION:
═══════════════════════════════════════════════════════════════

GitHub Repository:
   https://github.com/Romahef/flow-mantis

Full Documentation:
   See README.md in repository

Issues & Bug Reports:
   https://github.com/Romahef/flow-mantis/issues

═══════════════════════════════════════════════════════════════

Last Updated: 2025-10-06
Installer Version: 3.0
Size: 72.3 MB

═══════════════════════════════════════════════════════════════
