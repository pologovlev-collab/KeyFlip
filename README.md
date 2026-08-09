# KeyFlip

KeyFlip is a lightweight Windows utility that fixes text typed in the wrong Russian/English keyboard layout.

`ghbdtn` → `привет`

`руддщ` → `hello`

Select the text and press `Ctrl+Shift+K`. KeyFlip replaces the selection without changing the active Windows keyboard layout.

## Features

- Selected-text conversion with a configurable global hotkey
- Smart mixed Russian/English selections
- Complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping
- Shift and symbol conversion, including `@#$^&` ↔ `"№;:?`
- Clipboard preservation for materialized text, image, file-list, stream, and common data formats
- Tray controls and optional current-user autostart
- Terminal processes excluded by default; `Code.exe` remains supported as an editor target
- Fully offline: no AI, network access, telemetry, analytics, or cloud services

Smart mixed selection uses the local Windows Spell Checking API when available, with a small deterministic fallback. Uncertain mixed-language words are kept unchanged; selections containing letters from only one script retain predictable forced conversion.

## Install

KeyFlip requires 64-bit Windows and the .NET 8 Windows Desktop Runtime.

1. Extract the release candidate.
2. Run `install.ps1` from PowerShell.
3. Select mistyped text and press `Ctrl+Shift+K`.

The installer copies KeyFlip to `%LocalAppData%\Programs\KeyFlip`, creates only its own current-user autostart value, and launches it. Run `uninstall.ps1` to remove those items.

## Build

Requirements: Windows x64 and the .NET 8 SDK.

```powershell
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj
.\build.ps1
```

The release candidate is written to `artifacts\KeyFlip-RC\KeyFlip.exe`. The offline build is framework-dependent and does not download runtime packs.

## Privacy

KeyFlip never logs or stores selected text, converted text, clipboard contents, or passwords. It uses standard Copy/Paste only as an internal transport, restores a deep materialized snapshot of the previous clipboard after success or failure, and aborts before copying when a safe snapshot cannot be made. Its temporary converted-text clipboard value is marked as excluded from Windows clipboard history, monitor processing, and cloud clipboard.

## Limitations

- The target application's own initial Copy operation may still add the original selected text to Windows clipboard history; KeyFlip cannot attach history metadata to clipboard data produced by another process.
- Elevated applications can reject synthetic input from a non-elevated KeyFlip process because of Windows UIPI.
- Password protection depends on the focused control exposing the standard Windows UI Automation password property. If protection is detected, KeyFlip does nothing.
- Terminals are intentionally ignored, and read-only or non-standard editors may reject Copy/Paste.
- Proprietary delayed clipboard formats that cannot be safely materialized cause an early abort rather than risking the existing clipboard.
