# SqlSyncService API - Postman Guide

## 🚀 Quick Start

### 1. Import the Collection into Postman

1. Open Postman
2. Click **Import** (top left)
3. Select `SqlSyncService-Postman-Collection.json`
4. Click **Import**

### 2. Configure Your API Key

After importing, you need to set your API key:

1. Click on the **SqlSyncService API** collection
2. Go to the **Variables** tab
3. Find `apiKey` variable
4. Set the **CURRENT VALUE** to your actual API key
5. Click **Save**

**To get your API key:**
- Run the Admin app: `C:\Program Files\SqlSyncService\Admin\SqlSyncService.Admin.exe`
- Go to Security tab
- Click "Generate New API Key" (or use existing key from config)

### 3. Set Your Base URL

The collection has two URL options:

**Local Access (same computer):**
- Variable: `baseUrl`
- Current Value: `http://localhost:8088`

**External Access (from outside network):**
- Variable: `baseUrl`  
- Current Value: `http://YOUR_PUBLIC_IP:8088`
- ✅ Port 8088 is already forwarded to this computer

---

## 📋 Available Endpoints

### 1. **LogisticSites** (Non-Paginable)
```
GET /api/LogisticSites
```
Returns all logistic sites.

**Response:**
```json
{
  "arrays": {
    "logisticSites": [
      {
        "los_ID": 1,
        "los_Code": "MAIN",
        "los_Description": "Main Warehouse"
      }
    ]
  }
}
```

---

### 2. **Depositors** (Offset Pagination)
```
GET /api/Depositors
GET /api/Depositors?offset=0&limit=50
```

**Pagination Parameters:**
- `offset` (optional): Starting row number (default: 0)
- `limit` (optional): Number of rows per page (default: 100)

**Response:**
```json
{
  "arrays": {
    "depositors": [
      {
        "dep_ID": 1,
        "dep_Code": "AC",
        "cmp_FullName": "Company Name",
        "cmp_Address": "Address",
        "cmp_City": "City",
        "cmp_ZipCode": "12345",
        "cmp_Phone": "123-456-7890",
        "cmp_Responsible": "Manager Name",
        "cmp_Fax": "123-456-7891",
        "cmp_AFM": "123456789",
        "cmp_DUNSNumber": null,
        "cmp_LastUpdateTime": "2025-01-15T10:30:00"
      }
    ]
  },
  "metadata": {
    "totalRows": 150,
    "offset": 0,
    "limit": 50,
    "hasMore": true
  }
}
```

**How to paginate:**
1. First page: `?offset=0&limit=50`
2. Second page: `?offset=50&limit=50`
3. Third page: `?offset=100&limit=50`

---

### 3. **Customers** (Offset Pagination)
```
GET /api/Customers
GET /api/Customers?offset=0&limit=100
```

Same pagination as Depositors.

**Response includes:**
- Customer details
- Delivery addresses
- Associated depositor info

---

### 4. **Items/Products** (Offset Pagination)
```
GET /api/Items
GET /api/Items?offset=0&limit=200
```

**Response:**
```json
{
  "arrays": {
    "items": [
      {
        "dep_ID": 1,
        "dep_code": "AC",
        "prd_ID": 123,
        "prd_PrimaryCode": "ITEM001",
        "prd_SecondaryCode": "SEC001",
        "prdl_Description": "Product Name",
        "prdl_ShortDescription": "Short Name"
      }
    ]
  },
  "metadata": {
    "totalRows": 5000,
    "offset": 0,
    "limit": 200,
    "hasMore": true
  }
}
```

---

### 5. **Stock** (Offset Pagination)
```
GET /api/Stock
GET /api/Stock?offset=0&limit=100
```

**Response includes:**
- Current stock levels (PC_QTY, CRT_QTY)
- Free quantities
- Pending orders/receipts
- Safety stock levels

---

### 6. **DetailedStock** (Offset Pagination)
```
GET /api/DetailedStock
GET /api/DetailedStock?offset=0&limit=100
```

**Response includes:**
- SSCC (container) details
- Days in stock
- Unit information
- Location details

---

### 7. **Orders** (Token Pagination)
```
GET /api/Orders
GET /api/Orders?limit=50
GET /api/Orders?limit=50&token=NEXT_TOKEN
```

**Token Pagination Parameters:**
- `limit` (optional): Number of rows per page (default: 100)
- `token` (optional): Token from previous response

