#!/bin/bash
function run_file {
    dotnet run --project ../../Lupi/Lupi.csproj $1
    rm -f results.log
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
done
popd