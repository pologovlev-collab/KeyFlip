# KeyFlip

KeyFlip is a small, fully local Windows utility that fixes selected text typed in the wrong Russian/English keyboard layout.

`ghbdtn` → `привет`

`руддщ` → `hello`

Select text and press `Ctrl+Shift+K`. KeyFlip replaces the selection without changing the active Windows keyboard layout.

## Behavior

- A selection with one alphabetic token uses forced physical-layout conversion.
- A selection with two or more alphabetic tokens uses conservative per-word conversion. Uncertain words remain unchanged.
- Punctuation attached to a converted word follows that word's direction in ordinary prose: `Это ghbdtn@ текст` → `Это привет" текст`.
- Code-like selections preserve every punctuation/operator character and convert only confidently wrong-layout alphabetic tokens. For example, `std::string text = "руддщ";` becomes `std::string text = "hello";`.
- Microsoft Word uses targeted range replacements instead of whole-selection paste, preserving surrounding character and paragraph formatting. If the active Word selection cannot be obtained safely, KeyFlip does nothing.
- A confirmed File Explorer rename preserves literal dots and the final extension: `ghbdtn.vbh.zip` → `привет.мир.zip`. Explorer search and address fields keep normal text behavior.
- A confirmed VS Code integrated terminal is ignored. VS Code editor and unknown VS Code contexts remain enabled to avoid breaking editor compatibility.
- Disabling KeyFlip from the tray or Settings releases the global hotkey.

The converter includes the complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping, including Shift and symbol keys. Smart decisions use the local Windows Spell Checking API when available and a deterministic offline fallback.

## Privacy and security

KeyFlip has no AI, network access, cloud service, telemetry, or analytics. It never logs selected text, converted text, clipboard contents, terminal commands, or passwords.

For normal applications, KeyFlip uses Copy/Paste as an internal transport and restores a materialized snapshot of the previous clipboard after success or failure. The temporary converted-text item is excluded from Windows clipboard history and cloud clipboard. Word's safe path does not use the clipboard at all. Masked password controls and excluded terminal processes abort before Copy.

## Download and run

The final candidate is a self-contained single-file Windows x64 executable:

`artifacts\release\win-x64\KeyFlip.exe`

It includes the .NET 8 runtime, so a separate .NET Desktop Runtime installation is not required. This x64 build is for ordinary Intel/AMD 64-bit Windows machines; it is not a universal ARM64 binary.

You can place `KeyFlip.exe` in a permanent folder and run it directly. This is the recommended portable setup. Put it in its permanent location before enabling autostart: the current-user Run entry points to the executable's current path and becomes invalid if the file is later moved or deleted.

`install.ps1` is optional. It stops an existing KeyFlip process, copies the current final artifact to `%LocalAppData%\Programs\KeyFlip`, configures current-user autostart, and starts the new copy. `uninstall.ps1` removes those installed items.

## Build

Requirements: Windows x64 and the .NET 8 SDK or a newer SDK capable of targeting .NET 8.

```powershell
.\build.ps1
```

The script restores dependencies, builds the solution, runs all automated checks, and publishes a self-contained untrimmed single-file candidate. The final release folder contains only `KeyFlip.exe`.

## Diagnostics

The best-effort local diagnostic log is `%LocalAppData%\KeyFlip\keyflip.log`. Startup entries include the application version, short build commit when supplied by the build script, and the absolute running executable path. VS Code classification is logged only as editor, terminal, or unknown; no UI Automation names or user content are recorded.

Settings shows the running version. Starting a second instance displays a message instead of silently leaving an older build active.

## Limitations

- The target application's initial Copy operation may add original selected text to Windows clipboard history; KeyFlip cannot attach history metadata to data produced by another process.
- Elevated applications can reject synthetic input from a non-elevated KeyFlip process because of Windows UIPI.
- Password protection depends on the focused control exposing the standard UI Automation password property.
- Read-only or non-standard editors can reject Copy/Paste. Proprietary delayed clipboard formats that cannot be materialized cause an early abort rather than risking clipboard loss.
- Word formatting preservation requires desktop Microsoft Word and a safely obtainable current selection; failure is fail-closed with no plain-text fallback.

## Roadmap

- v1.0: complete the manual A–T validation and publish the verified x64 asset.
- v1.1: migrate the target framework to .NET 10 LTS as a separate compatibility pass; consider a separate Windows ARM64 asset.
