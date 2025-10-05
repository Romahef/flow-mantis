# SqlSyncService

**Version:** 1.0.0  
**Platform:** .NET 8.0 Windows Service  
**License:** Proprietary

A secure Windows Service that provides HTTPS API access to Microsoft SQL Server data with on-demand query execution, JSON streaming, and comprehensive security controls.

---

## 🎯 Overview

SqlSyncService acts as a secure gateway between your SQL Server database and external systems. It:

- ✅ Runs as a Windows Service (.NET 8)
- ✅ Exposes data via secure HTTPS API (port 8443)
- ✅ Executes queries on-demand (no scheduling)
- ✅ Returns JSON matching predefined schemas
- ✅ Supports pagination (offset and token-based)
- ✅ Includes local admin web interface (port 9443)
- ✅ Encrypts all secrets using Windows DPAPI
- ✅ Enforces IP allow-lists and API key authentication

---

## 📋 Requirements

### System Requirements
- **Operating System:** Windows Server 2019+ or Windows 10/11
- **Framework:** .NET 8.0 Runtime
- **Database:** Microsoft SQL Server 2016+ (any edition)
- **Memory:** 512 MB minimum, 1 GB recommended
- **Disk Space:** 100 MB for application + log storage

### Database Requirements
- SQL Server accessible via SQL Login authentication
- Read-only database user recommended
- Network connectivity from service host to SQL Server

### Security Requirements
- Valid SSL/TLS certificate (.pfx format)
- Administrator privileges for installation
- Firewall access for port 8443 (main API)
- Port 9443 available for admin interface (localhost only)

---

## 🚀 Installation

### Option 1: MSI Installer (Recommended)

1. **Download** the latest `SqlSyncService-1.0.0.msi` installer
2. **Run as Administrator**
3. **Follow the installation wizard:**
   - Welcome & EULA
   - Security Configuration (IP allow-list, certificate, API key)
   - Database Configuration (server, credentials)
   - Query Definitions
   - Endpoint Mapping
   - Installation Directory

4. **Complete installation** - the service will start automatically

### Option 2: PowerShell Script

```powershell
# Run as Administrator
.\scripts\install-service.ps1 `
    -DbServer "sqlserver.example.com" `
    -DbPort 1433 `
    -DbName "WMS_Database" `
    -DbUser "sqlsync_user" `
    -DbPassword "SecurePassword123!" `
    -CertPath "C:\certs\server.pfx" `
    -CertPassword "CertPass123!" `
    -IpAllowList @("203.0.113.10", "203.0.113.20") `
    -AdminPassphrase "AdminPass123!"
```

**Note:** The script will generate an API key and display it. **Save this key securely!**

### Option 3: Manual Installation

1. **Build the solution:**
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false
   ```

