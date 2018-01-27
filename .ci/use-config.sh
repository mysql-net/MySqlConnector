#!/usr/bin/env bash
cd $(dirname $0)/config

display_usage() {
    echo -e "\nUsage:\n$0 [config.json script] [host] [port] [name] [features]\n"
}

# check whether user had supplied -h or --help . If yes display usage
if [[ ( $# == "--help") ||  $# == "-h" ]]
then
    display_usage
    exit 0
fi

# check number of arguments
if [ $# -eq 0 ]
then
    display_usage
    exit 1
fi

# check that directory exists
if [ ! -f $1 ]
then
    echo -e "Config file does not exist: $1"
    exit 1
fi

cp $1 ../../tests/SideBySide/config.json

if [ $# -ge 2 ]
then
    sed -i "s/127.0.0.1/$2/g" ../../tests/SideBySide/config.json
fi

if [ $# -ge 3 ]
then
    sed -i "s/3306/$3/g" ../../tests/SideBySide/config.json
fi

if [ $# -ge 4 ]
then
    sed -i "s/run\/mysql/run\/$4/g" ../../tests/SideBySide/config.json
fi

if [ $# -ge 5 ]
then
    sed -i "s/\"UnsupportedFeatures\": \".*\"/\"UnsupportedFeatures\": \"$5\"/g" ../../tests/SideBySide/config.json
fi
