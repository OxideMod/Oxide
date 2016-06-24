@echo off
cls
:start
echo Starting server...

Hurtworld.exe -batchmode -nographics -exec "host 12871;queryport 12881;maxplayers 100;servername My Oxide Server"

echo.
echo Restarting server...
echo.
goto start
