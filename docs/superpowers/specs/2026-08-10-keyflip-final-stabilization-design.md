# KeyFlip Final Stabilization Design

## Goal

Produce the final `1.0.0-rc-final` candidate without replacing the existing application architecture: code syntax must remain intact, Word formatting must survive conversion, Explorer multi-dot names must be safe, the running build must be identifiable, and the release must be one self-contained x64 executable.

## Conversion boundaries

`LayoutConverter` remains the owner of physical US QWERTY ↔ Russian ЙЦУКЕН mapping and the existing general behavior. A compact `ConversionResult` adds `OutputText`, `Changed`, and ordered `ConversionEdit` spans. Pure policies reuse the existing word scorer:

- General mode keeps forced one-token conversion and direction-aware attached punctuation.
- Conservative word mode changes only alphabetic tokens that the scorer confidently recognizes as wrong-layout and leaves every separator unchanged.
- Code-safe mode uses conservative word mode and protects keywords, acronyms, and identifier-shaped tokens.
- Word mode returns word-only edit spans; no span may include whitespace, CR/LF, Word paragraph marks, or punctuation.

`CodeLikeDetector.LooksLikeCode` uses combinations of strong syntax markers. A lone quote or the `Code.exe` process alone is insufficient. The process is only supporting evidence; prose in VS Code continues through general mode.

## Word-safe path

`WordReplacementService` is the only new Word-specific component. It calls the Win32 `GetActiveObject` function for the already-running Word application, obtains the current selection, and verifies that `Selection.Application.ActiveWindow.Hwnd` matches the captured foreground window. It never starts Word.

The service reads selection text, calculates word-only edits, duplicates the selection range for each edit, narrows the duplicate with absolute `SetRange` positions, and assigns `Range.Text`. Edits are applied from the end toward the beginning. It never replaces the whole selection and never invokes clipboard or synthetic input. Any COM error, handle mismatch, missing selection, or invalid edit yields `WORD_SAFE_ABORT` with no clipboard fallback. COM access is guarded in production and is excluded from automated integration tests; automated coverage targets the pure edit calculation.

## Context priority and safety

The operation order is: password abort, confirmed VS Code terminal abort, Word targeted path, general Copy transport, then code/filename/general conversion after text is available. Code detection necessarily follows text acquisition but precedes filename/general conversion. Explorer rename is used only after conservative UIA confirmation; VS Code unknown remains fail-open as editor.

Immediately before the synthetic Copy, the foreground HWND is compared with the original HWND. If it changed, the operation aborts without copying or restoring a snapshot that was never modified. The existing pre-Paste check remains.

## Explorer filename policy

Confirmed rename text is split at the final dot. Dotfiles remain unchanged. The final dot and extension are copied exactly. Every basename segment is converted independently with conservative word decisions, while every internal dot is emitted literally. Explorer search/address fields remain on the general path.

## Settings, hotkey, and identity

Settings schema version 2 gates the one-time exact-default migration from `Ctrl+Alt+K` to `Ctrl+Shift+K`. Saving stamps schema 2, so a later user choice of `Ctrl+Alt+K` persists.

Disabled state unregisters the global hotkey; enabled state registers it. Hotkey replacement reports its actual state: if the new registration and previous-registration rollback both fail, the manager records no active hotkey.

`BuildInfo` exposes version `1.0.0-rc-final`, optional short source revision, and the running executable path. Startup diagnostics contain all three, and Settings shows the version. A second instance displays the required explanatory message instead of exiting silently.

## Build and documentation

The project remains .NET 8 WinForms and adds official single-file publish properties for `win-x64`, self-contained deployment, native library extraction, and no trimming. `build.ps1` restores, builds, runs the existing console test suite, publishes to a staging folder, and leaves exactly `artifacts/release/win-x64/KeyFlip.exe`. `install.ps1` targets this artifact and stops an existing KeyFlip process before updating. README documents actual modes, portability, x64 scope, permanent-path autostart guidance, and a .NET 10 LTS v1.1 roadmap item.

## Verification

Automated tests cover code detection/conversion, technical-token preservation, general punctuation regression, Word edit spans, filename segments/extensions, one-time migration, logical hotkey state, logger failure tolerance, and build identity. UIA and Word COM remain manual smoke tests. Final validation is a clean restore/build/test/publish, zero host crashes, exactly one release file, PE x64 and icon checks, SHA-256, file size, and the requested A–T manual checklist. No tag or GitHub release is created.
