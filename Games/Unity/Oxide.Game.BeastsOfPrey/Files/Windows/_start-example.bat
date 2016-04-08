@echo off
cls
:start
echo Starting server...

BoPServer -batchmode

echo.
echo Restarting server...
echo.
goto start
