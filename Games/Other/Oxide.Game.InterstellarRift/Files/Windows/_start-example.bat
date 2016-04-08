@echo off
cls
:start
echo Starting server...

Build\IR -server

echo.
echo Restarting server...
echo.
goto start
