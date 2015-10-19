@echo off
cls
:start

TerrariaServer -config serverconfig.txt

@echo.
@echo Restarting server...
@echo.
goto start
