**[🇷🇺 Русский](../../README.md) • 🇬🇧 English**

# KeyFlip

A small Windows utility that fixes selected text typed using the wrong Russian or English keyboard layout.

`ghbdtn` → `привет`<br>
`руддщ` → `hello`

Default hotkey: `Ctrl + Shift + K`.

## 🚀 Download

| System | Architecture | Download | Status |
|---|---:|---|---|
| Windows 10 / 11 | x64 | `KeyFlip_windows_x64.exe` | Stable |
| Windows 10 / 11 | ARM64 | — | Planned / not tested |

The public GitHub release does not exist yet, so this README intentionally contains no dead download link. After publication, the stable direct URL will be:

```text
https://github.com/<OWNER>/KeyFlip/releases/latest/download/KeyFlip_windows_x64.exe
```

All releases: `https://github.com/<OWNER>/KeyFlip/releases`. Replace `<OWNER>` with the real repository owner during publication.

The release is one portable executable and requires no separate .NET installation. Download `SHA256SUMS.txt` from the same release and verify the SHA-256 checksum.

## Quick start

1. Download `KeyFlip_windows_x64.exe` from the official GitHub release.
2. Put it in a permanent folder and run it.
3. Select text in a supported application.
4. Press `Ctrl + Shift + K`.
5. Manage KeyFlip, the hotkey, and autostart from its system tray icon.

When autostart is enabled, KeyFlip refreshes the startup entry with the EXE's current path. After moving the portable file, launch it manually once.

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

зкште (ЭруддщЭ)ж
→ print ("hello");

ghbdtn vbh.zip
→ привет мир.zip
```

## Features

- Smart per-word Russian ↔ English conversion.
- Complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping, including Shift and symbols.
- Code-aware mode that preserves existing syntax and converts confidently recognized wrong-layout syntax.
- Formatting and paragraph preservation in Microsoft Word.
- File Explorer filename conversion with final-extension preservation.
- Configurable global hotkey, system tray, and Windows autostart.
- Protected-field and terminal exclusion.
- Clipboard snapshot and restoration safeguards.
- Fully local and offline operation with no AI, telemetry, or analytics.

## How it works

A selection containing one alphabetic token uses forced physical-layout conversion. Multi-word selections use conservative per-word decisions based on local Windows spell checking and deterministic rules. Spaces, tabs, and line endings are preserved exactly.

In code contexts, KeyFlip first builds a safe candidate that preserves existing ASCII punctuation. It chooses full physical conversion of possible wrong-layout syntax only when that candidate strongly improves code plausibility. Ambiguous input keeps the safe candidate. A confirmed VS Code integrated terminal aborts before Copy or any clipboard modification.

Microsoft Word uses targeted Range replacements instead of replacing the complete selection. In File Explorer, a confirmed rename context uses filename policy and preserves the final extension.

## Supported systems

| System | Support |
|---|---|
| Windows 10 x64 | Yes |
| Windows 11 x64 | Yes |
| Windows 10 / 11 ARM64 | Planned and not tested; no native ARM64 build |
| Windows 7 | No — the current build uses .NET 8 |
| Linux / macOS | No — the application depends on WinForms, Win32, COM, and UI Automation |

The same self-contained x64 release targets both Windows 10 and Windows 11 x64.

## Security

KeyFlip is open source and operates entirely on the local computer. An unsigned executable may trigger Windows SmartScreen or antivirus warnings. Do not disable security software or add an unknown file to exclusions. Download KeyFlip only from the official GitHub release, verify its SHA-256 checksum, or build it yourself from source.

## Privacy

KeyFlip does not send text anywhere, use AI, collect telemetry, require an account or internet connection, or persist selected text. Diagnostic logs contain only safe operation stages, version data, and context classifications—never selected text, terminal commands, or clipboard contents.

## Known limitations

- Elevated applications may reject synthetic input because of Windows security boundaries.
- Password fields and terminals are intentionally ignored.
- KeyFlip safely aborts if a rare or unsupported clipboard format could not be preserved.
- Mixed formatting inside one individual Microsoft Word word may become uniform.
- Unusual Word add-ins and custom hosts may not provide a safe automation context.
- A folder name containing a dot may not be recognized by the conservative Explorer fallback; confirmed rename context is handled separately.

## Build from source

Requirements: Windows and the .NET 8 SDK.

```powershell
.\build.ps1
```

The script restores dependencies, builds Release, runs every automated check, and creates the self-contained single-file win-x64 release in `artifacts\release\win-x64`:

```text
KeyFlip_windows_x64.exe
SHA256SUMS.txt
```

See [docs/ARCHITECTURE.md](../ARCHITECTURE.md) for the technical overview and [RELEASE_NOTES.md](../../RELEASE_NOTES.md) for the v1.0.0 release text.
