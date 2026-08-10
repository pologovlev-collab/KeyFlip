$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot 'KeyFlip.sln'
$applicationProject = Join-Path $PSScriptRoot 'src\KeyFlip\KeyFlip.csproj'
$testProject = Join-Path $PSScriptRoot 'tests\KeyFlip.Tests\KeyFlip.Tests.csproj'
$releaseDirectory = Join-Path $PSScriptRoot 'artifacts\release\win-x64'
$publishDirectory = Join-Path $PSScriptRoot 'artifacts\release\.publish-win-x64'
$releaseFileName = 'KeyFlip_windows_x64.exe'
$releaseExecutable = Join-Path $releaseDirectory $releaseFileName
$checksumFile = Join-Path $releaseDirectory 'SHA256SUMS.txt'
$runtime = 'win-x64'
$sourceRevision = 'unknown'

try {
    $candidateRevision = (& git -C $PSScriptRoot rev-parse --short=7 HEAD 2>$null)
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($candidateRevision)) {
        $sourceRevision = $candidateRevision.Trim()
    }
} catch { }

function Invoke-DotNet {
    param([string[]]$CommandArguments)
    & dotnet @CommandArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed with exit code $LASTEXITCODE" }
}

Invoke-DotNet @('restore', $applicationProject, '--runtime', $runtime, '--disable-parallel', '-p:NuGetAudit=false')
Invoke-DotNet @('restore', $testProject, '--runtime', $runtime, '--disable-parallel', '-p:NuGetAudit=false')
Invoke-DotNet @(
    'build', $solution,
    '--configuration', 'Release',
    '--no-restore',
    '-m:1',
    '-p:UseSharedCompilation=false',
    "-p:SourceRevisionId=$sourceRevision")
Invoke-DotNet @('run', '--project', $testProject, '--configuration', 'Release', '--no-build', '--no-restore')

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $releaseDirectory) {
    Remove-Item -LiteralPath $releaseDirectory -Recurse -Force
}

Invoke-DotNet @(
    'publish', $applicationProject,
    '-p:PublishProfile=win-x64',
    '--configuration', 'Release',
    '--runtime', $runtime,
    '--self-contained', 'true',
    '--no-restore',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:PublishTrimmed=false',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    '-p:UseSharedCompilation=false',
    "-p:SourceRevisionId=$sourceRevision",
    '--output', $publishDirectory)

$publishedExecutable = Join-Path $publishDirectory 'KeyFlip.exe'
if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "Published executable not found: $publishedExecutable"
}

New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null
Copy-Item -LiteralPath $publishedExecutable -Destination $releaseExecutable
Remove-Item -LiteralPath $publishDirectory -Recurse -Force

$hash = (Get-FileHash -LiteralPath $releaseExecutable -Algorithm SHA256).Hash
Set-Content -LiteralPath $checksumFile -Value "$hash  $releaseFileName" -Encoding ASCII

$actualFiles = @(Get-ChildItem -LiteralPath $releaseDirectory -File | Select-Object -ExpandProperty Name | Sort-Object)
$expectedFiles = @($releaseFileName, 'SHA256SUMS.txt') | Sort-Object
if (Compare-Object -ReferenceObject $expectedFiles -DifferenceObject $actualFiles) {
    throw "Release directory contains unexpected or missing files: $($actualFiles -join ', ')"
}

Write-Host "Final release created: $releaseExecutable"
Write-Host "Version: 1.0.0; commit: $sourceRevision"
Write-Host "SHA-256: $hash"
