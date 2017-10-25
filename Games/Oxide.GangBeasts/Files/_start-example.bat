@echo off
cls
:start
echo Starting server...

echo start | Wrapper.exe

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
