param(
    [string]$SourceDirectory = (Join-Path $PSScriptRoot 'artifacts\release\win-x64')
)

$ErrorActionPreference = 'Stop'
$releaseAssetName = 'KeyFlip-v1.0.0-win-x64.exe'
$installedExecutableName = 'KeyFlip.exe'
$sourceExecutable = Join-Path $SourceDirectory $releaseAssetName
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\KeyFlip'
$runKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

if (-not (Test-Path -LiteralPath $sourceExecutable)) {
    throw "Release executable not found: $sourceExecutable. Run build.ps1 first."
}

$runningProcesses = @(Get-Process -Name 'KeyFlip' -ErrorAction SilentlyContinue)
foreach ($process in $runningProcesses) {
    Stop-Process -InputObject $process -Force
    if (-not $process.WaitForExit(10000)) {
        throw "Existing KeyFlip process did not stop within 10 seconds."
    }
}

New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourceExecutable -Destination (Join-Path $installDirectory $installedExecutableName) -Force
$installedExecutable = Join-Path $installDirectory $installedExecutableName
Set-ItemProperty -Path $runKeyPath -Name 'KeyFlip' -Value "`"$installedExecutable`""
Start-Process -FilePath $installedExecutable -WindowStyle Hidden
