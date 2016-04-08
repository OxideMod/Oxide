@echo off
cls
:start
echo Starting server...

Blockstorm -batchmode -nographics -config blockstorm.cfg

echo.
echo Restarting server...
echo.
goto start
