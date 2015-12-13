@echo off
cls
:start

Hurtworld -batchmode -nographics -exec "host 12871;maxplayers 10;servername My Oxide Server"

@echo.
@echo Restarting server...
@echo.
goto start
