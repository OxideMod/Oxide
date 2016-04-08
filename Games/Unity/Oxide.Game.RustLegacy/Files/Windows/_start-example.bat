@echo off
cls
:start
echo Starting server...

rust_server -batchmode -ip 0.0.0.0 -port 28015 -maxplayers 10 -hostname "My Oxide Server"

echo.
echo Restarting server...
echo.
goto start
