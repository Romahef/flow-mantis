# Query Testing Instructions

## ✅ What I've Updated

### 1. Queries Configuration
**Location:** `C:\ProgramData\SqlSyncService\queries.json`

**8 New Queries:**
1. **LS** - Logistic Sites
2. **DEPOSITOR** - Depositor information  
3. **CUSTOMER** - Customer details
4. **ITEMS** - Product items
5. **ITEMS_STOCK** - Items stock levels
6. **DETAILED_STOCK** - Detailed stock with SSCC
7. **ORDER_STATUS** - Order status
8. **RECEIPT_STATUS** - Receipt status

### 2. Endpoint Mapping
**Location:** `C:\ProgramData\SqlSyncService\mapping.json`

**8 API Endpoints:**
- `http://localhost:8088/LogisticSites` → logisticSites array
- `http://localhost:8088/Depositors` → depositors array
- `http://localhost:8088/Customers` → customers array
- `http://localhost:8088/Items` → items array
- `http://localhost:8088/Stock` → itemsStock array
- `http://localhost:8088/DetailedStock` → detailedStock array
- `http://localhost:8088/Orders` → orders array
- `http://localhost:8088/Receipts` → receipts array

---

## 🧪 How to Test

### Option 1: Automated Test (Recommended)
Run the test script as **Administrator**:

```powershell
# Right-click PowerShell → Run as Administrator
cd C:\Users\IMOE001\Downloads\git\flow-mantis\flow-mantis
Set-ExecutionPolicy Bypass -Scope Process -Force
.\test-queries.ps1
```

This script will:
- ✓ Start the service
- ✓ Test all endpoints
- ✓ Verify JSON responses
- ✓ Count records returned
- ✓ Show a summary

### Option 2: Manual Test via Admin UI

1. **Open Admin UI:**
   ```
   C:\Users\IMOE001\Downloads\git\flow-mantis\flow-mantis\admin-fixed\SqlSyncService.Admin.exe
   ```

2. **Log in** (default admin credentials)

3. **Go to Queries tab** - You should see all 8 new queries

4. **Click Edit** on any query to verify:
   - Query name is correct
   - SQL matches your requirements
   - Pagination settings are appropriate

5. **Go to Database tab** and click **"Test Connection"**
   - This verifies database credentials work

### Option 3: Manual API Test

1. **Start the service** (as Administrator):
   ```powershell
   Start-Service SqlSyncService
   ```

2. **Get your API key** from Admin UI (Security tab → Show API Key)

3. **Test with PowerShell:**
   ```powershell
   $apiKey = "YOUR_API_KEY_HERE"
   $headers = @{ "X-API-Key" = $apiKey }
   
   # Test Logistic Sites
   $response = Invoke-RestMethod -Uri "http://localhost:8088/LogisticSites" -Headers $headers
   $response | ConvertTo-Json -Depth 10
   
   # Test Depositors
   $response = Invoke-RestMethod -Uri "http://localhost:8088/Depositors" -Headers $headers
   $response | ConvertTo-Json -Depth 10
   ```

4. **Test with curl:**
   ```bash
   curl -H "X-API-Key: YOUR_API_KEY_HERE" http://localhost:8088/LogisticSites
   ```

---

## 📋 Expected JSON Response Format

Each endpoint returns JSON with the mapped array name:

```json
{
  "logisticSites": [
    {
      "los_ID": 1,
      "los_Code": "WH01",
      "los_Description": "Main Warehouse"
    }
  ]
}
```

**With Pagination** (for paginated queries):
```json
{
  "depositors": [
    { "dep_ID": 1, "dep_Code": "AC", ... }
  ],
  "_pagination": {
    "hasMore": true,
    "nextPageUrl": "http://localhost:8088/Depositors?pageSize=100&pageNumber=2"
  }
}
```

---

## 🔍 Troubleshooting

### Service Won't Start
1. Check Windows Event Viewer → Application logs
2. Check service logs: `C:\ProgramData\SqlSyncService\logs`
3. Verify database is accessible from this machine

### "Invalid API Key" Error
1. Open Admin UI → Security tab
2. Click "Rotate API Key" to generate a new one
3. Copy the new key and use it in your requests

### Empty Results
- This is normal if your database tables don't have data matching the WHERE clauses
- Check the queries - some have filters like `prd_PrimaryCode = 'AC' and dep_Code = 'AC'`
- You may need to adjust these filters in the Admin UI

### Query Errors
1. Open Admin UI → Queries tab
2. Click Edit on the failing query
3. Test the SQL directly in SQL Server Management Studio
4. Adjust table names or column names if your schema is different

---

## 🎯 Next Steps

1. **Test all endpoints** using the automated script or manually
2. **Verify data is returned** correctly
3. **Adjust WHERE clauses** if needed (e.g., change `dep_Code = 'AC'` to match your data)
4. **Create final installer** with all fixes once testing is complete

---

## 📝 Notes

- The service runs on **port 8088** (configured in appsettings.json)
- API key authentication is **enabled by default**
- Database connection uses **encrypted credentials** (stored in appsettings.json)
- All queries with `(nolock)` hints are preserved as you requested

