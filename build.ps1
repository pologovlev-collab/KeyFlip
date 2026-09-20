$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot 'KeyFlip.sln'
$applicationProject = Join-Path $PSScriptRoot 'src\KeyFlip\KeyFlip.csproj'
$testProject = Join-Path $PSScriptRoot 'tests\KeyFlip.Tests\KeyFlip.Tests.csproj'
$candidateVersion = 'v1.0.1-rc4'
$releaseDirectory = Join-Path $PSScriptRoot "artifacts\candidates\$candidateVersion"
$publishDirectory = Join-Path $PSScriptRoot "artifacts\candidates\.$candidateVersion-publish-win-x64"
$releaseFileName = 'KeyFlip_windows_x64.exe'
$releaseExecutable = Join-Path $releaseDirectory $releaseFileName
$runtime = 'win-x64'
$releaseInputs = @(
    '.github\workflows\ci.yml',
    'assets',
    'build.ps1',
    'KeyFlip.sln',
    'src',
    'tests'
)
$sourceRevision = (& git -C $PSScriptRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceRevision -notmatch '^[0-9a-f]{40}$') {
    throw 'Unable to resolve the exact source commit.'
}
$shortRevision = $sourceRevision.Substring(0, 7)

function Invoke-DotNet {
    param([string[]]$CommandArguments)
    & dotnet @CommandArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed with exit code $LASTEXITCODE" }
}

function Assert-ReleaseInputsClean {
    $changes = @(& git -C $PSScriptRoot status --porcelain --untracked-files=all -- @releaseInputs)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect release inputs.' }
    if ($changes.Count -ne 0) {
        throw "Release inputs differ from HEAD:`n$($changes -join [Environment]::NewLine)"
    }
}

function Assert-SourceRevision {
    $currentRevision = (& git -C $PSScriptRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $currentRevision -ne $sourceRevision) {
        throw "HEAD changed during release build: expected $sourceRevision; actual $currentRevision"
    }
}

function Remove-BuildDirectory([string]$Path) {
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

Assert-ReleaseInputsClean
Assert-SourceRevision

Remove-BuildDirectory (Join-Path $PSScriptRoot 'src\KeyFlip\bin')
Remove-BuildDirectory (Join-Path $PSScriptRoot 'src\KeyFlip\obj')
Remove-BuildDirectory (Join-Path $PSScriptRoot 'tests\KeyFlip.Tests\bin')
Remove-BuildDirectory (Join-Path $PSScriptRoot 'tests\KeyFlip.Tests\obj')
Remove-BuildDirectory $publishDirectory
if (Test-Path -LiteralPath $releaseDirectory) {
    Get-ChildItem -LiteralPath $releaseDirectory -Force | Remove-Item -Recurse -Force
}

Invoke-DotNet @('restore', $applicationProject, '--runtime', $runtime, '--disable-parallel', '-p:NuGetAudit=false')
Invoke-DotNet @('restore', $testProject, '--runtime', $runtime, '--disable-parallel', '-p:NuGetAudit=false')
Assert-SourceRevision
Invoke-DotNet @(
    'build', $solution,
    '--configuration', 'Release',
    '--no-restore',
    '-m:1',
    '-p:UseSharedCompilation=false',
    "-p:SourceRevisionId=$shortRevision")
Invoke-DotNet @('run', '--project', $testProject, '--configuration', 'Release', '--no-build', '--no-restore')
Assert-ReleaseInputsClean
Assert-SourceRevision

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
    "-p:SourceRevisionId=$shortRevision",
    '--output', $publishDirectory)
Assert-ReleaseInputsClean
Assert-SourceRevision

$publishedExecutable = Join-Path $publishDirectory 'KeyFlip.exe'
if (-not (Test-Path -LiteralPath $publishedExecutable -PathType Leaf)) {
    throw "Published executable not found: $publishedExecutable"
}

New-Item -ItemType Directory -Path $releaseDirectory -Force | Out-Null
Copy-Item -LiteralPath $publishedExecutable -Destination $releaseExecutable
Remove-Item -LiteralPath $publishDirectory -Recurse -Force

$hash = (Get-FileHash -LiteralPath $releaseExecutable -Algorithm SHA256).Hash
$productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($releaseExecutable).ProductVersion
$expectedProductVersion = "1.0.1-rc4+$shortRevision"
if ($productVersion -ne $expectedProductVersion) {
    throw "Unexpected ProductVersion '$productVersion'; expected '$expectedProductVersion'."
}

$actualFiles = @(Get-ChildItem -LiteralPath $releaseDirectory -File | Select-Object -ExpandProperty Name | Sort-Object)
$expectedFiles = @($releaseFileName)
if (Compare-Object -ReferenceObject $expectedFiles -DifferenceObject $actualFiles) {
    throw "Release directory contains unexpected or missing files: $($actualFiles -join ', ')"
}

Write-Host "Candidate created: $releaseExecutable"
Write-Host "Version: $productVersion"
Write-Host "Commit: $sourceRevision"
Write-Host "SHA-256: $hash"
