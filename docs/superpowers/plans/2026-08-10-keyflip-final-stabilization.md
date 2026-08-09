# KeyFlip Final Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the testable `1.0.0-rc-final` candidate with code-safe conversion, Word targeted replacements, reliable Explorer filenames, explicit runtime identity, truthful hotkey state, and a single self-contained x64 EXE.

**Architecture:** Keep the clipboard/input pipeline intact and add small pure conversion policies plus one isolated late-bound Word COM service. Windows integrations consume pure helpers and fail safely; tests exercise pure behavior and injected native hotkey operations instead of interactive desktop/UIA/Word contexts.

**Tech Stack:** C# 12, .NET 8 Windows, WinForms, UI Automation, built-in COM/Win32 P/Invoke, PowerShell, no new packages.

## Global Constraints

- Preserve selected text and clipboard privacy; never log content.
- Keep the complete physical US QWERTY ↔ Russian mapping.
- `TERMINAL_CONFIRMED` aborts; VS Code unknown continues.
- Word COM uncertainty aborts without clipboard fallback.
- No AI, network features, telemetry, trimming, target-framework migration, installer framework, or GitHub release/tag.
- Final output is exactly `artifacts/release/win-x64/KeyFlip.exe` for Windows x64.

---

### Task 1: Pure conversion results and code-safe policy

**Files:**
- Create: `src/KeyFlip/ConversionResult.cs`
- Create: `src/KeyFlip/CodeLikeDetector.cs`
- Modify: `src/KeyFlip/LayoutConverter.cs`
- Modify: `src/KeyFlip/WordLanguageScorer.cs`
- Create: `tests/KeyFlip.Tests/CodeSafeConversionTests.cs`
- Modify: `tests/KeyFlip.Tests/LayoutConverterTests.cs`
- Modify: `tests/KeyFlip.Tests/Program.cs`

**Interfaces:**
- Produces: `ConversionResult LayoutConverter.ConvertCodeSafe(string)`, `ConversionResult LayoutConverter.ConvertWords(string, bool forceSingleToken)`, `bool CodeLikeDetector.LooksLikeCode(string, bool)`.

- [ ] Add literal failing tests for every requested C++/Python/JSON example, technical acronyms, identifiers, and the general attached-punctuation regression.
- [ ] Run the test project and confirm failures are caused by missing code-safe APIs or syntax punctuation conversion.
- [ ] Add immutable edit/result records and conservative word-token conversion that never includes separators in edits.
- [ ] Add strong-marker code detection and compact technical-token guards.
- [ ] Run all tests and refactor only duplicated token scanning.

### Task 2: Word edit planning and safe COM replacement

**Files:**
- Create: `src/KeyFlip/WordReplacementService.cs`
- Create: `tests/KeyFlip.Tests/WordConversionTests.cs`
- Modify: `src/KeyFlip/KeyFlipContext.cs`
- Modify: `tests/KeyFlip.Tests/Program.cs`

**Interfaces:**
- Consumes: `LayoutConverter.ConvertWords(text, forceSingleToken: true)` and `ConversionResult.Edits`.
- Produces: `WordReplacementResult WordReplacementService.TryReplaceSelection(IntPtr, DiagnosticLogger)`.

- [ ] Add failing pure tests showing only wrong words become spans and CR/LF/paragraph/table separators never enter a span.
- [ ] Run tests and verify the requested literal spans are absent.
- [ ] Implement late-bound `GetActiveObject`, foreground Word-window verification, selection-range duplication, reverse-order `SetRange`, and guarded `Range.Text` assignment.
- [ ] Integrate Word before clipboard capture; log safe stage names only and abort on any uncertainty.
- [ ] Build and run all non-interactive tests; do not instantiate Word or AutomationElement in tests.

### Task 3: Explorer filename and foreground race

**Files:**
- Modify: `src/KeyFlip/FileNameConverter.cs`
- Modify: `src/KeyFlip/KeyFlipContext.cs`
- Modify: `tests/KeyFlip.Tests/FileNameConverterTests.cs`

**Interfaces:**
- Consumes: conservative word conversion from Task 1.

