@echo off
cls
:start
echo Starting server...

TheForestDedicatedServer.exe ^
-nosteamclient ^
-serverip 0.0.0.0 ^
-serversteamport 8766 ^
-servergameport 27015 ^
-serverqueryport 27016 ^
-servername "My Oxide Server" ^
-serverplayers 10 ^
-serverautosaveinterval 15 ^
-serverpassword "" ^
-serverpassword_admin "" ^
-enableVAC ^
-difficulty Normal ^
-inittype Continue ^
-slot 1

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