2. **Copy files** to `C:\Program Files\SqlSyncService\`

3. **Create configuration directory:** `C:\ProgramData\SqlSyncService\`

4. **Configure** `appsettings.json`, `queries.json`, `mapping.json`

5. **Encrypt secrets** using DPAPI helper

6. **Install Windows Service:**
   ```powershell
   sc create SqlSyncService binPath= "C:\Program Files\SqlSyncService\SqlSyncService.exe" start= auto
   sc description SqlSyncService "Provides secure HTTPS API access to SQL Server data"
   sc start SqlSyncService
   ```

7. **Configure firewall:**
   ```powershell
   New-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound" `
       -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow
   ```

---

## ⚙️ Configuration

### Configuration Files

All configuration files are located in `C:\ProgramData\SqlSyncService\`:

- **appsettings.json** - Service, security, and database settings
- **queries.json** - SQL query definitions
- **mapping.json** - API endpoint to query mappings
- **integration.json** - JSON schema definition (in application directory)

### appsettings.json

```json
{
  "Service": {
    "ListenUrl": "https://0.0.0.0:8443"
  },
  "Security": {
    "RequireApiKey": true,
    "IpAllowList": ["203.0.113.10"],
    "EnableHttps": true,
    "Certificate": {
      "Path": "C:\\ProgramData\\SqlSyncService\\certs\\server.pfx",
      "PasswordEncrypted": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA..."
    },
    "ApiKeyEncrypted": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA..."
  },
  "Database": {
    "Server": "127.0.0.1",
    "Instance": "",
    "Port": 1433,
    "Database": "WMS_Database",
    "UsernameEncrypted": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA...",
    "PasswordEncrypted": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA...",
    "CommandTimeoutSeconds": 60
  },
  "Logging": {
    "Level": "Information",
    "Directory": "C:\\ProgramData\\SqlSyncService\\logs"
  },
  "Admin": {
    "ListenUrl": "https://localhost:9443",
    "PassphraseEncrypted": "AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAA..."
  }
}
```

### queries.json

Define SQL queries that can be executed via API:

```json
{
  "Queries": [
    {
      "Name": "Customers",
      "Sql": "SELECT cus_ID, cus_Code, cus_Name FROM Customers WHERE cus_Active = 1",
      "Paginable": true,
      "PaginationMode": "Offset",
      "OrderBy": "cus_ID"
    },
    {
      "Name": "Inventory",
      "Sql": "SELECT stc_ID, stc_SSCC, stc_Quantity FROM Stock",
      "Paginable": true,
      "PaginationMode": "Token",
      "KeyColumns": ["stc_EntryDate", "stc_SSCC"]
    }
  ]
}
```

**Pagination Modes:**
- **Offset** - Uses `ROW_NUMBER()` with page numbers. Requires `OrderBy` field.
- **Token** - Uses keyset pagination with continuation tokens. Requires `KeyColumns` list.

### mapping.json

Map queries to API endpoints:

```json
{
  "Routes": [
    {
      "Endpoint": "Customers",
      "Queries": [
        {
          "QueryName": "Customers",
          "TargetArray": "customers"
        }
      ]
    },
    {
      "Endpoint": "Items",
      "Queries": [
        {
          "QueryName": "Items",
          "TargetArray": "items"
        },
        {
          "QueryName": "StockItems",
          "TargetArray": "stockItems"
        }
      ]
    }
  ]
}
```

**Note:** `TargetArray` names must match arrays defined in `integration.json`.

---

## 🎨 Admin Desktop Application

The service includes a native Windows desktop application for administration.

**Launch:** `SqlSyncService.Admin.exe` or use the desktop shortcut "SqlSyncService Admin"

### Features

- **Security Management** - Rotate API keys, manage IP allow-list, update certificates
- **Database Configuration** - Edit connection settings, test connectivity
- **Query Management** - Add/edit/delete SQL queries with SQL editor
- **Endpoint Mapping** - View query-to-endpoint mappings
- **Validation** - Real-time validation against integration schema
- **Apply Changes** - Atomic configuration updates with rollback on failure

### Authentication

The admin application requires a passphrase set during installation. This passphrase is stored encrypted using DPAPI.

### Usage

1. Launch **SqlSyncService Admin** from Start Menu or Desktop
2. Enter admin passphrase
3. Use tabs to navigate: Security, Database, Queries, Mapping, About
4. Make configuration changes
5. Click **💾 Save Configuration**
6. Restart the service for changes to take effect

**Note:** The admin application reads/writes configuration files directly from `C:\ProgramData\SqlSyncService\`

---

## 🔒 Security

### Security Features

1. **HTTPS Required** - No HTTP listener, TLS 1.2+ only
2. **IP Allow-List** - Requests from non-allowed IPs return 403
3. **API Key Authentication** - X-API-Key header required, returns 401 if invalid
4. **DPAPI Encryption** - All secrets encrypted using Windows LocalMachine scope
5. **Startup Validation** - Service fails to start if security requirements not met
6. **Audit Logging** - All API access logged (IP, endpoint, duration)
7. **Read-Only Database** - Recommend SQL Login with SELECT-only permissions

### Security Rules

| Rule | Behavior |
|------|----------|
| HTTPS disabled | Service fails to start |
| Non-loopback bind without HTTPS | Service fails to start |
| Non-loopback bind with empty allow-list | Service fails to start |
| Missing certificate | Service fails to start |
| Non-allowed IP | Returns 403 Forbidden.IP |
| Missing/invalid API key | Returns 401 Unauthorized.ApiKey |

### Firewall Configuration

**Inbound rule for API (required):**
```powershell
New-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound" `
    -Direction Inbound -Protocol TCP -LocalPort 8443 -Action Allow
