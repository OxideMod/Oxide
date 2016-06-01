@echo off
cls
:start
echo Starting server...

H2o.exe -batchmode -nographics

echo.
echo Restarting server...
echo.
goto start
