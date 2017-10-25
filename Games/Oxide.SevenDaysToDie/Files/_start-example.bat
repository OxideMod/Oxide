@echo off
cls
:start
echo Starting server...

7DaysToDieServer.exe -batchmode -nographics -configfile=serverconfig.xml -dedicated

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
