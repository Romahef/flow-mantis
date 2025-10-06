@echo off
:: Restart SQL Server and enable Mixed Mode Authentication
:: Right-click this file and select "Run as administrator"

echo.
echo ========================================
echo   Restarting SQL Server (SQLEXPRESS)
echo ========================================
echo.

:: Check for admin privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo.
    echo Right-click this file and select "Run as administrator"
    echo.
    pause
    exit /b 1
)

echo [1/3] Stopping SQL Server...
net stop MSSQL$SQLEXPRESS
timeout /t 3 /nobreak >nul

echo [2/3] Starting SQL Server...
net start MSSQL$SQLEXPRESS
timeout /t 3 /nobreak >nul

echo [3/3] Testing connection...
echo.

sqlcmd -S "localhost\SQLEXPRESS" -d "SqlSyncTest" -U "sqlsync_user" -P "SqlSync@2024!Strong" -Q "SELECT 'Connection Successful!' AS Result, DB_NAME() AS Database, SYSTEM_USER AS User" -W

if %errorLevel% equ 0 (
    echo.
    echo ========================================
    echo   SUCCESS! SQL Server is ready!
    echo ========================================
    echo.
    echo You can now run the SqlSyncService installer!
    echo Use these credentials:
    echo   Server: localhost\SQLEXPRESS
    echo   Database: SqlSyncTest
    echo   Username: sqlsync_user
    echo   Password: SqlSync@2024!Strong
    echo.
) else (
    echo.
    echo ========================================
    echo   ERROR: Connection test failed!
    echo ========================================
    echo.
)

pause
