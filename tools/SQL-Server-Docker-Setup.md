# SQL Server on macOS using Docker

This guide helps you set up SQL Server locally on macOS using Docker to test SqlSyncService.

## Prerequisites

- Docker Desktop for Mac installed
- At least 4GB RAM available for Docker

## Step 1: Install Docker Desktop

If not already installed:
```bash
# Download from https://www.docker.com/products/docker-desktop
# Or install via Homebrew
brew install --cask docker
```

## Step 2: Run SQL Server in Docker

```bash
# Pull SQL Server 2022 image
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Run SQL Server container
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name sqlserver \
  --hostname sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**Important:** Change `YourStrong@Passw0rd` to your own strong password!

**Password requirements:**
- At least 8 characters
- Contains uppercase, lowercase, numbers, and symbols

## Step 3: Verify SQL Server is Running

```bash
# Check container status
docker ps

# Should show:
# CONTAINER ID   IMAGE                                       STATUS
# xxxxxxxxxxxx   mcr.microsoft.com/mssql/server:2022-latest  Up X minutes
```

## Step 4: Connect to SQL Server

### Using Azure Data Studio (Recommended for Mac)

1. Download: https://docs.microsoft.com/en-us/sql/azure-data-studio/download
2. Connect with:
   - **Server:** localhost,1433
   - **Authentication:** SQL Login
   - **Username:** sa
   - **Password:** YourStrong@Passw0rd

### Using sqlcmd (Command Line)

```bash
# Install sqlcmd (if not already installed)
brew install sqlcmd

# Connect
sqlcmd -S localhost,1433 -U sa -P 'YourStrong@Passw0rd'
```

## Step 5: Create Sample Database

```bash
# Connect and create database
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -C \
  -Q "CREATE DATABASE WMS_Database;"
```

Or use this SQL script:

```sql
-- Create database
CREATE DATABASE WMS_Database;
GO

USE WMS_Database;
GO

-- Create Warehouses table
CREATE TABLE Warehouses (
    whs_ID INT PRIMARY KEY IDENTITY(1,1),
    whs_Code NVARCHAR(50) NOT NULL,
    whs_Name NVARCHAR(200) NOT NULL,
    whs_Address NVARCHAR(500),
    whs_Active BIT NOT NULL DEFAULT 1
);

-- Create StockOwners table
CREATE TABLE StockOwners (
    own_ID INT PRIMARY KEY IDENTITY(1,1),
    own_Code NVARCHAR(50) NOT NULL,
    own_Name NVARCHAR(200) NOT NULL,
    own_Active BIT NOT NULL DEFAULT 1
);

