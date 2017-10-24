@echo off
cls
:start
echo Starting server...

Build\IR.exe -server

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
