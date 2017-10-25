@echo off
cls
:start
echo Starting server...

Server\PE_Server.exe -batchmode

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
