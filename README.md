# KeyFlip

Lightweight Windows utility for converting selected text between Russian and English keyboard layouts.

KeyFlip corrects text only when you ask it to: select text, press a hotkey, and the selected text is replaced without changing the Windows keyboard layout.

## Features

- RU ↔ EN selected-text conversion with default `Ctrl+Alt+K` global hotkey;
- physical US QWERTY ↔ Russian ЙЦУКЕН mapping, including Shift and punctuation;
- works through standard Copy/Paste, targeting browsers, VS Code, Notepad, and typical desktop editors;
- terminal processes are excluded by default;
- configurable hotkey and excluded executable names;
- tray menu, autostart, and single-instance protection;
- fully local: no AI, network, telemetry, analytics, or cloud sync.

## Usage

1. Select text in an application.
2. Press `Ctrl+Alt+K`.
3. KeyFlip replaces it with the corresponding layout conversion.

Examples:

`ghbdtn` → `привет`  
`руддщ` → `hello`  
`,` → `б`  
`<` → `Б`

## Installation

Download the release archive, unpack it, then run `install.ps1` from PowerShell. It installs only to `%LocalAppData%\Programs\KeyFlip`, enables the current-user autostart entry, and launches KeyFlip.

To remove it, run `uninstall.ps1`. It removes only KeyFlip's application folder and its own `HKCU\...\Run\KeyFlip` value.

## Build

Requirements: Windows x64 and .NET 8 SDK.

```powershell
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj
.\build.ps1
```

The Windows release is produced at `artifacts\KeyFlip\KeyFlip.exe`. It is framework-dependent and needs the .NET 8 Windows Desktop Runtime. The project deliberately does not download runtime packs during its offline build; use a machine with the `win-x64` runtime pack already installed if a self-contained single-file publish is required.

## Privacy

KeyFlip does not send data anywhere and does not log selected text, converted text, or clipboard contents. It temporarily uses the Windows clipboard to work across applications, then attempts to restore its prior `IDataObject` (including available non-text formats).

## Limitations

- Elevated target applications can reject `SendInput` because of Windows UIPI.
- Protected, password, and read-only controls may not allow copy/paste; KeyFlip does not bypass them.
- Windows Terminal, cmd, PowerShell, WSL, and similar terminal executables are intentionally ignored.
- Clipboard restoration relies on what the source application exposes through the standard Windows clipboard API.

## Manual smoke tests

- Chrome, Edge, Firefox, Яндекс Браузер: both directions in input, textarea, and contenteditable.
- VS Code: `руддщ цщкдв` → `hello world`.
- Notepad: both directions and multiline selection.
- Punctuation: `, < . > @ " # № $ ; ^ : & ?`.
- No selection: ensure no previous clipboard text is inserted.
- Windows Terminal, PowerShell, and cmd: ensure the hotkey does nothing.
- Clipboard preservation: copy text/image/file first, run a conversion, then check restoration.
