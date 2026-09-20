# Changelog

## 1.0.1-rc3 (pre-release)

- Fixed clipboard transactions being cancelled by optional unsupported formats in an otherwise restorable clipboard.
- Added complete, partial, empty, and unusable clipboard snapshot handling with content-free diagnostics.
- Kept restored streams, images, file drops, text, and other supported values independent from snapshot disposal.
- Improved long mixed-layout prose conversion with direction-aware punctuation, atomic physical clusters, and local clause evidence for typo-containing words.
- Expanded clipboard and long-context regression coverage.

## 1.0.0

- Initial public release.
- Smart Russian/English selected-text conversion with complete physical keyboard mapping.
- Code-aware conversion that preserves existing syntax and recovers confidently detected wrong-layout syntax.
- Formatting-preserving targeted replacements in Microsoft Word.
- File Explorer filename conversion, including names with spaces, with extension preservation.
- Clipboard snapshot and restoration safeguards.
- Configurable global hotkey, system tray controls, settings, and Windows autostart.
