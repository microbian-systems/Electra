#!/usr/bin/env pwsh

# create powershell script paramater for visual studio configuration (Debug, staging, local or release)
$config = $args[0]
if ($config -ne "Debug" -and $config -ne "Release" -and $config -ne "Local" -and $config -ne "Staging" ) {
    Write-Host "no or invalid configuration value provided as argument - defaulting to \"Local\""
    Write-Host "Usage: ./update-database.ps1 [Debug|Release|Local|Staging]"
    #exit 1
    $config = "Local"
}

Write-Host "Updating database contexts for $config config"

$startProj = "DealerManagement/DealerManagement.csproj"
$contextProj = "Microbians.Core.Persistence/Microbians.Core.Persistence.csproj"
$authProj = "Microbians.Common.Web/Microbians.Common.Web.csproj"

Write-Host "applying update(s) for Microbians.Common.Web.ApiAuthContext"
dotnet ef database update -p $authProj  -c ApiAuthContext -s $startProj

Write-Host "applying update(s) for CustomerContext"
dotnet ef database update -p $contextProj  -c CustomerContext -s $startProj

Write-Host "applying update(s) for InventoryContext"
dotnet ef database update -p $contextProj  -c InventoryContext -s $startProj

Write-Host "applying update(s) for LoggingContext"
dotnet ef database update -p $contextProj  -c LoggingContext  -s $startProj

Write-Host "applying update(s) for PurchaseContext"
dotnet ef database update -p $contextProj  -c PurchaseContext -s $startProj

Write-Host "applying update(s) for IBVContext"
dotnet ef database update -p $contextProj  -c IBVContext -s $startProj

Write-Host "applying update(s) for IDVContext"
dotnet ef database update -p $contextProj  -c IDVContext -s $startProj

Write-Host "applying update(s) for ReviewContext"
dotnet ef database update -p $contextProj  -c ReviewContext -s $startProj

