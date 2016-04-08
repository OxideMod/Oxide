@echo off
cls
:start
echo Starting server...

Hurtworld -batchmode -nographics -exec "host 12871;queryport 12881;maxplayers 10;servername My Oxide Server"

echo.
echo Restarting server...
echo.
goto start
