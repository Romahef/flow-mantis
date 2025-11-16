#!/bin/bash

# Quick restore helper - edit the path below to your .bak file location
# Example: BACKUP_FILE="/Users/roman/Downloads/MyDatabase.bak"

BACKUP_FILE="YOUR_BACKUP_PATH_HERE"

if [ "$BACKUP_FILE" == "YOUR_BACKUP_PATH_HERE" ]; then
    echo "⚠️  Please edit this file and set your backup file path!"
    echo ""
    echo "1. Open: tools/quick-restore.sh"
    echo "2. Change: BACKUP_FILE=\"YOUR_BACKUP_PATH_HERE\""
    echo "3. To:     BACKUP_FILE=\"/path/to/your/backup.bak\""
    echo "4. Save and run: ./tools/quick-restore.sh"
    echo ""
    exit 1
fi

cd "$(dirname "$0")"
./restore-database.sh "$BACKUP_FILE"

