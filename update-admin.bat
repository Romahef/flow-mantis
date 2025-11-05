@echo off
echo Updating Admin App...
taskkill /IM SqlSyncService.Admin.exe /F >nul 2>&1
timeout /t 2 /nobreak >nul
xcopy /Y /E "src\SqlSyncService.Admin\bin\Release\net8.0-windows\*.*" "C:\Program Files\SqlSyncService\Admin\"
echo Done! You can now run the Admin app normally.
pause


