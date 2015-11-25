@echo off
cls
:start

Build\IR -server

@echo.
@echo Restarting server...
@echo.
goto start
