@echo off
cls
:start
echo Starting server...

rust_server.exe -batchmode -ip 0.0.0.0 -port 28015 -maxplayers 10 -hostname "My Oxide Server"

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
