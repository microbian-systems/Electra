#!/usr/bin/env pwsh


Write-Host "running ef core migrations"

$migrationName = $args[0]
if ($migrationName -eq "" -or $migrationName -eq $null ) {
    Write-Host "a migration name must be provided"
    Write-Host "Usage: ./run-ef-migrations.ps1 {migration-name}"
    exit 1
}

Write-Host "Updating migrations with name $migrationName"

$startProj = "Microbians.Persistence/Microbians.Persistence.csproj"
$contextProj = "Microbians.Persistence/Microbians.Persistence.csproj"
$authProj = "Microbians.Common.Web/Microbians.Common.Web.csproj"

Write-Host "applying update(s) for Microbians.Common.Web.ApiAuthContext"
dotnet ef migrations add $migrationName -p $authProj -c ApiAuthContext --startup-project $startProj
