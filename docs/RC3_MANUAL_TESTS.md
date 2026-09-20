# KeyFlip v1.0.1-rc3 manual checks

Run these checks against the published candidate on Windows x64.

1. Empty clipboard: clear the clipboard, convert `ghbdtn`, and confirm `привет`.
2. Existing text: copy `SOME OLD TEXT`, convert selected `ghbdtn`, confirm `привет`, then paste elsewhere and confirm the original clipboard text is restored.
3. Identical text: put `ghbdtn` in the clipboard, select another `ghbdtn`, convert it, and confirm both conversion and restoration of the original `ghbdtn`.
4. Screenshot: capture with `Win+Shift+S`, convert selected text, then paste into Paint and confirm the original screenshot is restored.
5. Explorer file copy: copy a file, convert selected text, then paste into a folder and confirm the original file operation remains available.
6. Rich browser text: copy formatted browser content, convert selected text, and confirm the original rich clipboard content remains usable.
7. Microsoft Word: confirm the formatting-preserving Word path still converts a selection without using the clipboard transaction.

For the deterministic local text transaction and identical-text sequence check, build the Release configuration and run:

```powershell
pwsh -NoProfile -Sta -File tooling\Verify-ClipboardTransaction.ps1
```

The script never prints clipboard contents and restores the original clipboard in `finally`.
