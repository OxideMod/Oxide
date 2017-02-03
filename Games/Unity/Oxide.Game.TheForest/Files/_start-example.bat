@echo off
cls
:start
echo Starting server...

TheForest.exe -batchmode -nographics -dedicated -servername "My Oxide Server" -serverplayers 16 -serverautosaveinterval 15

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
