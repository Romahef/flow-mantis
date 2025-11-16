#!/bin/bash

# SqlSyncService - SQL Server Docker Setup Script
# Run on macOS to start SQL Server in Docker

echo "================================================="
echo "  Starting SQL Server 2022 in Docker"
echo "================================================="
echo ""

# Configuration
SQL_PASSWORD="YourStrong@Passw0rd"
CONTAINER_NAME="sqlserver"
SQL_PORT=1433

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running!"
    echo ""
    echo "Please:"
    echo "1. Install Docker Desktop: https://www.docker.com/products/docker-desktop"
    echo "2. Start Docker Desktop"
    echo "3. Run this script again"
    echo ""
    exit 1
fi

echo "✓ Docker is running"
echo ""

# Check if container already exists
if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "📦 SQL Server container already exists"
    
    # Check if it's running
    if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        echo "✓ SQL Server is already running"
    else
        echo "🔄 Starting existing SQL Server container..."
        docker start $CONTAINER_NAME
        echo "✓ SQL Server started"
    fi
else
    echo "📥 Pulling SQL Server 2022 image (this may take a few minutes)..."
    docker pull mcr.microsoft.com/mssql/server:2022-latest
    
    echo ""
    echo "🚀 Starting SQL Server container..."
    docker run -e "ACCEPT_EULA=Y" \
        -e "MSSQL_SA_PASSWORD=$SQL_PASSWORD" \
        -p $SQL_PORT:1433 \
        --name $CONTAINER_NAME \
        --hostname sqlserver \
        -d mcr.microsoft.com/mssql/server:2022-latest
    
    echo "⏳ Waiting for SQL Server to start (15 seconds)..."
    sleep 15
fi

echo ""
echo "================================================="
echo "✅ SQL Server is ready!"
echo "================================================="
echo ""
echo "Connection Details:"
echo "  Server:   localhost,1433"
echo "  Username: sa"
echo "  Password: $SQL_PASSWORD"
echo ""
echo "To restore your database backup:"
echo "  1. Use the automated restore script:"
echo "     ./restore-database.sh /path/to/your-backup.bak"
echo ""
echo "  2. Or copy and restore manually:"
echo "     docker cp /path/to/your-backup.bak sqlserver:/var/opt/mssql/data/"
echo "     docker exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '$SQL_PASSWORD' -C -Q \"RESTORE DATABASE YourDB FROM DISK='/var/opt/mssql/data/your-backup.bak' WITH REPLACE\""
echo ""
echo "Useful commands:"
echo "  Stop:    docker stop sqlserver"
echo "  Start:   docker start sqlserver"
echo "  Restart: docker restart sqlserver"
echo "  Logs:    docker logs sqlserver"
echo "  Remove:  docker rm -f sqlserver"
echo ""