-- Create Customers table
CREATE TABLE Customers (
    cus_ID INT PRIMARY KEY IDENTITY(1,1),
    cus_Code NVARCHAR(50) NOT NULL,
    cus_Name NVARCHAR(200) NOT NULL,
    cus_Email NVARCHAR(200),
    cus_Phone NVARCHAR(50),
    cus_Address NVARCHAR(500),
    cus_Active BIT NOT NULL DEFAULT 1,
    cus_CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- Create Products table
CREATE TABLE Products (
    prd_ID INT PRIMARY KEY IDENTITY(1,1),
    prd_Code NVARCHAR(50) NOT NULL,
    prd_Name NVARCHAR(200) NOT NULL,
    prd_Description NVARCHAR(1000),
    prd_UnitPrice DECIMAL(18,2) NOT NULL,
    prd_Barcode NVARCHAR(100),
    prd_Weight DECIMAL(18,2),
    prd_Volume DECIMAL(18,2),
    prd_Active BIT NOT NULL DEFAULT 1
);

-- Create Stock table
CREATE TABLE Stock (
    stc_ID INT PRIMARY KEY IDENTITY(1,1),
    stc_SSCC NVARCHAR(50) NOT NULL,
    stc_ProductID INT NOT NULL,
    stc_WarehouseID INT NOT NULL,
    stc_Quantity INT NOT NULL,
    stc_Location NVARCHAR(50),
    stc_EntryDate DATETIME NOT NULL DEFAULT GETDATE(),
    stc_ExpiryDate DATETIME,
    FOREIGN KEY (stc_ProductID) REFERENCES Products(prd_ID),
    FOREIGN KEY (stc_WarehouseID) REFERENCES Warehouses(whs_ID)
);

-- Create Receipts table
CREATE TABLE Receipts (
    rct_ID INT PRIMARY KEY IDENTITY(1,1),
    rct_Code NVARCHAR(50) NOT NULL,
    rct_Type NVARCHAR(50) NOT NULL,
    rct_ProductID INT NOT NULL,
    rct_WarehouseID INT NOT NULL,
    rct_Quantity INT NOT NULL,
    rct_InputDate DATETIME NOT NULL DEFAULT GETDATE(),
    rct_Reference NVARCHAR(200),
    FOREIGN KEY (rct_ProductID) REFERENCES Products(prd_ID),
    FOREIGN KEY (rct_WarehouseID) REFERENCES Warehouses(whs_ID)
);

-- Insert sample data
INSERT INTO Warehouses (whs_Code, whs_Name, whs_Address, whs_Active) VALUES
('WH001', 'Main Warehouse', '123 Storage St, City', 1),
('WH002', 'North Depot', '456 North Ave, City', 1),
('WH003', 'South Facility', '789 South Rd, City', 1);

INSERT INTO StockOwners (own_Code, own_Name, own_Active) VALUES
('OWN001', 'Company A', 1),
('OWN002', 'Company B', 1),
('OWN003', 'Company C', 1);

INSERT INTO Customers (cus_Code, cus_Name, cus_Email, cus_Phone, cus_Address, cus_Active) VALUES
('CUS001', 'Acme Corp', 'contact@acme.com', '+1234567890', '100 Business Blvd', 1),
('CUS002', 'Global Traders', 'info@global.com', '+1234567891', '200 Trade Center', 1),
('CUS003', 'Local Store', 'orders@local.com', '+1234567892', '300 Retail Plaza', 1),
('CUS004', 'Big Box Retailer', 'wholesale@bigbox.com', '+1234567893', '400 Mall Dr', 1),
('CUS005', 'Online Market', 'support@online.com', '+1234567894', '500 Digital Way', 1);

INSERT INTO Products (prd_Code, prd_Name, prd_Description, prd_UnitPrice, prd_Barcode, prd_Weight, prd_Volume, prd_Active) VALUES
('PRD001', 'Widget A', 'Standard widget for general use', 19.99, '1234567890123', 1.5, 0.1, 1),
('PRD002', 'Widget B', 'Premium widget with extra features', 29.99, '1234567890124', 2.0, 0.15, 1),
('PRD003', 'Gadget X', 'High-tech gadget', 49.99, '1234567890125', 0.8, 0.05, 1),
('PRD004', 'Tool Set', 'Complete tool set', 89.99, '1234567890126', 5.0, 0.5, 1),
('PRD005', 'Component Y', 'Replacement component', 12.50, '1234567890127', 0.3, 0.02, 1);

INSERT INTO Stock (stc_SSCC, stc_ProductID, stc_WarehouseID, stc_Quantity, stc_Location, stc_ExpiryDate) VALUES
('SSCC001', 1, 1, 100, 'A-01-01', DATEADD(year, 2, GETDATE())),
('SSCC002', 1, 1, 50, 'A-01-02', DATEADD(year, 2, GETDATE())),
('SSCC003', 2, 1, 75, 'A-02-01', DATEADD(year, 2, GETDATE())),
('SSCC004', 3, 2, 200, 'B-01-01', DATEADD(year, 1, GETDATE())),
('SSCC005', 4, 2, 30, 'B-02-01', NULL),
('SSCC006', 5, 3, 500, 'C-01-01', DATEADD(year, 3, GETDATE()));

INSERT INTO Receipts (rct_Code, rct_Type, rct_ProductID, rct_WarehouseID, rct_Quantity, rct_Reference) VALUES
('RCT001', 'Purchase', 1, 1, 150, 'PO-2024-001'),
('RCT002', 'Purchase', 2, 1, 75, 'PO-2024-002'),
('RCT003', 'Transfer', 1, 2, -50, 'TRF-2024-001'),
('RCT004', 'Purchase', 3, 2, 200, 'PO-2024-003'),
('RCT005', 'Purchase', 4, 2, 30, 'PO-2024-004'),
('RCT006', 'Purchase', 5, 3, 500, 'PO-2024-005');

GO
```

## Step 6: Test with JSON Generator

```bash
cd /Users/roman/Sites/mantis-flow/flow-mantis/tools/JsonExampleGenerator

dotnet run -- --connection "Server=localhost,1433;Database=WMS_Database;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

## Managing the Container

### Stop SQL Server
```bash
docker stop sqlserver
```

### Start SQL Server
```bash
docker start sqlserver
```

### Remove SQL Server
```bash
docker rm -f sqlserver
```

### View logs
```bash
docker logs sqlserver
```

### Execute SQL scripts
```bash
# From a file
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -C \
  -i /path/to/script.sql

# Interactive mode
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' -C
```

## Connection String for SqlSyncService

Once your data is ready, use this connection string in your SqlSyncService configuration:

```json
{
  "Database": {
    "Server": "localhost",
    "Port": 1433,
    "Database": "WMS_Database",
    "UsernameEncrypted": "<encrypted-sa>",
    "PasswordEncrypted": "<encrypted-password>"
  }
}
```

## Troubleshooting

### Container won't start
- Check Docker Desktop is running
- Ensure port 1433 is not in use: `lsof -i :1433`
- Check Docker logs: `docker logs sqlserver`

### Can't connect
- Verify container is running: `docker ps`
- Check password meets complexity requirements
- Try: `docker restart sqlserver`

### Need more memory
```bash
# Stop and recreate with more memory
docker stop sqlserver
docker rm sqlserver
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name sqlserver \
  --memory="4g" \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

## Resources

- SQL Server Docker: https://hub.docker.com/_/microsoft-mssql-server
- Azure Data Studio: https://docs.microsoft.com/en-us/sql/azure-data-studio/
- T-SQL Reference: https://docs.microsoft.com/en-us/sql/t-sql/
