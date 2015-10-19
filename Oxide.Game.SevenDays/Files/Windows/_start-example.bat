@echo off
cls
:start

7DaysToDieServer -batchmode -nographics -configfile=serverconfig.xml -dedicated

@echo.
@echo Restarting server...
@echo.
goto start
