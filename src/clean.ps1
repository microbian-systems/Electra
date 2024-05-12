#!/usr/bin/env pwsh

Write-Host "Cleaning project via dotnet clean..."
dotnet clean ./Electra.sln

Write-Host "Cleaning up bin and obj folders in directory: $PWD"
Write-Host ""

Get-ChildItem .\ -include bin,obj -Recurse | foreach ($_) {
    Write-Host "Removing $($_.fullname)"
    remove-item $_.fullname -Force -Recurse
}

Write-Host ""
Write-Host ""
Write-Host "restoring packages via dotnet restore..."
dotnet restore ./Electra.sln
