#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
clear
while :
do
    echo "Starting server...\n"
    exec ./blockstormServer -batchmode -nographics -config blockstorm.cfg
    echo "\nRestarting server...\n"
done
