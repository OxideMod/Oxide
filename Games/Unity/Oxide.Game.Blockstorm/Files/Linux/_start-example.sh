#!/bin/sh
clear
while :
do
    echo "Starting server...\n"
    exec ./blockstormServer -batchmode -nographics -config blockstorm.cfg
    echo "\nRestarting server...\n"
done
