@echo off
cls
:start
echo Starting server...

for %%* in (.) do set DIR=%%~nx*
if NOT "%DIR%" == "64" cd 64
FC_64.exe -batchmode -nographics

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