```

**Admin interface is localhost-only** - no firewall rule needed.

### Certificate Requirements

- Must be .pfx format with private key
- Must be valid (not expired or not-yet-valid)
- Password must be encrypted in configuration
- Stored in `C:\ProgramData\SqlSyncService\certs\`

### Rotating API Keys

1. Launch **SqlSyncService Admin** application
2. Navigate to **Security** tab
3. Click **🔄 Generate New API Key**
4. **Copy and securely store** the new key displayed (shown in popup)
5. Click **💾 Save Configuration**
6. Update all external systems with new key
7. Restart service

---

## 📡 API Reference

### Base URL

```
https://your-server:8443
```

### Authentication

All API endpoints (except `/health`) require:

**Header:**
```
X-API-Key: your-api-key-here
```

### Endpoints

#### `GET /health`

Health check endpoint (no authentication required).

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-10-05T12:34:56Z",
  "service": "SqlSyncService"
}
```

---

#### `GET /api/queries`

Lists all available queries and endpoint mappings.

**Response:**
```json
{
  "queries": [
    {
      "Name": "Customers",
      "Paginable": true,
      "PaginationMode": "Offset"
    }
  ],
  "routes": [
    {
      "Endpoint": "Customers",
      "Queries": [
        {
          "QueryName": "Customers",
          "TargetArray": "customers"
        }
      ]
    }
  ]
}
```

---

#### `GET /api/queries/{endpointName}`

Executes all queries mapped to an endpoint and returns combined JSON.

**Path Parameters:**
- `endpointName` - Name of the endpoint (e.g., "Customers")

**Query Parameters:**
- `timeout` - Query timeout in seconds (optional)
- `page` - Page number for offset pagination (optional, 1-based)
- `pageSize` - Number of records per page (optional, default 100, max 10000)
- `continuationToken` - Token for next page in token pagination (optional)
- `maxRows` - Maximum total rows to return (optional)

**Examples:**

Simple request:
```http
GET /api/queries/Customers
X-API-Key: your-api-key
```

With offset pagination:
```http
GET /api/queries/Customers?page=2&pageSize=50
X-API-Key: your-api-key
```

With token pagination:
```http
GET /api/queries/Inventory?pageSize=100&continuationToken=AbC123...
X-API-Key: your-api-key
```

**Response:**
```json
{
  "customers": [
    {
      "cus_ID": 1,
      "cus_Code": "CUST001",
      "cus_Name": "Acme Corp",
      "cus_Active": true
    }
  ],
  "_page": {
    "mode": "offset",
    "page": 2,
    "pageSize": 50
  }
}
```

For token pagination:
```json
{
  "stockDetail": [...],
  "_page": {
    "mode": "token",
    "pageSize": 100,
    "continuationToken": "XyZ789..."
  }
}
```

When `continuationToken` is `null`, no more pages remain.

---

#### `POST /api/queries/{endpointName}/execute`

Same as GET endpoint but using POST method (for frameworks that prefer POST).

---

### Error Responses

| Status | Error Code | Description |
|--------|------------|-------------|
| 400 | BadRequest.PaginationNotSupported | Pagination requested on non-paginable query |
| 400 | BadRequest.InvalidToken | Malformed or tampered continuation token |
| 401 | Unauthorized.ApiKey | Missing or invalid X-API-Key header |
| 403 | Forbidden.IP | Request from non-allowed IP address |
| 404 | NotFound.Endpoint | Endpoint name not found in mappings |
| 500 | Server.Error | Internal server error |
| 504 | Db.Timeout | Database query timeout |

**Example Error:**
```json
{
  "error": "Unauthorized.ApiKey",
  "message": "Missing or invalid API key"
}
```

---

## 🔄 Pagination

### Offset Pagination

Best for: Small to medium datasets, when total count is needed.

**How it works:**
- Uses `ROW_NUMBER()` OVER (ORDER BY ...)
- Wraps your SQL in a CTE
- Applies `WHERE __RowNum BETWEEN start AND end`

**Query Requirements:**
```json
{
  "Paginable": true,
  "PaginationMode": "Offset",
  "OrderBy": "cus_ID"
}
```

**Usage:**
```http
GET /api/queries/Customers?page=1&pageSize=100
```

### Token Pagination (Keyset)

Best for: Large datasets, infinite scrolling, efficient cursor-style navigation.

**How it works:**
- Uses composite key comparison: `WHERE (col1, col2) > (lastVal1, lastVal2)`
- Returns HMAC-signed continuation token
- More efficient than offset for large datasets

