@echo off
cls
:start
echo Starting server...

Unturned.exe -batchmode -nographics -bind 0.0.0.0 -port 27015 -maxplayers 10 -map PEI -name "My Oxide Server" -welcome "Weclome to my Oxide server!" +secureserver "Oxide"

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
