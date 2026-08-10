# KeyFlip architecture

KeyFlip is a .NET 8 Windows x64 tray application. It registers a configurable global hotkey and performs each conversion against the window that was active when the hotkey was pressed.

## General conversion path

For most applications KeyFlip captures a materialized snapshot of the clipboard, sends Copy, converts the selected Unicode text, pastes the result, and restores the snapshot. It checks the foreground window immediately before Copy and Paste. Unsupported delayed clipboard formats cause an early abort instead of risking clipboard data.

Selections with one alphabetic token use forced physical keyboard-layout conversion. Multi-word selections use local Windows spell checking when available plus a deterministic fallback to change only confidently mistyped words. The complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping includes Shift and symbol keys.

## Specialized contexts

- **Code:** Pure syntax evidence selects a code-aware conversion policy. A safe candidate changes only confidently wrong-layout word tokens and preserves existing ASCII punctuation. A targeted syntax candidate starts from the safe result and changes only strong syntax-position evidence: paired wrong-layout quote delimiters and a wrong-layout statement terminator immediately after a closing bracket or confirmed closing quote. It never physically converts all Cyrillic content. Ambiguous input therefore keeps the safe result. VS Code editor context provides additional evidence for short selections; confirmed integrated terminals always abort.
- **Microsoft Word:** Word uses the running Word COM Object Model instance. KeyFlip calculates small edit spans, duplicates the current selection range, and applies edits from end to start. It does not replace the entire selection or use the clipboard. Any uncertain Word attachment or window mismatch aborts without a fallback.
- **File Explorer:** UI Automation first distinguishes rename, search, address, and unknown edit contexts. Confirmed rename always uses filename conversion, including names with spaces. An unknown Explorer edit may use a conservative filename-shape fallback after Copy. Internal dots and the final extension are preserved exactly.

When autostart is enabled, each normal application startup best-effort refreshes the current-user Run entry from `Environment.ProcessPath`. Moving the portable executable and launching it manually therefore repairs the stored path without an installer framework.

## Safety and privacy

UI Automation is limited to password detection, VS Code terminal/editor classification, and Explorer context classification. Password controls and configured terminal processes abort before Copy. UIA names, selected text, converted text, filenames, clipboard contents, and commands are never logged.

Diagnostics contain only operational stages, process/context classification, version, commit, and executable path. KeyFlip has no network code, AI, account system, telemetry, analytics, or cloud integration.
