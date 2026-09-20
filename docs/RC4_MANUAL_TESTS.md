# KeyFlip v1.0.1-rc4 manual checks

Run these checks against the downloaded GitHub release asset on Windows x64.

1. In Notepad, convert exact TEST11 and confirm the exact expected full output.
2. In Notepad, convert exact TEST12 and confirm the exact expected full output.
3. Empty clipboard: convert `ghbdtn` and confirm `привет`.
4. Existing text: copy `SOME OLD TEXT`, convert selected `ghbdtn`, confirm `привет`, then paste elsewhere and confirm `SOME OLD TEXT` is restored.
5. Identical text: put `ghbdtn` in the clipboard, convert another selected `ghbdtn`, then confirm ordinary paste still produces the original `ghbdtn`.
6. Screenshot: copy a screenshot, convert selected text, then paste into Paint and confirm the screenshot is restored.

Development-only converter diagnostics:

```powershell
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj -c Release -- --convert-debug none TEST11
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj -c Release -- --convert-debug english TEST12
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj -c Release -- --convert-debug windows TEST11
```

The trace is printed only by the test executable. KeyFlip does not log selected text or clipboard contents.
