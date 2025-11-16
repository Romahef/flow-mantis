# Tools for SqlSyncService

This directory contains helpful tools for development and testing.

## 📊 JSON Example Generator

Connects to a real SQL Server database and generates example JSON files for all queries.

### Quick Start

```bash
cd JsonExampleGenerator

# Generate examples from your database
dotnet run -- --connection "Server=localhost,1433;Database=WMS_Database;User Id=sa;Password=YourPass;TrustServerCertificate=True"
```

### Options

```bash
dotnet run -- --connection "<connection-string>" \
              --queries "../../config-samples/queries.json" \
              --output "./examples" \
              --max-rows 10
```

**Parameters:**
- `--connection` - SQL Server connection string (required)
- `--queries` - Path to queries.json (default: config-samples/queries.json)
- `--output` - Output directory (default: ./examples)
- `--max-rows` - Max rows per query (default: 10)

### What it Does

1. ✅ Loads all queries from queries.json
2. ✅ Tests database connection
3. ✅ Executes each query
4. ✅ Saves results as formatted JSON files
5. ✅ Shows sample data in console
6. ✅ Handles errors gracefully

### Output

For each query, generates a JSON file:

**Example: `Customers.json`**
```json
{
  "queryName": "Customers",
  "rowCount": 5,
  "maxRowsLimit": 10,
  "data": [
    {
      "cus_ID": 1,
      "cus_Code": "CUS001",
      "cus_Name": "Acme Corp",
      "cus_Email": "contact@acme.com",
      "cus_Active": true
    }
  ]
}
```

---

## 🐳 SQL Server on macOS

Use Docker to run SQL Server locally on your Mac for testing.

### Quick Setup

```bash
# 1. Start SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# 2. Create test database
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -Q "CREATE DATABASE WMS_Database;"

# 3. Load sample data (see SQL-Server-Docker-Setup.md)
```

### Complete Guide

See **[SQL-Server-Docker-Setup.md](SQL-Server-Docker-Setup.md)** for:
- Full installation instructions
- Sample database schema
- Sample data scripts
- Troubleshooting tips

---

## 📖 Usage Examples

### Example 1: Local Docker SQL Server

```bash
# Start Docker SQL Server
docker start sqlserver

# Generate JSON examples
cd JsonExampleGenerator
dotnet run -- --connection "Server=localhost,1433;Database=WMS_Database;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

### Example 2: Remote SQL Server

```bash
cd JsonExampleGenerator
dotnet run -- --connection "Server=your-server.com;Database=Production;User Id=readonly;Password=pass;TrustServerCertificate=True" \
              --max-rows 5
```

### Example 3: Custom Queries

```bash
# Use your own queries.json
dotnet run -- --connection "..." \
              --queries "/path/to/custom-queries.json" \
              --output "./my-examples"
```

---

## 🎯 Workflow

### Complete Testing Workflow

```bash
# 1. Setup (one-time)
cd /Users/roman/Sites/mantis-flow/flow-mantis/tools

# Start SQL Server
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Wait for SQL Server to start (about 10-15 seconds)
sleep 15

# 2. Create and populate database
# Run the SQL script from SQL-Server-Docker-Setup.md
# (Copy the CREATE DATABASE and INSERT statements)

# 3. Generate JSON examples
cd JsonExampleGenerator
dotnet run -- --connection "Server=localhost,1433;Database=WMS_Database;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"

# 4. View results
ls -la examples/
cat examples/Customers.json
```

---

## 📁 Generated Files

After running the generator, you'll have:

```
examples/
├── Warehouses.json          # Simple reference data
├── StockOwners.json         # Simple reference data
├── Customers.json           # Paginated customer data
├── Items.json               # Product information
├── StockItems.json          # Product with stock levels
├── Inventory.json           # Stock detail with SSCC
└── InventoryMovement.json   # Receipt/movement history
```

Each file contains:
- **queryName** - Name of the query
- **rowCount** - Number of rows returned
- **maxRowsLimit** - Configured limit
- **data** - Array of result rows (with actual data!)

---

## 🔧 Troubleshooting

### "Connection failed"

- Check SQL Server is running: `docker ps`
- Test connection: `docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -Q "SELECT @@VERSION"`
- Verify password and connection string

### "No data returned"

- Database might be empty
- Check table names match queries
- Run sample data scripts from SQL-Server-Docker-Setup.md

### "Error: Login failed"

- Password must meet complexity requirements:
  - At least 8 characters
  - Uppercase, lowercase, numbers, symbols
- Example: `YourStrong@Passw0rd`

### Build errors

```bash
# Restore dependencies
dotnet restore

# Rebuild
dotnet build
```

---

## 💡 Tips

1. **Start Small**: Test with `--max-rows 3` first
2. **Check Queries**: Review queries in config-samples/queries.json
3. **Sample Data**: Use the provided SQL scripts for realistic test data
4. **Documentation**: Generated JSON files are great for API documentation
5. **Version Control**: Commit example files to show expected API responses

---

## 🚀 Next Steps

1. **Generate examples** from your database
2. **Use in documentation** - show real API responses
3. **Update integration.json** - verify schema matches
4. **Test pagination** - check offset vs token modes
5. **Share with team** - example files help frontend developers

---

## 📚 Related Documentation

- **SQL Server Setup**: [SQL-Server-Docker-Setup.md](SQL-Server-Docker-Setup.md)
- **Queries Config**: [../config-samples/queries.json](../config-samples/queries.json)
- **Integration Schema**: [../src/SqlSyncService/integration.json](../src/SqlSyncService/integration.json)
- **Main README**: [../README.md](../README.md)
