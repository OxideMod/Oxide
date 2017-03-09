@echo off
cls
:start
echo Starting server...

BlackwakeServer.exe -batchmode -nographics

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
