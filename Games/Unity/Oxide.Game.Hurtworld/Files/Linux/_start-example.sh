#!/bin/sh
clear
while :
do
    echo "Starting server...\n"
    exec ./Hurtworld.x86_64 -batchmode -nographics \
    -exec "host 12871;queryport 12881;maxplayers 10;servername My Oxide Server"
    echo "\nRestarting server...\n"
done
