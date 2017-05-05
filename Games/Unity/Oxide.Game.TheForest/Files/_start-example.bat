@echo off
cls
:start
echo Starting server...

TheForestDedicatedServer.exe -dedicated -servername "My Oxide Server" -serverplayers 8 -serverautosaveinterval 15

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
