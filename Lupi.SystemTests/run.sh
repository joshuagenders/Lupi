#!/bin/bash
function run_file {
    echo "running $1"
    dotnet run --project ../../Lupi/Lupi.csproj $1
    rm -f results.log
    echo "finished $1"
    echo "---"
}
echo "starting"

rm -rf ../Lupi.Examples/bin/Release/netcoreapp3.0/publish/
rm -rf ../Lupi/bin
pushd ..
dotnet build -c Release
dotnet publish -c Release
popd

rm -rf Examples
cp -r ../Lupi.Examples/bin/Release/netcoreapp3.0/publish/ Examples

pushd Configurations
for i in *.yml; do
    run_file $i
    echo "sleeping to create a gap between runs"
    sleep 12s
    echo "awake"
done
# run_file ComplexPhases.yml
popd