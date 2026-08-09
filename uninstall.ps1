$ErrorActionPreference = 'Stop'
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\KeyFlip'
$runKeyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Get-Process -Name 'KeyFlip' -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-ItemProperty -Path $runKeyPath -Name 'KeyFlip' -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}

