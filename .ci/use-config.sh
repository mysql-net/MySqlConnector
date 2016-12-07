#!/usr/bin/env bash
cd $(dirname $0)/config

display_usage() {
    echo -e "\nUsage:\n$0 [config.json script] [port]\n"
}

# check whether user had supplied -h or --help . If yes display usage
if [[ ( $# == "--help") ||  $# == "-h" ]]
then
    display_usage
    exit 0
fi

# check number of arguments
if [ $# -le 1 ]
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

cp $1 ../../tests/SideBySide.New/config.json
sed -i "s/3306/$2/g" ../../tests/SideBySide.New/config.json

