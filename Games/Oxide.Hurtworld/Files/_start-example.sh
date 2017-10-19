#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
clear
while :
do
    echo "Starting server...\n"
    exec ./Hurtworld.x86_64 -batchmode -nographics \
    -exec "host 12871;queryport 12881;maxplayers 60;servername My Oxide Server"
    echo "\nRestarting server...\n"
done
