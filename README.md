# KeyFlip

KeyFlip is a lightweight Windows utility that fixes text typed using the wrong Russian/English keyboard layout.

`ghbdtn` → `привет`  
`руддщ` → `hello`

Default hotkey: `Ctrl+Shift+K`.

## Features

- Russian ↔ English selected-text conversion
- Smart multi-word conversion
- Complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping
- Shift and symbol mapping
- Code-safe conversion that preserves syntax punctuation
- Formatting-preserving conversion in Microsoft Word
- File Explorer filename and extension preservation
- Configurable global hotkey and system tray
- Optional Windows autostart
- Terminal and protected-password exclusion
- Clipboard preservation
- Fully offline, with no AI or telemetry
- One standalone Windows x64 executable

## How to use

1. Select text.
2. Press `Ctrl+Shift+K`.
3. KeyFlip fixes the wrong-layout parts.

## Examples

```text
ghbdtn
→ привет

Это ghbdtn текст
→ Это привет текст

как руддщ дела
→ как hello дела

GHBDTN RFR LTKF
→ ПРИВЕТ КАК ДЕЛА

std::string text = "руддщ";
→ std::string text = "hello";

ghbdtn.vbh.zip
→ привет.мир.zip
```

## Smart conversion

A selection with one alphabetic token uses forced physical-layout conversion. Multiple words use conservative per-word decisions based on local Windows spell checking and a deterministic fallback. KeyFlip does not use AI or a network service.

## Code-safe mode

Code-like selections convert confidently mistyped alphabetic tokens while preserving punctuation, quotes, whitespace, delimiters, and operators exactly. VS Code integrated terminals are intentionally ignored.

## Microsoft Word

Word uses targeted Range replacements instead of replacing the whole selection, which preserves surrounding formatting and paragraph structure. Uncommon add-ins or custom Word hosts may not expose a safe automation context; KeyFlip aborts in that case.

## Download

Download `KeyFlip-v1.0.0-win-x64.exe` from the GitHub Releases section.

The release is portable. Place the executable in a permanent folder before enabling autostart, because the autostart entry points to its current path. The optional `install.ps1` script copies it to `%LocalAppData%\Programs\KeyFlip`.

## Requirements

- Windows 10 or Windows 11 x64
- No separate .NET runtime installation for the self-contained release

The x64 release is not a native ARM64 build.

## Privacy

KeyFlip does not send text anywhere, use AI, collect telemetry, require an account, require an internet connection, or store selected text. Diagnostic logs contain operational stages only and never include selected text or clipboard contents.

## Limitations

- Elevated applications may reject synthetic input because of Windows security boundaries.
- Protected password controls and terminals are intentionally ignored.
- Rare or custom clipboard formats may cause KeyFlip to abort rather than risk losing clipboard data.
- The x64 release is not a native ARM64 build.

## Build from source

Requirements: Windows and the .NET 8 SDK.

```powershell
.\build.ps1
```

The script restores dependencies, builds the solution, runs all automated checks, and publishes the self-contained release to `artifacts\release\win-x64`.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for a concise technical overview and [RELEASE_NOTES.md](RELEASE_NOTES.md) for the v1.0.0 release text.
