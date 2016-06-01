@echo off
cls
:start
echo Starting server...

DedicatedServer64\SpaceEngineersDedicated.exe -console -path config -ip 0.0.0.0 -port 27016 -maxPlayers 10

echo.
echo Restarting server...
echo.
goto start
