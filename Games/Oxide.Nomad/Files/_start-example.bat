@echo off
cls
:start
echo Starting server...

"Nomad Server\NomadServer.exe" -port 5127 -slots 10 -password "" -tcpLobby "149.202.51.185" 25565

echo.
echo Restarting server...
timeout /t 10
echo.
goto start
