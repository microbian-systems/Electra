#!/usr/bin/env bash

# Clean and build in release
dotnet restore
dotnet clean
dotnet build -c Release

# Create all NuGet packages
find . -name "*.*proj" | while read proj
do
  echo "Processing project $proj ..."
  # take action on each file. $f store current file name
  # cat "$f"
  dotnet pack $proj --no-build -c Release -o ../artifacts
done

#dotnet pack core/Piranha/Piranha.csproj --no-build -c Release -o ./artifacts

