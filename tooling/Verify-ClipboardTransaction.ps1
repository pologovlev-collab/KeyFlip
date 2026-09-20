param(
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '..\src\KeyFlip\bin\Release\net8.0-windows\KeyFlip.dll')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

$resolvedAssembly = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assembly = [System.Reflection.Assembly]::LoadFrom($resolvedAssembly)
$snapshotType = $assembly.GetType('KeyFlip.ClipboardSnapshot', $true)
$nativeMethodsType = $assembly.GetType('KeyFlip.NativeMethods', $true)
$staticFlags = [System.Reflection.BindingFlags]'Static,NonPublic'
$instanceFlags = [System.Reflection.BindingFlags]'Instance,Public,NonPublic'
$captureMethod = $snapshotType.GetMethod('Capture', $staticFlags)
$sequenceMethod = $nativeMethodsType.GetMethod('GetClipboardSequenceNumber', $staticFlags)

function Get-Capture([System.Windows.Forms.IDataObject]$DataObject) {
    $arguments = [object[]]::new(1)
    $arguments[0] = $DataObject
    $captureMethod.Invoke($null, $arguments)
}

function Get-CaptureProperty($Capture, [string]$Name) {
    $Capture.GetType().GetProperty($Name, $instanceFlags).GetValue($Capture)
}

function Restore-Snapshot($Snapshot) {
    $isEmpty = $Snapshot.GetType().GetProperty('IsEmpty', $instanceFlags).GetValue($Snapshot)
    if ($isEmpty) {
        [System.Windows.Forms.Clipboard]::Clear()
        return
    }

    $createDataObject = $Snapshot.GetType().GetMethod('CreateDataObject', $instanceFlags)
    $restored = $createDataObject.Invoke($Snapshot, @())
    [System.Windows.Forms.Clipboard]::SetDataObject($restored, $true)
}

function Dispose-Snapshot($Snapshot) {
    if ($null -ne $Snapshot) { ([System.IDisposable]$Snapshot).Dispose() }
}

$originalCapture = Get-Capture ([System.Windows.Forms.Clipboard]::GetDataObject())
$originalSnapshot = Get-CaptureProperty $originalCapture 'Snapshot'
$originalCanUse = Get-CaptureProperty $originalCapture 'CanUseForTransaction'
if (-not $originalCanUse -or $null -eq $originalSnapshot) {
    throw 'Current clipboard cannot be preserved safely; verification aborted before changing it.'
}

$knownSnapshot = $null
try {
    [System.Windows.Forms.Clipboard]::SetText('ORIGINAL_CLIPBOARD_VALUE')
    $knownCapture = Get-Capture ([System.Windows.Forms.Clipboard]::GetDataObject())
    $knownSnapshot = Get-CaptureProperty $knownCapture 'Snapshot'
    if (-not (Get-CaptureProperty $knownCapture 'CanUseForTransaction') -or $null -eq $knownSnapshot) {
        throw 'Known text clipboard snapshot was not usable.'
    }

    [System.Windows.Forms.Clipboard]::SetText('TEMPORARY_CONVERTED_VALUE')
    Restore-Snapshot $knownSnapshot
    $restoredExactly = [System.Windows.Forms.Clipboard]::GetText() -ceq 'ORIGINAL_CLIPBOARD_VALUE'

    [System.Windows.Forms.Clipboard]::SetText('ghbdtn')
    $sequenceBefore = [uint32]$sequenceMethod.Invoke($null, @())
    [System.Windows.Forms.Clipboard]::SetText('ghbdtn')
    $sequenceAfter = [uint32]$sequenceMethod.Invoke($null, @())
    $sameValueChangedSequence = $sequenceAfter -ne $sequenceBefore

    if (-not $restoredExactly) { throw 'Known clipboard text was not restored exactly.' }
    if (-not $sameValueChangedSequence) { throw 'Setting the same clipboard text did not change the sequence number.' }

    Write-Host 'KnownTextRestoredExactly=yes'
    Write-Host 'SameTextSequenceChanged=yes'
}
finally {
    try { Restore-Snapshot $originalSnapshot }
    finally {
        Dispose-Snapshot $knownSnapshot
        Dispose-Snapshot $originalSnapshot
    }
}
