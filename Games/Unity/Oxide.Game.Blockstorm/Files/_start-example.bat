@echo off
cls
:start
echo Starting server...

Blockstorm.exe -batchmode -nographics -config blockstorm.cfg

echo.
echo Restarting server...
echo.
goto start
