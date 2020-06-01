#!/bin/bash
function run_file {
    dotnet run --project ../../Lupi/Lupi.csproj $1
    rm -f results.log
}
echo "starting"

pushd ../Lupi
dotnet build -c Release
popd

pushd Configurations
run_file ConcurrencyOnly.yml
run_file OpenWorkload.yml
run_file ComplexPhases.yml
popd