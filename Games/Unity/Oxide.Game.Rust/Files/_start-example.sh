#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
clear
while :
do
    echo "Starting server...\n"

    exec ./RustDedicated -batchmode -nographics \
    -rcon.ip 0.0.0.0 \
    -rcon.port 28016 \
    -rcon.password "changeme" \
    -server.ip 0.0.0.0 \
    -server.port 28015 \
    -server.maxplayers 10 \
    -server.hostname "My Oxide Server" \
    -server.identity "my_server_identity" \
    -server.level "Procedural Map" \
    -server.seed 12345 \
    -server.worldsize 4000 \
    -server.saveinterval 300 \
    -server.globalchat true \
    -server.description "Powered by Oxide" \
    -server.headerimage "http://i.imgur.com/xNyLhMt.jpg" \
    -server.url "http://oxidemod.org"

    echo "\nRestarting server...\n"
    sleep 10
done
