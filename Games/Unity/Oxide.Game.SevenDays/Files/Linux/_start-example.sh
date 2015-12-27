#!/bin/sh
clear
while :
do
    exec ./7DaysToDieServer -batchmode -nographics -configfile=serverconfig.xml -dedicated
    echo "\nRestarting server...\n"
done
