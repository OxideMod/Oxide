@echo off
cls
:start
echo Starting server...

"Nomad Server\NomadServer.exe" -name "My Oxide Server" -port 5127 -slots 30 -clientVersion "0.57" -password "" -tcpLobby "149.202.51.185" 25565

@echo.
@echo Restarting server...
@echo.
goto start