- [ ] Add failing tests for `ghbdtn.vbh.zip`, `руддщ.ntcn.txt`, `photo.JPG`, extension case, and dotfiles.
- [ ] Run tests and confirm the current whole-basename mapping corrupts internal dots.
- [ ] Split basename segments, preserve literal dots/final extension, and convert segments independently.
- [ ] Add an HWND equality check immediately before Copy; if it fails, dispose the read-only snapshot without a clipboard restore write.
- [ ] Run filename/converter tests and solution build.

### Task 4: One-time settings migration and truthful hotkey state

**Files:**
- Modify: `src/KeyFlip/Settings.cs`
- Modify: `src/KeyFlip/SettingsService.cs`
- Modify: `src/KeyFlip/HotkeyManager.cs`
- Modify: `src/KeyFlip/KeyFlipContext.cs`
- Modify: `tests/KeyFlip.Tests/SettingsServiceTests.cs`
- Create: `tests/KeyFlip.Tests/HotkeyManagerTests.cs`
- Modify: `tests/KeyFlip.Tests/Program.cs`

**Interfaces:**
- Produces: schema-versioned settings and `HotkeyManager.IsRegistered`; native register/unregister delegates are injectable internally for pure logical tests.

- [ ] Add failing migration tests for a legacy file, a second load, and manually restored `Ctrl+Alt+K` under schema 2.
- [ ] Add failing manager tests for disable/unregister, successful rollback, and failed rollback leaving `IsRegistered == false`.
- [ ] Implement schema version 2 normalization/save and injected hotkey native operations.
- [ ] Update apply/startup behavior so disabled settings never register and rollback error messages reflect actual active state.
- [ ] Run all tests.

### Task 5: Build identity, logger resilience, and second instance

**Files:**
- Create: `src/KeyFlip/BuildInfo.cs`
- Modify: `src/KeyFlip/KeyFlip.csproj`
- Modify: `src/KeyFlip/Program.cs`
- Modify: `src/KeyFlip/SettingsForm.cs`
- Modify: `src/KeyFlip/KeyFlipContext.cs`
- Modify: `src/KeyFlip/DiagnosticLogger.cs`
- Create: `tests/KeyFlip.Tests/BuildInfoTests.cs`
- Create: `tests/KeyFlip.Tests/DiagnosticLoggerTests.cs`
- Modify: `tests/KeyFlip.Tests/Program.cs`

**Interfaces:**
- Produces: `BuildInfo.Version`, `BuildInfo.Commit`, `BuildInfo.ProcessPath` and a testable logger path constructor.

- [ ] Add failing tests for nonempty release version/absolute process path and logger write failure not escaping.
- [ ] Add assembly/package version metadata and optional `SourceRevisionId` parsing.
- [ ] Add startup version/commit/path diagnostics, Settings footer, and the second-instance message.
- [ ] Run all tests and a Release build with zero errors.

### Task 6: Self-contained release scripts and docs

**Files:**
- Modify: `src/KeyFlip/KeyFlip.csproj`
- Modify: `build.ps1`
- Modify: `install.ps1`
- Modify: `README.md`

**Interfaces:**
- Produces: one release artifact at `artifacts/release/win-x64/KeyFlip.exe`.

- [ ] Configure `PublishSingleFile`, `SelfContained`, `RuntimeIdentifier=win-x64`, `IncludeNativeLibrariesForSelfExtract`, `PublishTrimmed=false`, and no release PDB.
- [ ] Make `build.ps1` restore, build, test, publish with the short Git revision, and clean the exact final output directory before copying only `KeyFlip.exe`.
- [ ] Point `install.ps1` at the final folder, stop only existing `KeyFlip` processes, copy the executable, preserve autostart behavior, and launch hidden/default as appropriate.
- [ ] Update README for actual modes, Word/Explorer behavior, standalone x64 deployment, permanent autostart path, and v1.1 .NET 10 LTS roadmap.
- [ ] Run `build.ps1` from the repository state and confirm exactly one output file.
- [ ] Run the final test executable directly, inspect PE architecture/icon, scan source for forbidden network/telemetry additions and content logging, compute size/SHA-256, and record warnings/errors.
- [ ] Prepare the A–T manual checklist without creating a tag or release.