**Response:**
```json
{
  "arrays": {
    "orders": [
      {
        "dep_ID": 1,
        "dep_Code": "AC",
        "ord_Code": "ORD-2025-001",
        "ord_StatusID": 1,
        "msg_Greek": "Ενεργή",
        "ord_InputDate": "2025-01-15T08:00:00",
        "orc_Code": "CUST001",
        "orc_FullName": "Customer Name",
        "orc_Address": "Delivery Address",
        "orc_City": "Athens"
      }
    ]
  },
  "pagination": {
    "limit": 50,
    "hasMore": true,
    "nextToken": "eyJvcmRfSW5wdXREYXRlIjoiMjAyNS0wMS0xNVQwODowMDowMCIsIm9yZF9Db2RlIjoiT1JELTQ1In0="
  }
}
```

**How to paginate with tokens:**
1. First page: `GET /api/Orders?limit=50`
2. Get `nextToken` from response
3. Next page: `GET /api/Orders?limit=50&token={nextToken}`
4. Repeat until `hasMore` is false

---

### 8. **Receipts** (Token Pagination)
```
GET /api/Receipts
GET /api/Receipts?limit=50
GET /api/Receipts?limit=50&token=NEXT_TOKEN
```

Same token pagination as Orders.

**Response includes:**
- Receipt code and status
- Progress status (Greek description)
- Input and update timestamps

---

## 🔑 Authentication

All requests require the `X-API-Key` header:

```
X-API-Key: YOUR_API_KEY_HERE
```

**Troubleshooting:**
- **401 Unauthorized** - Check your API key is correct
- **403 Forbidden** - Your IP may not be in the allow list (current: 127.0.0.1, ::1)

---

## 🌐 Network Access

### Local Access
```
http://localhost:8088/api/LogisticSites
```

### External Access
✅ **Port 8088 is forwarded to this computer**

Find your public IP:
```powershell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content
```

Then use:
```
http://YOUR_PUBLIC_IP:8088/api/LogisticSites
```

### Adding External IPs to Allow List

If you need to allow access from specific external IPs:

1. Open Admin app
2. Go to **Security** tab
3. Add the external IP to **IP Allow List**
4. Click **Save Configuration**
5. Restart the service

---

## 📊 Response Formats

### Success Response
```json
{
  "arrays": {
    "targetArray": [ /* data */ ]
  },
  "metadata": { /* pagination info for Offset mode */ },
  "pagination": { /* pagination info for Token mode */ }
}
```

### Error Response
```json
{
  "error": "Error.Type",
  "message": "Human readable error message",
  "timestamp": "2025-11-04T14:30:00Z"
}
```

**Common Error Codes:**
- `Unauthorized.ApiKey` - Missing or invalid API key
- `Unauthorized.IpAddress` - IP not in allow list
- `BadRequest.InvalidParameters` - Invalid pagination parameters
- `InternalServerError.Database` - Database query error

---

## 💡 Tips

### Performance
- Use pagination for large datasets
- Typical page sizes: 50-200 rows
- Token pagination is more efficient for time-series data (Orders, Receipts)

### Testing Sequence
1. Start with **LogisticSites** (simple, no pagination)
2. Test **Depositors** with offset pagination
3. Test **Orders** with token pagination
4. Verify authentication with wrong API key (should get 401)

### Monitoring
Check service logs at:
```
C:\ProgramData\SqlSyncService\logs\
```

---

## 🔧 Troubleshooting

### Service Not Responding
```powershell
# Check if service is running
Get-Service SqlSyncService

# Start service
Start-Service SqlSyncService

# View logs
Get-Content "C:\ProgramData\SqlSyncService\logs\sqlsync-*.log" -Tail 50
```

### Test Connection
```powershell
# Test from localhost
Invoke-WebRequest -Uri "http://localhost:8088/api/LogisticSites" -Headers @{"X-API-Key"="YOUR_KEY"}

# Test from external
Invoke-WebRequest -Uri "http://YOUR_PUBLIC_IP:8088/api/LogisticSites" -Headers @{"X-API-Key"="YOUR_KEY"}
```

---

## 📞 Support

- **Config Location:** `C:\ProgramData\SqlSyncService\`
- **Admin Tool:** `C:\Program Files\SqlSyncService\Admin\SqlSyncService.Admin.exe`
- **Service Port:** 8088 (HTTP)
- **Admin Port:** 9443 (HTTPS)


