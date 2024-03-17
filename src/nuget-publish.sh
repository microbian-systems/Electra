#!/usr/bin/env bash

# Clean and build in release
dotnet restore
dotnet clean
dotnet build -c Release

# Create all NuGet packages
find . -name "*.nupkg" | while read nupkg
do
  echo "Processing project $nupkg ..."
  # take action on each file. $f store current file name
  # dotnet pack nupkg --no-build -c Release -o ../artifacts
  # https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package
  dotnet nuget push $nupkg --api-key qz2jga8pl3dvn2akksyquwcs9ygggg4exypy3bhxy6w6x6 --source https://api.nuget.org/v3/index.json
done

#dotnet pack core/Piranha/Piranha.csproj --no-build -c Release -o ./artifacts

