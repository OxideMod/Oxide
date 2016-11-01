@echo off
cls
:start
echo Starting server...

Build\IR.exe -server

echo.
echo Restarting server...
echo.
goto start
