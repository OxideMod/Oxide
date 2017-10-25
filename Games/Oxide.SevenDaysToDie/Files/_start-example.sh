#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
clear
while :
do
    echo "Starting server...\n"
    exec ./7DaysToDieServer -batchmode -nographics -configfile=serverconfig.xml -dedicated
    echo "\nRestarting server...\n"
done
