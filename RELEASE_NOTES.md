# KeyFlip v1.1.0

KeyFlip 1.1.0 is the current stable release. It promotes the converter and clipboard fixes validated in the RC4 candidate without changing their behavior.

Highlights:

- Preserves the RC3 clipboard transaction fixes and restoration safeguards.
- Fixes long mixed-layout prose when Windows spell-check dictionaries are missing or only partly available.
- Uses explicit clause direction to resolve ambiguous and typo-containing wrong-layout tokens without correcting their spelling.
- Keeps physical clusters atomic and protects technical tokens and strongly supported English islands.
- Adds deterministic no-scorer, English-only, Russian-only, and both-language scorer coverage; the trace tool reports whether the optional Windows scorer is available.
- Adds a development-only conversion trace and stale-build release guards.
- Shows `KeyFlip 1.1.0` in muted gray text at the bottom of the Settings window.

Download `KeyFlip_windows_x64.exe`. It is a self-contained, untrimmed, single-file build for Windows 10/11 x64 and does not require a separate .NET runtime installation.

The SHA-256 checksum is included in the GitHub release description. Unsupported-only or otherwise non-restorable clipboard data still causes a safe abort before KeyFlip changes the clipboard.
