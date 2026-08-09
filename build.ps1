$ErrorActionPreference = 'Stop'

$outputDirectory = Join-Path $PSScriptRoot 'artifacts\KeyFlip'
dotnet publish (Join-Path $PSScriptRoot 'src\KeyFlip\KeyFlip.csproj') `
    --configuration Release `
    --no-restore `
    -p:UseAppHost=true `
    --output $outputDirectory

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Release created: $(Join-Path $outputDirectory 'KeyFlip.exe')"
