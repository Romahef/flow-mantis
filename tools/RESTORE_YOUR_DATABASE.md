# Restore Your Database on macOS

Complete guide to restore your SQL Server database backup on macOS using Docker.

---

## 📋 Prerequisites

1. Your SQL Server database backup file (`.bak`)
2. Docker Desktop installed on Mac

---

## 🚀 Quick Start

### Step 1: Install Docker Desktop

```bash
# Download from: https://www.docker.com/products/docker-desktop
# Or install via Homebrew:
brew install --cask docker
```

**After installation:**
- Open Docker Desktop application
- Wait for it to start (whale icon in menu bar)

---

### Step 2: Start SQL Server

```bash
cd /Users/roman/Sites/mantis-flow/flow-mantis/tools

# Run the helper script
./start-sql-server.sh
```

This will:
- ✅ Pull SQL Server 2022 image
- ✅ Start SQL Server container
- ✅ Configure with default password: `YourStrong@Passw0rd`

**Connection Details:**
- **Server:** `localhost,1433`
- **Username:** `sa`
- **Password:** `YourStrong@Passw0rd`

---

## 📦 Step 3: Restore Your Database Backup

### Option A: Using the Restore Script (Easiest)

I'll create a restore script for you. Just tell me:
1. Path to your .bak file
2. Original database name
3. What to name it locally

### Option B: Manual Steps

**1. Copy your backup file to Docker container:**

```bash
# Replace /path/to/your-backup.bak with your actual backup file location
docker cp /path/to/your-backup.bak sqlserver:/var/opt/mssql/data/
```

**2. Check what's inside the backup:**

```bash
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE FILELISTONLY FROM DISK='/var/opt/mssql/data/your-backup.bak'"
```

This shows the logical file names (you'll need them for the next step).

**3. Restore the database:**

```bash
# Basic restore (if logical names match)
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE DATABASE YourDBName FROM DISK='/var/opt/mssql/data/your-backup.bak' WITH REPLACE"

# Or with explicit file names (if needed)
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE DATABASE YourDBName FROM DISK='/var/opt/mssql/data/your-backup.bak' WITH MOVE 'LogicalDataName' TO '/var/opt/mssql/data/YourDB.mdf', MOVE 'LogicalLogName' TO '/var/opt/mssql/data/YourDB_log.ldf', REPLACE"
```

**4. Verify the restore:**

```bash
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "SELECT name FROM sys.databases"
```

---

## 🎯 Step 4: Generate JSON Examples

Once your database is restored:

```bash
cd /Users/roman/Sites/mantis-flow/flow-mantis/tools/JsonExampleGenerator

# Replace YourDBName with your actual database name
dotnet run -- --connection "Server=localhost,1433;Database=YourDBName;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

This will generate JSON files from your **real data**! 🎉

---

## 📝 Example: Complete Workflow

Let's say you have a backup at `/Users/roman/Downloads/MantisDB.bak`:

```bash
# 1. Start SQL Server
cd /Users/roman/Sites/mantis-flow/flow-mantis/tools
./start-sql-server.sh

# 2. Copy backup to container
docker cp /Users/roman/Downloads/MantisDB.bak sqlserver:/var/opt/mssql/data/

# 3. Check backup contents
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE FILELISTONLY FROM DISK='/var/opt/mssql/data/MantisDB.bak'"

# 4. Restore database
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE DATABASE MantisDB FROM DISK='/var/opt/mssql/data/MantisDB.bak' WITH REPLACE"

# 5. Verify
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "SELECT name FROM sys.databases"

# 6. Generate JSON examples
cd JsonExampleGenerator
dotnet run -- --connection "Server=localhost,1433;Database=MantisDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

---

## 🛠️ Troubleshooting

### "Docker is not running"

- Make sure Docker Desktop is started
- Check the whale icon in your menu bar
- Try: `docker ps` to verify

### "Cannot connect to SQL Server"

```bash
# Check if container is running
docker ps

# Check logs
docker logs sqlserver

# Restart container
docker restart sqlserver
```

### "Backup restore fails"

```bash
# Check backup file is in container
docker exec sqlserver ls -la /var/opt/mssql/data/

# Try simple restore first
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE DATABASE TestDB FROM DISK='/var/opt/mssql/data/your-backup.bak'"
```

### "Database name conflicts"

If a database with the same name already exists:

```bash
# Drop existing database first
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "DROP DATABASE YourDBName"

# Then restore
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE DATABASE YourDBName FROM DISK='/var/opt/mssql/data/your-backup.bak'"
```

---

## 🎓 SQL Server Management

### Connect with Azure Data Studio

1. Download: https://docs.microsoft.com/en-us/sql/azure-data-studio/download
2. Connect:
   - **Server:** `localhost,1433`
   - **Authentication:** SQL Login
   - **Username:** `sa`
   - **Password:** `YourStrong@Passw0rd`

### Useful Docker Commands

```bash
# Stop SQL Server
docker stop sqlserver

# Start SQL Server
docker start sqlserver

# Restart SQL Server
docker restart sqlserver

# View logs
docker logs sqlserver

# Interactive SQL prompt
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C

# Execute SQL file
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -i /path/to/script.sql

# Backup database
docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "BACKUP DATABASE YourDB TO DISK='/var/opt/mssql/data/YourDB-backup.bak'"

# Copy backup out of container
docker cp sqlserver:/var/opt/mssql/data/YourDB-backup.bak /Users/roman/Downloads/
```

---

## 🔐 Security Notes

- The password `YourStrong@Passw0rd` is for **local development only**
- SQL Server is only accessible from your Mac (localhost)
- Not exposed to network by default
- For production, use a stronger password

---

## 📞 Need Help?

Just tell me:
1. **Where is your .bak file?** (full path)
2. **What's the database name?**
3. Any error messages you see

And I'll help you restore it! 🚀
