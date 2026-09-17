$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'SteamGameImporter.csproj'
$output = Join-Path $PSScriptRoot 'publish\win-x64'
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -o $output
Write-Host "Elkeszult: $output\SteamGameImporter.exe"

