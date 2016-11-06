@echo off
cls
:start
echo Starting server...

TerrariaServer.exe -config serverconfig.txt

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
