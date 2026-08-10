# KeyFlip v1.0.0

KeyFlip fixes selected text typed using the wrong Russian/English keyboard layout. Select text and press `Ctrl+Shift+K`.

Highlights:

- Smart RU ↔ EN conversion with full physical keyboard and symbol mapping.
- Code-safe syntax preservation.
- Formatting-preserving Microsoft Word replacements.
- File Explorer filename and extension handling.
- Clipboard preservation, terminal exclusion, tray settings, and autostart.
- Fully offline: no AI, account, network communication, or telemetry.

Download `KeyFlip-v1.0.0-win-x64.exe`. It is a self-contained single-file build for Windows 10/11 x64 and does not require a separate .NET runtime installation.

Known limitations: elevated applications may reject synthetic input; protected password controls and terminals are intentionally ignored; rare clipboard formats cause a safe abort; this is not a native ARM64 build. Microsoft Word behavior can vary with uncommon add-ins or custom document hosts.
