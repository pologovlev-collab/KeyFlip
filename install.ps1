param(
    [string]$SourceDirectory = (Join-Path $PSScriptRoot 'artifacts\KeyFlip')
)

$ErrorActionPreference = 'Stop'
$executableName = 'KeyFlip.exe'
$sourceExecutable = Join-Path $SourceDirectory $executableName
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\KeyFlip'
$runKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

if (-not (Test-Path -LiteralPath $sourceExecutable)) {
    throw "Release executable not found: $sourceExecutable. Run build.ps1 first."
}

New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $SourceDirectory '*') -Destination $installDirectory -Force
$installedExecutable = Join-Path $installDirectory $executableName
Set-ItemProperty -Path $runKeyPath -Name 'KeyFlip' -Value "`"$installedExecutable`""
Start-Process -FilePath $installedExecutable
