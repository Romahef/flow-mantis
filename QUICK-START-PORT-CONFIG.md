# Quick Start: Port Configuration

## 📦 Installation

### Run the Installer
```cmd
SqlSyncService-Installer.exe
```

### Configure Port During Installation

1. **Welcome Page** → Click "Next"
2. **Database Configuration** → Enter SQL Server details → Click "Next"
3. **Security Configuration**:
   - Enter Admin Passphrase
   - **Set API Listen Port** (default: 8080 for HTTP, 8443 for HTTPS)
     - Common choices: `8080`, `8443`, `5000`, `3000`, etc.
     - Must be between `1024-65535`
   - Select Security Mode (HTTP or HTTPS)
   - Click "Next"
4. Complete installation

### Port is Automatically Configured
- ✅ Service configured to listen on your port
- ✅ Windows Firewall rule created
- ✅ Configuration saved

---

## 🔧 Change Port After Installation

### Using Admin UI

1. **Open Admin UI**:
   - Desktop shortcut: `SqlSyncService Admin`
   - Or: `C:\Program Files\SqlSyncService\Admin\SqlSyncService.Admin.exe`

2. **Login** with your admin passphrase

3. **Go to Security Tab** (🔒 icon)

4. **Find "API Listen Port" section**

5. **Enter new port** (1024-65535)

6. **Click "💾 Update Port"**

7. **Restart the service**:
   ```powershell
   Restart-Service SqlSyncService
   ```
   Or via Services console:
   - Press `Win + R`, type `services.msc`
   - Find "SQL Sync Service"
   - Right-click → Restart

---

## 🌐 Network Configuration

### For Local Network Access

**Configure Router Port Forwarding**:
1. Access your router admin panel (usually http://192.168.1.1)
2. Find "Port Forwarding" or "Virtual Server" section
3. Add rule:
   - **External Port**: Your chosen port (e.g., 8443)
   - **Internal IP**: Server IP (e.g., 192.168.1.100)
   - **Internal Port**: Same port (e.g., 8443)
   - **Protocol**: TCP

### For Windows Firewall (Advanced)

Installer creates rule automatically, but to verify:
```powershell
# Check current rule
netsh advfirewall firewall show rule name="SqlSyncService API"

# Manually add/update rule for port 8443
netsh advfirewall firewall delete rule name="SqlSyncService API"
netsh advfirewall firewall add rule name="SqlSyncService API" dir=in action=allow protocol=TCP localport=8443
```

---

## 🧪 Testing

### Test Port is Open

**From Same Machine**:
```powershell
# HTTP
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing

# HTTPS (with self-signed cert)
Invoke-WebRequest -Uri "https://localhost:8443/health" -SkipCertificateCheck -UseBasicParsing
```

**From Another Machine**:
```powershell
# Replace SERVER_IP with your server's IP
Invoke-WebRequest -Uri "http://SERVER_IP:8080/health" -UseBasicParsing
```

**Expected Response**:
```
StatusCode: 200
```

---

## ⚠️ Common Port Choices

| Port | Protocol | Common Use | Recommendation |
|------|----------|------------|----------------|
| 8080 | HTTP | Development/Internal | ✅ Good for internal networks |
| 8443 | HTTPS | Standard alternate HTTPS | ✅ Recommended for HTTPS |
| 443 | HTTPS | Standard HTTPS | ⚠️ Requires admin/elevated |
| 5000 | HTTP/HTTPS | ASP.NET default | ✅ Works well |
| 3000-3999 | HTTP/HTTPS | Application range | ✅ Usually available |

**Note**: Ports below 1024 typically require elevated privileges on most systems.

---

## 🔍 Troubleshooting

### Port Already in Use
**Error**: "Failed to start service"

**Solution**:
1. Check what's using the port:
   ```powershell
   netstat -ano | findstr :8080
   ```
2. Change to a different port via Admin UI
3. Restart service

### Can't Connect from External Network
**Check**:
1. ✅ Firewall rule exists:
   ```powershell
   netsh advfirewall firewall show rule name="SqlSyncService API"
   ```
2. ✅ Router port forwarding configured
3. ✅ Service is running:
   ```powershell
   Get-Service SqlSyncService
   ```
4. ✅ No ISP blocking (some ISPs block common ports)

### Service Won't Start After Port Change
**Solution**:
1. Check Event Viewer:
   - `eventvwr.msc` → Windows Logs → Application
   - Look for SqlSyncService errors
2. Verify port is available:
   ```powershell
   Test-NetConnection -ComputerName localhost -Port 8080
   ```
3. Try a different port (8081, 8082, etc.)

---

## 📝 Examples

### Example 1: HTTP on Port 5000
During installation or via Admin UI:
- Port: `5000`
- Security Mode: HTTP
- Result: Service listens on `http://0.0.0.0:5000`

### Example 2: HTTPS on Port 8443
- Port: `8443`
- Security Mode: HTTPS (Self-Signed or Let's Encrypt)
- Result: Service listens on `https://0.0.0.0:8443`

### Example 3: Custom Port for Multiple Instances
- Instance 1: Port `8080`
- Instance 2: Port `8081`
- Instance 3: Port `8082`

---

## 🎯 Best Practices

1. **Use Standard Ports for Simplicity**:
   - HTTP: 8080
   - HTTPS: 8443

2. **Avoid Well-Known Ports**:
   - Don't use 80, 443 unless necessary (require admin)
   - Don't use 22 (SSH), 3306 (MySQL), 1433 (SQL Server)

3. **Document Your Port Choice**:
   - Keep record for API consumers
   - Update firewall documentation

4. **Test After Changes**:
   - Always test connectivity after port change
   - Test from both internal and external networks

5. **Use HTTPS in Production**:
   - Always use HTTPS (8443) for external access
   - HTTP (8080) OK for internal/development only

---

## 📚 Related Documents

- `PORT-CONFIGURATION-FEATURE.md` - Technical documentation
- `README.md` - General project information
- `BUILD_INSTRUCTIONS.md` - Build from source

## 🆘 Need Help?

Check the log files:
- Service logs: `C:\ProgramData\SqlSyncService\logs\`
- Event Viewer: Windows Logs → Application
- Admin UI shows current configuration in Security tab

---

**Quick Reference Card**:
```
┌─────────────────────────────────────┐
│ Port Configuration Checklist        │
├─────────────────────────────────────┤
│ ☐ Select port (1024-65535)         │
│ ☐ Configure during install OR      │
│   change in Admin UI → Security tab│
│ ☐ Restart SqlSyncService            │
│ ☐ Test local connection             │
│ ☐ Configure router port forwarding │
│ ☐ Test external connection          │
└─────────────────────────────────────┘
```

