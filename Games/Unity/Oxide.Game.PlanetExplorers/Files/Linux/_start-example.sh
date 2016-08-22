#!/bin/sh
clear
while :
do
    echo "Starting server...\n"
    exec ./Server/PE_Server.x86 -batchmode
    echo "\nRestarting server...\n"
done
