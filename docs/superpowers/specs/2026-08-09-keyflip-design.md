# KeyFlip Design

## Goal

Create a small offline Windows 10/11 x64 utility that replaces selected text typed in the wrong US QWERTY or Russian ЙЦУКЕН layout after the user presses a configurable global hotkey.

## Architecture

The .NET 8 WinForms executable runs without a console window and owns a tray icon plus a hidden native window for `WM_HOTKEY`. `HotkeyManager` registers `Ctrl+Alt+K` with `MOD_NOREPEAT`; `KeyFlipContext` serializes conversion operations and keeps the target application focused.

On hotkey, `ForegroundProcessService` reads the foreground process and ignores configured terminal executables. `ClipboardService` snapshots the current `IDataObject`, records the clipboard sequence number, sends Ctrl+C using `SendInput`, and accepts copied text only after a sequence-number change. It writes converted Unicode text, sends Ctrl+V, and then attempts to restore the original clipboard object. A timeout or no selection never causes insertion.

`LayoutConverter` is pure, has no Win32 dependency, and converts through static bidirectional tables for standard Windows US QWERTY and Russian ЙЦУКЕН printable keys, including shifted number row and punctuation. Direction is inferred strictly from counts of Latin and Cyrillic letters; equal/no-letter content remains unchanged.

Settings persist under `%LocalAppData%\\KeyFlip\\settings.json`; the app uses `HKCU\\...\\Run` for autostart and a named mutex for one instance. No network calls, telemetry, stored user text, or external dependencies are permitted.

## Boundaries

- `LayoutConverter`: mapping and direction decisions; tested exhaustively and round-trip.
- `ClipboardService` + `InputSimulator`: safe universal text replacement and clipboard restoration.
- `ForegroundProcessService`: terminal exclusion only; VS Code remains eligible.
- `Settings*`, `StartupManager`, `SettingsForm`: local configuration and Windows autostart.
- `KeyFlipContext`, `HotkeyManager`: application lifetime, hotkey, tray menu, and operation serialization.

## Reliability Rules

- A `SemaphoreSlim` permits exactly one conversion workflow.
- No clipboard sequence change means no selection; restore and return.
- Clipboard polling has short retries and a finite timeout, never long sleeps.
- Failures are quiet and log only technical metadata, never copied or converted text.
- Elevated targets and protected/read-only fields are not bypassed.

## Testing and Delivery

Use xUnit tests for examples, mixed/Unicode content, all physical-key mapping variants, and round trips. Build the WinForms project after integration changes and publish a self-contained, single-file win-x64 executable through `build.ps1`. Include install/uninstall scripts, README, `.gitignore`, and `AGENTS.md`.
