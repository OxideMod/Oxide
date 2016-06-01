@echo off
cls
:start
echo Starting server...

ROK.exe -batchmode -nographics -silentcrash

echo.
echo Restarting server...
echo.
goto start
