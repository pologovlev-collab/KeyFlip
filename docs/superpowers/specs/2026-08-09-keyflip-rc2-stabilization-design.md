# KeyFlip RC2 Stabilization Design

## Scope

This pass fixes only four confirmed regressions: same-script multi-word conversion, punctuation attached to converted words, VS Code Integrated Terminal activation, and Explorer rename extension conversion. It also adds converter-level line-ending regression coverage for the unconfirmed Word edge case. ClipboardService, InputSimulator, the password guard, hotkey, tray, icon, and autostart remain unchanged.

## Conversion

`LayoutConverter.Convert` first counts contiguous alphabetic word tokens rather than only counting scripts.

- Zero word tokens keep the existing symbol-only direction logic.
- Exactly one word token keeps forced physical-layout conversion for the whole selection. This preserves the explicit single-word behavior and converts supported attached punctuation in the same direction.
- Two or more word tokens always use smart per-word conversion, even when all words use the same script.

Smart mode tokenizes contiguous Latin or Russian letters as words and preserves every other character in place. For each word it uses the existing `MixedWordDecider` and records whether the decision was keep, English-to-Russian, or Russian-to-English. Supported mapped punctuation directly adjacent to a converted word is converted using that word's direction; punctuation adjacent to a kept word is kept. A punctuation run between two words belongs to the word immediately before it, while leading punctuation at the beginning of text or after whitespace belongs to the following word. Whitespace, including spaces, tabs, `\r`, `\n`, and `\r\n`, is copied exactly.

## Focused Application Context

A new focused-context component is separate from `ProtectedFieldDetector`. It reads the focused `AutomationElement` and a bounded ancestor chain through `TreeWalker.ControlViewWalker`, converts those elements into small descriptors, and passes them to pure classifiers. UI Automation exceptions return `Unknown`; UIA `Name` may be inspected in memory only when necessary and is never returned as diagnostic metadata or logged.

### VS Code

The classifier runs only when the foreground executable is `Code.exe`.

- `Terminal` requires a strong terminal marker in `AutomationId` or `ClassName`, such as an `xterm` marker, on the focused element or an ancestor. A generic Edit/Document control, read-only state, Chromium class, or a weak textual label is insufficient by itself.
- `Editor` requires a strong editor/Monaco marker in the same safe properties.
- All other cases are `Unknown`.

`Terminal` logs `VSCODE_CONTEXT_TERMINAL` and `VSCODE_TERMINAL_ABORT`, then exits before clipboard snapshot and Ctrl+C. `Editor` logs `VSCODE_CONTEXT_EDITOR`. `Unknown` logs `VSCODE_CONTEXT_UNKNOWN` and continues. No UIA name or terminal content enters diagnostics.

### Explorer Rename

The classifier runs only when the foreground executable is `explorer.exe`. A rename context is confirmed only when the focused control is an Edit and its ancestor chain contains a file-list `ListItem` or `DataItem`. Explorer search and address controls lack that ancestry and remain normal conversion contexts. UIA failures or ambiguous ancestry return `Unknown` and retain normal conversion.

## Filename Conversion

A pure filename helper is called only for a confirmed Explorer rename context. It finds the last dot. With no dot it converts the entire name. If the last dot is the first character, it returns the dotfile unchanged. Otherwise it converts only the basename through `LayoutConverter.Convert` and appends the final dot and suffix exactly as received, preserving case and characters.

## Data Flow and Safety

After modifier release and foreground/exclusion checks, KeyFlip evaluates the focused application context before password detection, clipboard snapshot, or Ctrl+C. A confirmed VS Code terminal returns immediately. A confirmed Explorer rename sets a local conversion mode; no UIA element is retained across asynchronous clipboard work. After Copy, normal text uses `LayoutConverter.Convert`, while a confirmed rename uses the filename helper. The existing foreground recheck, temporary clipboard, paste, restore, and synthetic-input cleanup are unchanged.

## Testing

Converter tests cover all requested same-script multi-word, single-token, attached-punctuation, symbol-only, and line-ending cases while retaining the full physical mapping suite. Pure filename tests cover `.zip`, `.txt`, no extension, case-preserved suffixes, and dotfiles. Pure focused-context classifier tests cover strong xterm, strong Monaco editor, weak/ambiguous Code context, Explorer Edit under ListItem, and Explorer Edit outside a file-list item. Real VS Code and Explorer UIA trees remain manual smoke tests; no fake AutomationElement framework is introduced.

## Release

Each behavior is implemented test-first and committed in three focused commits: conversion, VS Code terminal safety, and Explorer rename extension preservation. Final verification performs clean builds, all tests, privacy/dependency scans, and publishes a separate x64 release candidate to `artifacts\KeyFlip-RC2\KeyFlip.exe`. No v1.0.0 release is created.
