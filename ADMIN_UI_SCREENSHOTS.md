# Admin Desktop Application - UI Guide

## 🖥️ Login Window

```
┌───────────────────────────────────────────┐
│  SqlSyncService - Admin Login             │
├───────────────────────────────────────────┤
│                                           │
│         🔐 Admin Login                    │
│    SqlSyncService Administration         │
│                                           │
│    Admin Passphrase:                      │
│    [●●●●●●●●●●●●●●●●●●●●●●●●●●]          │
│                                           │
│              [   Login   ]                │
│                                           │
└───────────────────────────────────────────┘
```

**Features:**
- Simple, secure login
- Password masking
- Enter key to submit
- Error messages displayed inline

---

## 🏠 Main Window

```
┌────────────────────────────────────────────────────────────────────────────┐
│  SqlSyncService Administration                                        [_][□][X] │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  SqlSyncService Administration                                            │
│  Manage service configuration, queries, and security settings             │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│  [🔒 Security] [🗄️ Database] [📝 Queries] [🔗 Mapping] [ℹ️ About]       │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  TAB CONTENT AREA                                                          │
│  (Changes based on selected tab)                                          │
│                                                                            │
│                                                                            │
│                                                                            │
│                                                                            │
│                                                                            │
│                                                                            │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│  💾 Remember to restart the service after saving changes                  │
│                                               [💾 Save Configuration]     │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 🔒 Security Tab

```
┌────────────────────────────────────────────────────────────────┐
│  Security Configuration                                        │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ API Key                                                   ││
│  │                                                           ││
│  │ Current API key is encrypted for security.               ││
│  │                                                           ││
│  │ [🔄 Generate New API Key]                                ││
│  └──────────────────────────────────────────────────────────┘│
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ IP Allow List                                             ││
│  │                                                           ││
│  │ Only requests from these IPs will be accepted.           ││
│  │                                                           ││
│  │ ┌────────────────────────────────────────────────────┐  ││
│  │ │ • 203.0.113.10                                      │  ││
│  │ │ • 192.168.1.100                                     │  ││
│  │ └────────────────────────────────────────────────────┘  ││
│  │                                                           ││
│  │ [192.168.1.___]  [Add IP]  [Remove]                      ││
│  └──────────────────────────────────────────────────────────┘│
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ HTTPS Certificate                                         ││
│  │                                                           ││
│  │ Certificate Path (.pfx):                                 ││
│  │ [C:\ProgramData\SqlSyncService\certs\server.pfx] [Browse]││
│  │                                                           ││
│  │ [✓ Validate Certificate]                                 ││
│  └──────────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Database Tab

```
┌────────────────────────────────────────────────────────────────┐
│  Database Configuration                                        │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ Server:              Port:                                ││
│  │ [localhost    ]      [1433]                               ││
│  │                                                           ││
│  │ Database Name:                                            ││
│  │ [WMS_Database                          ]                  ││
│  │                                                           ││
│  │ Instance (optional):                                      ││
│  │ [                                      ]                  ││
│  │                                                           ││
│  │ ⚠️ Database credentials are stored encrypted using        ││
│  │    Windows DPAPI. Changes require service restart.       ││
│  │                                                           ││
│  │ [🔌 Test Database Connection]                            ││
│  └──────────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────────────┘
```

---

## 📝 Queries Tab

