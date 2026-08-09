# KeyFlip Release Candidate Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Produce a safe KeyFlip release candidate that preserves user text and clipboard data, supports mixed RU/EN and symbol-only conversion, uses Ctrl+Shift+K by default, and carries a real application icon.

**Architecture:** Keep the existing WinForms/Win32 Copy→Convert→Paste architecture. Add small focused boundaries for protected-field detection, materialized clipboard snapshots, paste eligibility, local word scoring, and icon loading; keep `LayoutConverter` pure and retain the existing diagnostic logger.

**Tech Stack:** C# 12, .NET 8 WinForms, Win32 P/Invoke, built-in Windows UI Automation and Spell Checking COM APIs, no external packages or network calls.

## Global Constraints

- Work directly on `main`; preserve Git history and existing behavior.
- Data safety takes priority over correctness, compatibility, and UI.
- Never log selected, converted, password, clipboard, or file-list content.
- Preserve terminal exclusions and do not exclude `Code.exe`.
- No AI, cloud, network, telemetry, browser/VS Code extensions, or runtime dependencies.
- Write and observe failing tests before production changes; build after each integration phase.
- Do not claim Brave, VS Code, Word, password, or clipboard GUI success without manual verification.

---

### Task 1: Protect target text and synthetic modifiers

**Files:** Create `src/KeyFlip/ConversionGuard.cs`, `src/KeyFlip/ProtectedFieldDetector.cs`, `tests/KeyFlip.Tests/ConversionGuardTests.cs`; modify `NativeMethods.cs`, `InputSimulator.cs`, `KeyFlipContext.cs`, `KeyFlip.csproj`, and the test runner.

**Interfaces:** `ConversionGuard.CanPaste(string? source, string? converted)` enforces non-empty/change/convertible invariants. `ProtectedFieldDetector.IsFocusedControlProtected()` checks UI Automation `IsPassword`. `InputSimulator` waits for three stable all-up polls and always releases only its own synthetic Ctrl.

- [ ] Add failing tests proving empty, whitespace-only, unchanged, and non-convertible sources never permit Paste.
- [ ] Run `dotnet run --project tests/KeyFlip.Tests/KeyFlip.Tests.csproj --no-restore` and confirm the guard tests fail because the guard is absent.
- [ ] Implement the guard, left/right Ctrl/Shift/Alt/Win polling, pre-Copy/pre-Paste rechecks, synthetic Ctrl cleanup, password abort, and target HWND preservation.
- [ ] Run build/tests and commit `fix: protect editable text operations`.

### Task 2: Materialize and restore the original clipboard

**Files:** Create `src/KeyFlip/ClipboardSnapshot.cs`, `tests/KeyFlip.Tests/ClipboardSnapshotTests.cs`; modify `ClipboardService.cs`, `KeyFlipContext.cs`, and diagnostics.

**Interfaces:** `ClipboardSnapshot.TryCapture(out snapshot)` copies available formats before Ctrl+C, records empty/snapshot/format-count state, and `RestoreAsync` restores a new `DataObject` or the original empty state. Temporary Unicode data includes Windows history/cloud exclusion formats.

- [ ] Add failing tests for deep cloning strings, byte arrays, streams, file-drop arrays, empty-state handling, and unsupported delayed objects.
- [ ] Implement materialization for text, file drops, bitmaps/images, byte arrays, and streams; abort before Copy if a safe snapshot cannot be obtained.
- [ ] Restore in `finally`, dispose cloned resources only after persistent clipboard copy, and log safe transaction stages/counts.
- [ ] Run build/tests and commit `fix: preserve clipboard across conversion transactions`.

### Task 3: Support symbol-only and smart mixed conversion

**Files:** Create `src/KeyFlip/WordLanguageScorer.cs`, `src/KeyFlip/WindowsSpellChecker.cs`; modify `LayoutConverter.cs`, `LayoutConverterTests.cs`, and the test runner.

**Interfaces:** Separate EN→RU and RU→EN maps remain direction-specific. Pure single-script text is forced; no-letter text uses strong symbol evidence; mixed text tokenizes letter runs and conservatively selects original/candidate using local Windows spell checking with deterministic n-gram fallback.

- [ ] Add failing literal tests for the required mixed sentences, punctuation/spacing/newlines, `@#$^&` ↔ `\"№;:?`, and ambiguous symbol-only unchanged behavior.
- [ ] Implement symbol evidence and mixed token conversion with `IWordLanguageScorer`; uncertainty keeps the original token.
- [ ] Integrate optional local `ru-RU`/`en-US` Windows Spell Checking COM checks and a compact deterministic fallback.
- [ ] Run all mapping tests/build and commit `feat: support smart mixed and symbol conversion`.

### Task 4: Stabilize the universal compatibility pipeline

**Files:** Modify `KeyFlipContext.cs`, `ClipboardService.cs`, `InputSimulator.cs`, and `DiagnosticLogger.cs` only if evidence requires it.

- [ ] Verify diagnostics expose target process/HWND, copy observed, temporary clipboard ready, paste sent, modifier cleanup, restore, and elapsed time without content.
- [ ] Keep copy polling at 900 ms and use a conservative 300 ms paste-to-restore delay; add no process-specific timing without diagnostic evidence.
- [ ] Build/tests and commit only if this phase produces real code changes.

### Task 5: Change the default hotkey, add icon, and update documentation

**Files:** Modify `Settings.cs`, `SettingsService.cs`, `NativeInteropTests.cs`, `SettingsForm.cs`, `KeyFlipContext.cs`, `KeyFlip.csproj`, `README.md`; create `tooling/Generate-KeyFlipIcon.ps1`, `assets/KeyFlip.ico`, and settings migration tests.

- [ ] Add failing tests for default Ctrl+Shift+K, legacy Ctrl+Alt+K migration, and custom-hotkey preservation.
- [ ] Implement idempotent migration and persist it on settings load.
- [ ] Generate a multi-resolution electric-blue swap icon locally, embed it as `ApplicationIcon`, and use it for tray/settings.
- [ ] Rewrite the concise English README with RC-accurate claims and Ctrl+Shift+K instructions.
- [ ] Run build/tests and commit hotkey/icon/docs changes in meaningful commits.

### Task 6: Build and verify the release candidate

**Files:** Modify `build.ps1` only if needed; output ignored artifacts under `artifacts/KeyFlip-RC/`.

- [ ] Run clean solution build and the complete offline test runner.
- [ ] Publish to `artifacts/KeyFlip-RC` without overwriting a running old executable.
- [ ] Verify the executable exists, icon resources are embedded, default settings are Ctrl+Shift+K, source has no network clients, and Git has only the user-owned untracked input files.
- [ ] Report unresolved GUI-only verification honestly and provide manual tests A–T verbatim.
