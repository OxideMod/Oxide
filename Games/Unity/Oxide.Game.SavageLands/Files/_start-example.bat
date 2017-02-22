@echo off
cls
:start
echo Starting server...

SavageLandsDedicatedServer.exe -batchmode -nographics -writelogs -console_password "oxide" -server_config "server.cfg"
echo.
echo Restarting server...
timeout /t 10
echo.
goto start
