@echo off
echo Closing old Admin app instances...
taskkill /IM SqlSyncService.Admin.exe /F >nul 2>&1
timeout /t 2 /nobreak >nul
echo Starting updated Admin app...
start "" "%~dp0admin-fixed\SqlSyncService.Admin.exe"
echo Done! The updated Admin app should now be running.
pause


