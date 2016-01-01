@echo off
cls
:start

Unturned -batchmode -nographics +secureserver "Oxide"

@echo.
@echo Restarting server...
@echo.
goto start
