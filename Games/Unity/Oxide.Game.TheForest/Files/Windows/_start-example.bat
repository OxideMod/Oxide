@echo off
cls
:start

TheForest -batchmode -nographics -ip 0.0.0.0 -port 27000 -maxplayers 10 -hostname "My Oxide Server" -friendsonly 0

@echo.
@echo Restarting server...
@echo.
goto start
