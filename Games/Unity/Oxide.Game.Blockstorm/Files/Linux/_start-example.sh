#!/bin/sh
clear
while :
do
    exec ./blockstormServer -batchmode -nographics -config blockstorm.cfg
    echo "\nRestarting server...\n"
done
