param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts\KeyFlip-RC')
)

$ErrorActionPreference = 'Stop'
dotnet publish (Join-Path $PSScriptRoot 'src\KeyFlip\KeyFlip.csproj') `
    --configuration Release `
    --no-restore `
    -p:UseAppHost=true `
    -p:UseSharedCompilation=false `
    --output $OutputDirectory

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Release candidate created: $(Join-Path $OutputDirectory 'KeyFlip.exe')"
