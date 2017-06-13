#!/bin/sh
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)
clear
while :
do
    echo "Starting server...\n"
    exec ./Server/PE_Server.x86 -batchmode
    echo "\nRestarting server...\n"
done
