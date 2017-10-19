@echo off
cls
:start
echo Starting server...

ROK.exe -batchmode -nographics -silentcrash

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
