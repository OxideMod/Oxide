@echo off
cls
:start

Unturned -batchmode -nographics -name "My Oxide Server" -bind 0.0.0.0 -port 25444 -maxplayers 10 -map pei +secureserver "Oxide"

@echo.
@echo Restarting server...
@echo.
goto start
