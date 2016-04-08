@echo off
cls
:start
echo Starting server...

TerrariaServer -config serverconfig.txt

echo.
echo Restarting server...
echo.
goto start
