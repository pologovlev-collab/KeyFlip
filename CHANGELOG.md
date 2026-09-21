# Changelog

## 1.1.0

- Promoted the fully validated RC4 converter and clipboard safeguards to the current stable release.
- Made long mixed-layout conversion deterministic without requiring Windows spell-check dictionaries.
- Preserved ambiguous physical clusters atomically while protecting technical tokens and English islands.
- Added robust empty, text, same-text, and image clipboard restoration.
- Displayed the current application version in muted text in the Settings window.
- Hardened clean builds and release verification against stale binaries or mismatched commits.

## 1.0.1-rc4 (pre-release)

- Made long mixed-layout conversion deterministic when Windows spell-check dictionaries are absent or only partially available.
- Added explicit clause-direction resolution while preserving hard technical tokens and strongly supported English islands.
- Ensured physical-cluster evidence is evaluated before optional spell-check validity, preventing partial or retained wrong-layout clusters.
- Added scorer-mode regression matrices and a development-only converter trace tool.
- Hardened release builds against stale assemblies and dirty production inputs.

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
