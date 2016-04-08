@echo off
cls
:start
echo Starting server...

7DaysToDieServer -batchmode -nographics -configfile=serverconfig.xml -dedicated

echo.
echo Restarting server...
echo.
goto start
