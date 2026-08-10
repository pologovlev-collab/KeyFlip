# KeyFlip v1.0.0

KeyFlip fixes selected text typed using the wrong Russian/English keyboard layout. Select text and press `Ctrl+Shift+K`.

Highlights:

- Smart RU ↔ EN conversion with the complete physical keyboard and symbol mapping.
- Code-aware conversion that preserves existing syntax and can recover confidently detected wrong-layout syntax.
- Formatting-preserving Microsoft Word replacements.
- File Explorer filename conversion, including names with spaces, while preserving the final extension.
- Clipboard preservation, terminal and protected-field exclusion, tray settings, and Windows autostart.
- Fully local and offline: no AI, account, network communication, telemetry, or analytics.

Download `KeyFlip_windows_x64.exe`. It is a self-contained, untrimmed, single-file build for Windows 10/11 x64 and does not require a separate .NET runtime installation. Verify it against `SHA256SUMS.txt` from the same release.

Known limitations: elevated applications may reject synthetic input; protected password controls and terminals are intentionally ignored; rare clipboard formats cause a safe abort; Windows ARM64 is not yet tested and has no native build; Windows 7, Linux, and macOS are not supported. Mixed formatting inside one individual Microsoft Word word may become uniform.
