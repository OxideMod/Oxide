@echo off
cls
:start

DedicatedServer64\SpaceEngineersDedicated -console -path config -ip 0.0.0.0 -port 27016 -maxPlayers 10

@echo.
@echo Restarting server...
@echo.
goto start
