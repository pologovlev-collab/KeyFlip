param(
    [string]$SourceDirectory = (Join-Path $PSScriptRoot 'artifacts\release\win-x64')
)

$ErrorActionPreference = 'Stop'
$executableName = 'KeyFlip.exe'
$sourceExecutable = Join-Path $SourceDirectory $executableName
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
Copy-Item -LiteralPath $sourceExecutable -Destination $installDirectory -Force
$installedExecutable = Join-Path $installDirectory $executableName
Set-ItemProperty -Path $runKeyPath -Name 'KeyFlip' -Value "`"$installedExecutable`""
Start-Process -FilePath $installedExecutable -WindowStyle Hidden
