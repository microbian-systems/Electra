#!/usr/bin/env pwsh

foreach ($file in Get-ChildItem -Path "." -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName) {
    Write-Host "Found $file"
    upgrade-assistant upgrade $file -t net8 -f html --skip-backup --non-interactive
}

#Get-ChildItem -Path "." -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName
