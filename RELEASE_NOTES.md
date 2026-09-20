# KeyFlip v1.0.1-rc3

Testing pre-release for KeyFlip. Stable v1.0.0 remains the recommended stable download.

Changes in this candidate:

- Fixed clipboard conversion when the clipboard already contains data.
- Made clipboard snapshots robust to optional unsupported formats while preserving meaningful restorable data.
- Kept original clipboard restoration in the transaction cleanup path.
- Improved long mixed-layout prose conversion.
- Made punctuation boundaries aware of the candidate keyboard-layout direction.
- Made physical word-cluster conversion atomic.
- Added local clause evidence for long ambiguous words containing user typos, without spell-correcting them.
- Expanded clipboard and long-context regression coverage.

Download `KeyFlip_windows_x64.exe`. It is a self-contained, untrimmed, single-file build for Windows 10/11 x64 and does not require a separate .NET runtime installation.

The SHA-256 checksum is included in the GitHub release description. Unsupported-only or otherwise non-restorable clipboard data still causes a safe abort before KeyFlip changes the clipboard.
