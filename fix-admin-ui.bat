@echo off
:: Fix Admin UI Login Issue - Run as Administrator
:: This updates the Admin UI files in your current installation

echo.
echo ========================================
echo   Fixing Admin UI Login Issue
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

echo Updating Admin UI files...
echo.

xcopy /E /I /Y "publish-installer\admin\*" "C:\Program Files\SqlSyncService\Admin\"

if %errorLevel% equ 0 (
    echo.
    echo ========================================
    echo   SUCCESS! Admin UI has been updated!
    echo ========================================
    echo.
    echo The login window should now accept input.
    echo Try opening the Admin UI again!
    echo.
) else (
    echo.
    echo ========================================
    echo   ERROR: Failed to update files!
    echo ========================================
    echo.
    echo Make sure SqlSyncService is installed.
    echo.
)

pause
