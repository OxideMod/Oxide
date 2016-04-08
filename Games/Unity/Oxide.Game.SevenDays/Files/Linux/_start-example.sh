#!/bin/sh
clear
while :
do
    echo "Starting server...\n"
    exec ./7DaysToDieServer -batchmode -nographics -configfile=serverconfig.xml -dedicated
    echo "\nRestarting server...\n"
done
