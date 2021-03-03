#!/bin/bash

# Build
docker build -t lupi-system-tests -f ./Lupi.SystemTests/Dockerfile .

# start Grafana
docker run -d -p 80:80 -p 8125:8125/udp -p 8126:8126 --name grafana marial/grafana-graphite-statsd

function run_file {
    echo "executing $1"
    docker run --network=host lupi-system-tests $1
    echo "finished $1"
    echo "---"
}

echo "starting test runs"

pushd Lupi.SystemTests/Configurations
for i in *.yml; do
    if [ "$i" != "base-config.yml" ]
    then
        run_file ./configurations/$i
        echo "sleeping to create a gap between runs"
        sleep 12s
        echo "awake"
    fi
done
popd

# docker stop grafana
