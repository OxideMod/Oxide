#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
clear
while :
do
    echo "Starting server...\n"
    exec ./SavageLandsDedicatedServer -batchmode -nographics -writelogs -console_adapter "Best" -console_password "oxide" -server_config server.cfg
    echo "\nRestarting server...\n"
    sleep 10
done
