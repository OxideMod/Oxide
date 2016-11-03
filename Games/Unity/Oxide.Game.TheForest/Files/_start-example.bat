@echo off
cls
:start
echo Starting server...

TheForest.exe -batchmode -nographics -ip 0.0.0.0 -port 27016 -maxplayers 10 -hostname "My Oxide Server" -friendsonly 0

echo.
echo Restarting server...
timeout /t 10echo.
goto start
