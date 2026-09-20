# KeyFlip v1.0.1-rc4

Testing pre-release for KeyFlip. Stable v1.0.0 remains the recommended stable download.

Changes in this candidate:

- Preserves the RC3 clipboard transaction fixes and restoration safeguards.
- Fixes long mixed-layout prose when Windows spell-check dictionaries are missing or only partly available.
- Uses explicit clause direction to resolve ambiguous and typo-containing wrong-layout tokens without correcting their spelling.
- Keeps physical clusters atomic and protects technical tokens and strongly supported English islands.
- Adds deterministic no-scorer, English-only, Russian-only, and both-language scorer coverage; the trace tool reports whether the optional Windows scorer is available.
- Adds a development-only conversion trace and stale-build release guards.

Download `KeyFlip_windows_x64.exe`. It is a self-contained, untrimmed, single-file build for Windows 10/11 x64 and does not require a separate .NET runtime installation.

The SHA-256 checksum is included in the GitHub release description. Unsupported-only or otherwise non-restorable clipboard data still causes a safe abort before KeyFlip changes the clipboard.