```
┌────────────────────────────────────────────────────────────────────────────┐
│  Query Definitions                                                         │
│  Define SQL queries that will be executed via API endpoints.              │
│                                                                            │
│  [➕ Add New Query]                                                        │
│                                                                            │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │ Name          │ Paginable │ Mode   │ OrderBy/KeyColumns │ Actions  │  │
│  ├────────────────────────────────────────────────────────────────────┤  │
│  │ Warehouses    │ □         │ -      │ -                  │[Edit][Del]│  │
│  │ Customers     │ ☑         │ Offset │ cus_ID             │[Edit][Del]│  │
│  │ Items         │ ☑         │ Offset │ prd_ID             │[Edit][Del]│  │
│  │ Inventory     │ ☑         │ Token  │ EntryDate, SSCC    │[Edit][Del]│  │
│  │ ...           │           │        │                    │           │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## ✏️ Query Editor Dialog

```
┌────────────────────────────────────────────────────────────────┐
│  Query Editor                                          [_][□][X] │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Query Name:                                                   │
│  [Customers                          ]                         │
│                                                                │
│  [☑ Enable Pagination]  Pagination Mode: [Offset ▼]           │
│                                                                │
│  Order By Column:                                              │
│  [cus_ID                             ]                         │
│  Column to use for ROW_NUMBER() ordering                       │
│                                                                │
│  SQL Query:                                                    │
│  ┌──────────────────────────────────────────────────────────┐│
│  │SELECT cus_ID, cus_Code, cus_Name,                        ││
│  │       cus_Email, cus_Active                              ││
│  │FROM Customers                                            ││
│  │WHERE cus_Active = 1                                      ││
│  │ORDER BY cus_ID                                           ││
│  │                                                          ││
│  │                                                          ││
│  └──────────────────────────────────────────────────────────┘│
│                                                                │
│                                      [Cancel]  [Save]          │
└────────────────────────────────────────────────────────────────┘
```

**Features:**
- Monospace font for SQL
- Multi-line editing
- Pagination mode selector
- Field visibility based on selection
- Validation before save

---

## 🔗 Mapping Tab

```
┌────────────────────────────────────────────────────────────────┐
│  Endpoint Mapping                                              │
│  Map queries to API endpoints and specify target JSON arrays.  │
│                                                                │
│  ⚠️ Mappings are validated against integration.json at startup │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ Endpoint: Warehouses                                      ││
│  │   • Warehouses → warehouses                               ││
│  │                                                           ││
│  │ Endpoint: Customers                                       ││
│  │   • Customers → customers                                 ││
│  │                                                           ││
│  │ Endpoint: Items                                           ││
│  │   • Items → items                                         ││
│  │   • StockItems → stockItems                               ││
│  │                                                           ││
│  │ Endpoint: Inventory                                       ││
│  │   • Inventory → stockDetail                               ││
│  │   • InventoryMovement → inventoryMovements                ││
│  └──────────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────────────┘
```

---

## ℹ️ About Tab

```
┌────────────────────────────────────────────────────────────────┐
│  About SqlSyncService                                          │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐│
│  │ Version: 1.0.0                                            ││
│  │ Platform: .NET 8.0 Windows Service                        ││
│  │ Listen Address: https://0.0.0.0:8443                      ││
│  │ Config Directory: C:\ProgramData\SqlSyncService           ││
│  │                                                           ││
│  │ ───────────────────────────────────────────────────────  ││
│  │                                                           ││
│  │ Features:                                                 ││
│  │ ✓ HTTPS API on port 8443                                 ││
│  │ ✓ SQL Server connectivity with SQL Login                 ││
│  │ ✓ IP allow-list and API key authentication               ││
│  │ ✓ DPAPI encryption for secrets                           ││
│  │ ✓ Offset and token-based pagination                      ││
│  │ ✓ JSON streaming for large datasets                      ││
│  └──────────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────────────┘
```

---

## 🎨 Color Scheme

**Primary Colors:**
- 🔵 Primary Blue: `#2563eb` (buttons, accents)
- 🟢 Success Green: `#059669` (save, success messages)
- 🔴 Danger Red: `#dc2626` (delete, errors)
- 🟡 Warning Amber: `#f59e0b` (warnings)

**Neutral Colors:**
- ⚪ Background: `#f5f5f5` (light gray)
- ⬜ Surface: `#ffffff` (white cards)
- ⬛ Text Primary: `#1f2937` (dark gray)
- ◻️ Text Secondary: `#6b7280` (medium gray)

---

## 🎯 Key UI Elements

### Buttons
- **Primary Button:** Blue background, white text, rounded corners
- **Success Button:** Green background, white text
- **Danger Button:** Red background, white text
- **Hover Effect:** Slightly darker shade

### Input Fields
- **Border:** Light gray (`#d1d5db`)
- **Focus:** Blue border (`#2563eb`)
- **Padding:** 10px
- **Font:** 14px

### Cards/Panels
- **Background:** White
- **Border Radius:** 8px
- **Padding:** 20px
- **Shadow:** Subtle drop shadow

### Alerts
- **Success:** Green background with white text
- **Error:** Red background with white text
- **Dismissible:** X button in top-right

---

## 📱 Responsive Design

The application is designed for:
- **Minimum Resolution:** 1024×768
- **Recommended:** 1920×1080
- **Scaling:** DPI-aware, looks good on high-DPI displays

---

## ⌨️ Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Enter` | Submit forms (login, add IP, save query) |
| `Esc` | Close dialogs |
| `Tab` | Navigate between fields |
| `Ctrl+S` | Save configuration (when implemented) |
| `F5` | Refresh (when implemented) |

---

## 🖱️ Mouse Interactions

| Action | Result |
|--------|--------|
| Click button | Execute action |
| Double-click query row | Open editor |
| Right-click (future) | Context menu |
| Hover button | Show darker shade |

---

## ✨ Visual Feedback

**Actions provide immediate feedback:**
- ✅ **Success:** Green alert bar appears
- ❌ **Error:** Red alert bar + error dialog
- ⏳ **Loading:** Button disabled during operations
- 🎯 **Validation:** Real-time field validation

---

**This is a modern, professional Windows desktop application that provides an intuitive interface for managing SqlSyncService!**
