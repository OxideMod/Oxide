@echo off
cls
:start

Blockstorm -batchmode -nographics -config blockstorm.cfg

@echo.
@echo Restarting server...
@echo.
goto start