**Query Requirements:**
```json
{
  "Paginable": true,
  "PaginationMode": "Token",
  "KeyColumns": ["stc_EntryDate", "stc_ID"]
}
```

**Usage:**

First page:
```http
GET /api/queries/Inventory?pageSize=100
```

Next pages (use token from previous response):
```http
GET /api/queries/Inventory?pageSize=100&continuationToken=AbC123XyZ...
```

**Security:** Continuation tokens are HMAC-signed. Tampered tokens return 400 error.

---

## 📊 JSON Schema Validation

All JSON output must match the structure defined in `integration.json`.

### integration.json Example

```json
{
  "arrays": {
    "customers": {
      "fields": [
        { "name": "cus_ID", "type": "int", "nullable": false },
        { "name": "cus_Code", "type": "string", "nullable": false },
        { "name": "cus_Name", "type": "string", "nullable": false },
        { "name": "cus_Email", "type": "string", "nullable": true }
      ]
    }
  }
}
```

### Validation Rules

1. **Startup Validation** - Service validates all mappings against schema
2. **Extra Fields** - Query returns fields not in schema → warning logged
3. **Missing Required Fields** - Query missing non-nullable field → warning logged
4. **Field Names** - Must match exactly (case-sensitive)
5. **Null Handling** - Database NULL → JSON `null`, string "NULL" preserved

---

## 📝 Logging

### Log Locations

- **Event Log:** `Application` → Source: `SqlSyncService`
- **File Logs:** `C:\ProgramData\SqlSyncService\logs\`

### Log Levels

- **Critical** - Startup failures, security violations
- **Error** - Query failures, configuration errors
- **Warning** - Schema mismatches, deprecated features
- **Information** - Requests, query execution times
- **Debug** - Detailed request/response data

### What's Logged

✅ **Logged:**
- Client IP address
- Endpoint accessed
- Query execution duration
- Row counts returned
- Authentication failures
- Configuration changes

❌ **Never Logged:**
- API keys
- Database passwords
- Certificate passwords
- Admin passphrases
- Query result data

### Log Rotation

Logs are automatically rotated daily. Configure retention in `appsettings.json`:

```json
{
  "Logging": {
    "Level": "Information",
    "Directory": "C:\\ProgramData\\SqlSyncService\\logs",
    "RetentionDays": 30
  }
}
```

---

## 🛠️ Troubleshooting

### Service Won't Start

**Check Event Log:**
```powershell
Get-EventLog -LogName Application -Source SqlSyncService -Newest 10
```

**Common Issues:**

1. **Certificate not found**
   - Verify path in `appsettings.json`
   - Check file permissions (SYSTEM account must have read access)

2. **Database connection failed**
   - Test connection: `sqlcmd -S server -U user -P pass`
   - Verify SQL Server allows SQL Login authentication
   - Check firewall between service and SQL Server

3. **Port already in use**
   - Check: `netstat -ano | findstr :8443`
   - Change port in configuration if needed

4. **Security validation failed**
   - Review startup requirements
   - Check IP allow-list not empty for non-loopback binding
   - Ensure HTTPS enabled with valid certificate

### API Returns 403 Forbidden

- **Cause:** Client IP not in allow-list
- **Solution:** Add IP to `Security.IpAllowList` in `appsettings.json`

```json
{
  "Security": {
    "IpAllowList": ["203.0.113.10", "203.0.113.20"]
  }
}
```

### API Returns 401 Unauthorized

- **Cause:** Missing or invalid X-API-Key header
- **Solution:** Include correct API key in request header

```http
X-API-Key: your-actual-api-key-here
```

### Query Timeout Errors

- **Cause:** Query takes longer than `CommandTimeoutSeconds`
- **Solution:** 
  - Optimize SQL query (add indexes, reduce data)
  - Increase timeout: `"CommandTimeoutSeconds": 120`
  - Use pagination to reduce result set size

### Invalid Continuation Token

- **Cause:** Token was tampered with or generated on different machine
- **Solution:** Tokens are machine-specific (HMAC key derived from machine). Restart pagination from beginning.

### Admin Application Won't Launch

- **Cause:** Missing .NET 8 Desktop Runtime
- **Solution:** Install .NET 8 Desktop Runtime from https://dotnet.microsoft.com/download/dotnet/8.0
- **Alternative:** Launch from console to see error messages: `SqlSyncService.Admin.exe`

---

## 🔧 Management

### Service Management

**Start:**
```powershell
Start-Service -Name SqlSyncService
```

**Stop:**
```powershell
Stop-Service -Name SqlSyncService
```

**Restart:**
```powershell
Restart-Service -Name SqlSyncService
```

**Status:**
```powershell
Get-Service -Name SqlSyncService
```

**View Logs:**
```powershell
Get-EventLog -LogName Application -Source SqlSyncService -Newest 50
```

### Configuration Updates

**Method 1: Admin Application (Recommended)**
1. Launch SqlSyncService Admin
2. Make changes
3. Click "💾 Save Configuration"
4. Restart service

**Method 2: Manual Edit**
1. Stop service
2. Edit JSON files in `C:\ProgramData\SqlSyncService\`
3. Validate JSON syntax
4. Start service

**Method 3: PowerShell**
```powershell
Stop-Service -Name SqlSyncService
# Edit configuration files
Start-Service -Name SqlSyncService
```

### Backup Configuration

```powershell
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$source = "C:\ProgramData\SqlSyncService"
$backup = "C:\Backups\SqlSyncService-$timestamp"
Copy-Item -Path $source -Destination $backup -Recurse
```

### Restore Configuration

```powershell
Stop-Service -Name SqlSyncService
Copy-Item -Path "C:\Backups\SqlSyncService-20241005-120000\*" `
    -Destination "C:\ProgramData\SqlSyncService\" -Recurse -Force
Start-Service -Name SqlSyncService
```

