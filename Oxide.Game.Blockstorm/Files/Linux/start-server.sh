#!/bin/sh

./blockstormServer -batchmode -nographics -config blockstorm.cfg -logFile "log_`date +%d-%m-%Y`.txt"
