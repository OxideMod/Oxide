@echo off
cls
:start
echo Starting server...

Unturned -batchmode -nographics +secureserver "Oxide"

echo.
echo Restarting server...
echo.
goto start