---

## 📦 Uninstallation

### Using MSI

1. **Open** Control Panel → Programs and Features
2. **Select** SqlSyncService
3. **Click** Uninstall

Configuration files are preserved by default.

### Using PowerShell Script

```powershell
# Remove service but keep configuration
.\scripts\uninstall-service.ps1

# Remove service and configuration
.\scripts\uninstall-service.ps1 -RemoveConfig
```

### Manual Uninstallation

```powershell
# Stop and remove service
Stop-Service -Name SqlSyncService
sc.exe delete SqlSyncService

# Remove firewall rule
Remove-NetFirewallRule -DisplayName "SqlSyncService 8443 Inbound"

# Remove files (optional)
Remove-Item "C:\Program Files\SqlSyncService" -Recurse -Force
Remove-Item "C:\ProgramData\SqlSyncService" -Recurse -Force
```

---

## 🧪 Development

### Building from Source

```powershell
# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Run tests
dotnet test

# Publish for Windows
dotnet publish -c Release -r win-x64 --self-contained false
```

### Running Locally (Development)

```powershell
# Set environment variable for config directory
$env:SQLSYNC_CONFIG_DIR = "C:\Dev\SqlSyncConfig"

# Run service
dotnet run --project src\SqlSyncService\SqlSyncService.csproj

# Run admin application (separate terminal)
dotnet run --project src\SqlSyncService.Admin\SqlSyncService.Admin.csproj
```

### Project Structure

```
/src
  /SqlSyncService              Main service project
    /Api                       API endpoint definitions
    /Config                    Configuration models & store
    /Database                  SQL Server connectivity
    /Pagination                Pagination implementations
    /Security                  Middleware & validators
    /Serialization             JSON streaming
    Program.cs                 Service entry point
    integration.json           Schema definition
    
  /SqlSyncService.Admin        Admin desktop application (WPF)
    /Services                  Auth & apply services
    MainWindow.xaml            Main admin window
    LoginWindow.xaml           Login dialog
    QueryEditorWindow.xaml     Query editor dialog
    App.xaml                   Application entry point
    
  /SqlSyncService.Tests        Unit & integration tests
    SecurityTests.cs
    PaginationTests.cs
    ContractTests.cs
    ConfigTests.cs
    
/installer                     WiX installer project
/scripts                       PowerShell installation scripts
/config-samples                Sample configuration files
```

---

## 📄 License

Proprietary - All rights reserved

---

## 🤝 Support

For issues, questions, or feature requests, contact your system administrator or development team.

---

## 📚 Additional Resources

- **.NET 8 Documentation:** https://learn.microsoft.com/en-us/dotnet/
- **SQL Server Best Practices:** https://docs.microsoft.com/en-us/sql/
- **Windows Services Guide:** https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service
- **WiX Toolset:** https://wixtoolset.org/

---

**Last Updated:** October 5, 2025  
**Version:** 1.0.0
