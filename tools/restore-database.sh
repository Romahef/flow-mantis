#!/bin/bash

# SqlSyncService - Database Restore Helper Script
# Restores a SQL Server backup file to Docker container

echo "================================================="
echo "  SQL Server Database Restore Tool"
echo "================================================="
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running!"
    echo "Please start Docker Desktop and try again."
    exit 1
fi

# Check if SQL Server container is running
if ! docker ps --format '{{.Names}}' | grep -q "^sqlserver$"; then
    echo "❌ SQL Server container is not running!"
    echo ""
    echo "Please start it first:"
    echo "  ./start-sql-server.sh"
    echo ""
    exit 1
fi

# Get backup file path
if [ -z "$1" ]; then
    echo "Usage: ./restore-database.sh <path-to-backup.bak> [database-name]"
    echo ""
    echo "Example:"
    echo "  ./restore-database.sh ~/Downloads/MyDB.bak MantisDB"
    echo ""
    exit 1
fi

BACKUP_FILE="$1"
BACKUP_FILENAME=$(basename "$BACKUP_FILE")

# Check if backup file exists
if [ ! -f "$BACKUP_FILE" ]; then
    echo "❌ Backup file not found: $BACKUP_FILE"
    exit 1
fi

echo "✓ Found backup file: $BACKUP_FILE"
echo ""

# Copy backup to container
echo "📦 Copying backup to SQL Server container..."
docker cp "$BACKUP_FILE" sqlserver:/var/opt/mssql/data/
echo "✓ Backup copied"
echo ""

# Get logical file names from backup
echo "🔍 Analyzing backup file..."
echo ""
FILELISTONLY=$(docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "RESTORE FILELISTONLY FROM DISK='/var/opt/mssql/data/$BACKUP_FILENAME'" -h -1 -W -s"|")

# Extract first logical names (typically the database files)
LOGICAL_DATA_NAME=$(echo "$FILELISTONLY" | head -1 | awk -F'|' '{print $1}' | xargs)
LOGICAL_LOG_NAME=$(echo "$FILELISTONLY" | sed -n '2p' | awk -F'|' '{print $1}' | xargs)

echo "Found logical files:"
echo "  Data: $LOGICAL_DATA_NAME"
echo "  Log:  $LOGICAL_LOG_NAME"
echo ""

# Determine database name
if [ -z "$2" ]; then
    # Use logical data name as database name
    DB_NAME="$LOGICAL_DATA_NAME"
else
    DB_NAME="$2"
fi

echo "📂 Database name: $DB_NAME"
echo ""

# Check if database already exists
EXISTING=$(docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "SELECT name FROM sys.databases WHERE name = '$DB_NAME'" -h -1 | xargs)

if [ ! -z "$EXISTING" ]; then
    echo "⚠️  Database '$DB_NAME' already exists!"
    echo ""
    read -p "Do you want to replace it? (yes/no): " CONFIRM
    
    if [ "$CONFIRM" != "yes" ]; then
        echo "❌ Restore cancelled"
        exit 1
    fi
    
    echo ""
    echo "🗑️  Dropping existing database..."
    docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DB_NAME]"
    echo "✓ Existing database removed"
    echo ""
fi

# Restore database
echo "🚀 Restoring database (this may take a few minutes)..."
echo ""

RESTORE_SQL="RESTORE DATABASE [$DB_NAME] FROM DISK='/var/opt/mssql/data/$BACKUP_FILENAME' WITH MOVE '$LOGICAL_DATA_NAME' TO '/var/opt/mssql/data/${DB_NAME}.mdf', MOVE '$LOGICAL_LOG_NAME' TO '/var/opt/mssql/data/${DB_NAME}_log.ldf', REPLACE"

if docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q "$RESTORE_SQL"; then
    echo ""
    echo "================================================="
    echo "✅ Database restored successfully!"
    echo "================================================="
    echo ""
    echo "Database: $DB_NAME"
    echo "Server:   localhost,1433"
    echo "Username: sa"
    echo "Password: YourStrong@Passw0rd"
    echo ""
    echo "Next step: Generate JSON examples"
    echo ""
    echo "  cd JsonExampleGenerator"
    echo "  dotnet run -- --connection \"Server=localhost,1433;Database=$DB_NAME;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True\""
    echo ""
else
    echo ""
    echo "❌ Restore failed!"
    echo ""
    echo "Troubleshooting:"
    echo "1. Check backup file is valid"
    echo "2. Try viewing backup contents:"
    echo "   docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd' -C -Q \"RESTORE FILELISTONLY FROM DISK='/var/opt/mssql/data/$BACKUP_FILENAME'\""
    echo ""
    echo "3. Check SQL Server logs:"
    echo "   docker logs sqlserver"
    echo ""
    exit 1
fi

