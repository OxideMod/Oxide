@echo off
cls
:start
echo Starting server...

H2o -batchmode -nographics

echo.
echo Restarting server...
echo.
goto start
