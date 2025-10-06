@echo off
:: Quick Fix - Update Admin UI with password fix
:: Run as Administrator

echo.
echo ========================================
echo   Updating Admin UI (Password Fix)
echo ========================================
echo.

:: Check for admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Run as Administrator!
    pause
    exit /b 1
)

echo Updating Admin UI files...
xcopy /E /I /Y "publish-installer\admin\*" "C:\Program Files\SqlSyncService\Admin\"

if %errorLevel% equ 0 (
    echo.
    echo ========================================
    echo   SUCCESS! Admin UI Updated!
    echo ========================================
    echo.
    echo The password validation is now fixed!
    echo.
    echo Try opening the Admin UI again and entering
    echo the password you chose during installation.
    echo.
    echo It should work now!
) else (
    echo.
    echo ERROR: Failed to update files!
)

echo.
pause
