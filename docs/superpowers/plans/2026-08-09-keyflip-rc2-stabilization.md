# KeyFlip RC2 Stabilization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the four remaining confirmed conversion and context-safety bugs, preserve all working behavior, and publish a separate x64 RC2 build.

**Architecture:** Keep conversion decisions inside `LayoutConverter`, add one pure filename helper, and add one focused UIA adapter backed by pure context classifiers. `KeyFlipContext` reads the context before clipboard capture, aborts only a confirmed VS Code terminal, and selects filename conversion only for a confirmed Explorer rename editor.

**Tech Stack:** C# 12, .NET 8, WinForms, built-in Windows UI Automation, Win32 P/Invoke, existing console regression runner.

## Global Constraints

- Work directly on `main`; do not rewrite Git history.
- Do not modify `ClipboardService`, `InputSimulator`, Win32 INPUT layout, hotkey, tray, autostart, icon, or password behavior.
- Add no package, network, AI, API, cloud, telemetry, or analytics dependency.
- Never log selected text, converted text, filenames, UIA Name, terminal commands, passwords, or clipboard contents.
- `Code.exe`: confirmed terminal aborts; confirmed editor and unknown continue.
- `explorer.exe`: preserve extensions only for a confirmed rename Edit under a file-list item.
- Run converter tests after converter changes and a solution build after Windows integration changes.
- Publish to `artifacts\KeyFlip-RC2\KeyFlip.exe`; do not overwrite RC1 and do not create v1.0.0.

---

### Task 1: Smart multi-word conversion and attached punctuation

**Files:**
- Modify: `src/KeyFlip/LayoutConverter.cs`
- Modify: `tests/KeyFlip.Tests/LayoutConverterTests.cs`

**Interfaces:**
- Consumes: existing `MixedWordDecider.ShouldUseConverted(string, WordLanguage, string, WordLanguage)` and the two physical-key maps.
- Produces: unchanged public signature `string LayoutConverter.Convert(string text)` with token-count routing and direction-aware separator conversion.

- [ ] **Step 1: Add failing literal regression cases**

Add these calls to `LayoutConverterTests.Run`:

```csharp
Converts("как руддщ дела", "как hello дела");
Converts("руддщ рщц фку нщг", "hello how are you");
Converts("привет руддщ мир", "привет hello мир");
Converts("hello ghbdtn world", "hello привет world");
Unchanged("как дела");
Unchanged("hello world");
Converts("Это ghbdtn@ текст", "Это привет\" текст");
Converts("как руддщ№ дела", "как hello# дела");
Converts("ghbdtn@", "привет\"");
Converts("руддщ№", "hello#");
Converts("ghbdtn\r\n", "привет\r\n");
Converts("ghbdtn\nnext", "привет\nnext");
Converts("ghbdtn\r", "привет\r");
```

Retain the existing single-token, symbol-only, mixed selection, and full physical mapping tests.

- [ ] **Step 2: Run RED**

Run:

```powershell
dotnet build KeyFlip.sln --no-restore -p:UseSharedCompilation=false -m:1
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj --no-build
```

Expected: the runner fails first on `как руддщ дела` because the current same-script branch force-converts every word.

- [ ] **Step 3: Replace script-count routing with word-count routing**

In `LayoutConverter.Convert`, count contiguous alphabetic runs with `CountAlphabeticTokens`. Route zero tokens to `ConvertSymbolOnly`, one token to `ConvertSingleToken`, and two or more to `ConvertSmart`:

```csharp
var tokenCount = CountAlphabeticTokens(text);
if (tokenCount == 0) return ConvertSymbolOnly(text);
if (tokenCount == 1) return ConvertSingleToken(text);
return ConvertSmart(text);
```

`ConvertSingleToken` finds the token language and applies the corresponding physical map to the complete selection, preserving the explicit forced behavior and converting attached mapped punctuation.

- [ ] **Step 4: Make smart word decisions explicit**

Add a private direction enum and word record:

```csharp
private enum ConversionDirection { None, EnglishToRussian, RussianToEnglish }
private readonly record struct WordToken(int Start, int End, WordLanguage Language, ConversionDirection Direction);
```

Build all tokens first. For each token, create its candidate with the matching map and call the existing decider. Store `None` for keep and the script-specific direction for convert.

- [ ] **Step 5: Convert separators using neighboring word decisions**

Construct the output in original order. Convert each changed word with its stored map. For separators:

```csharp
// before first word: only the suffix after the last whitespace belongs to the first word
// between words with no whitespace: the complete run belongs to the preceding word
// between words with whitespace: prefix belongs to preceding, suffix belongs to following
// after last word: only the prefix before the first whitespace belongs to the last word
```

Use the selected direction's map only when the owner word converted. Append whitespace and all unowned characters exactly, without normalizing line endings.

- [ ] **Step 6: Run GREEN and full converter regression suite**

Run the same build and test commands. Expected: all previous 245 checks plus 13 new literal checks pass with zero build warnings/errors.

- [ ] **Step 7: Commit**

```powershell
git add src/KeyFlip/LayoutConverter.cs tests/KeyFlip.Tests/LayoutConverterTests.cs
git commit -m "fix: improve smart multi-word conversion"
```

---

### Task 2: Conservative VS Code Integrated Terminal detection

**Files:**
- Create: `src/KeyFlip/FocusedContextDetector.cs`
- Create: `tests/KeyFlip.Tests/FocusedContextClassifierTests.cs`
- Modify: `src/KeyFlip/KeyFlipContext.cs`
- Modify: `tests/KeyFlip.Tests/Program.cs`

**Interfaces:**
- Produces `FocusedTargetContext FocusedContextDetector.Detect(string executableName)`.
- Produces pure `VsCodeContext FocusedContextClassifier.ClassifyVsCode(IReadOnlyList<UiElementDescriptor> elements)`.
- `KeyFlipContext` consumes the result before `CaptureSnapshotAsync`.

- [ ] **Step 1: Add failing pure classifier tests**

Create tests with literal descriptors for these outcomes:

```csharp
Equal(VsCodeContext.Terminal, Classify(
    new(UiControlKind.Edit, "xterm-helper-textarea", "", "Chrome")));
Equal(VsCodeContext.Terminal, Classify(
    new(UiControlKind.Document, "", "terminal.integrated.instance", "Chrome")));
Equal(VsCodeContext.Editor, Classify(
    new(UiControlKind.Document, "", "monaco-editor", "Chrome")));
Equal(VsCodeContext.Unknown, Classify(
    new(UiControlKind.Edit, "", "Chrome_RenderWidgetHostHWND", "Chrome")));
Equal(VsCodeContext.Unknown, Classify(
    new(UiControlKind.Document, "terminal", "", "Chrome")));
```

The last two tests prevent weak Chromium/control-type or generic `terminal` evidence from blocking the editor.

- [ ] **Step 2: Register the test class and run RED**

Add `FocusedContextClassifierTests` to `tests/KeyFlip.Tests/Program.cs`. Build and run the runner. Expected: compile failure because the classifier types do not exist.

- [ ] **Step 3: Implement pure descriptors and conservative classifier**

Create:

```csharp
internal enum UiControlKind { Unknown, Edit, Document, List, ListItem, DataItem }
internal enum VsCodeContext { Editor, Terminal, Unknown }
internal enum FocusedTargetContext { Default, VsCodeEditor, VsCodeTerminal, VsCodeUnknown, ExplorerFileRename }
internal readonly record struct UiElementDescriptor(
    UiControlKind ControlKind, string AutomationId, string ClassName, string FrameworkId);
```

Normalize `AutomationId` and `ClassName` with `OrdinalIgnoreCase`. Terminal is confirmed only by `xterm`, `terminal.integrated`, or `terminal-instance` markers. Editor is confirmed only by `monaco-editor` or `code-editor`. Terminal wins if both appear; all weak evidence returns `Unknown`.

- [ ] **Step 4: Implement the bounded UIA adapter**

`FocusedContextDetector.Detect` returns `Default` for every process except `Code.exe` and `explorer.exe`. For relevant processes it reads `AutomationElement.FocusedElement`, maps the focused element plus at most 12 `TreeWalker.ControlViewWalker` parents to descriptors, and catches `ElementNotAvailableException`, `InvalidOperationException`, and `COMException`. It never stores or exposes `AutomationElement.Current.Name`.

- [ ] **Step 5: Abort only confirmed terminal before clipboard work**

Add `_focusedContextDetector` to `KeyFlipContext`. Immediately after `PROTECTED_FIELD_ABORT` handling and before `CaptureSnapshotAsync`, classify the target:

```csharp
case FocusedTargetContext.VsCodeTerminal:
    _logger.Log("VSCODE_CONTEXT_TERMINAL", "process=Code.exe");
    _logger.Log("VSCODE_TERMINAL_ABORT", "process=Code.exe");
    return;
case FocusedTargetContext.VsCodeEditor:
    _logger.Log("VSCODE_CONTEXT_EDITOR", "process=Code.exe");
    break;
case FocusedTargetContext.VsCodeUnknown:
    _logger.Log("VSCODE_CONTEXT_UNKNOWN", "process=Code.exe");
    break;
```

Do not change Copy/Paste, cleanup, restore, or foreground checks.

- [ ] **Step 6: Build and run all tests**

Run the solution build and runner. Expected: all checks pass, zero errors/warnings, and diagnostics contain classification only—no UIA Name.

- [ ] **Step 7: Commit**

```powershell
git add src/KeyFlip/FocusedContextDetector.cs src/KeyFlip/KeyFlipContext.cs tests/KeyFlip.Tests/FocusedContextClassifierTests.cs tests/KeyFlip.Tests/Program.cs
git commit -m "fix: ignore VS Code integrated terminal"
```

---

### Task 3: Preserve extensions only during confirmed Explorer rename

**Files:**
- Create: `src/KeyFlip/FileNameConverter.cs`
- Create: `tests/KeyFlip.Tests/FileNameConverterTests.cs`
- Modify: `src/KeyFlip/FocusedContextDetector.cs`
- Modify: `tests/KeyFlip.Tests/FocusedContextClassifierTests.cs`
- Modify: `src/KeyFlip/KeyFlipContext.cs`
- Modify: `tests/KeyFlip.Tests/Program.cs`

**Interfaces:**
- Produces `string FileNameConverter.ConvertForRename(string fileName)`.
- Produces pure `bool FocusedContextClassifier.IsExplorerFileRename(IReadOnlyList<UiElementDescriptor> elements)`.
- `KeyFlipContext` calls the filename helper only for `FocusedTargetContext.ExplorerFileRename`.

- [ ] **Step 1: Add failing filename tests**

Create literal checks:

```csharp
Converts("ghbdtn.zip", "привет.zip");
Converts("руддщ.txt", "hello.txt");
Converts("ghbdtn.exe", "привет.exe");
Converts("ghbdtn", "привет");
Unchanged(".gitignore");
Converts("ghbdtn.JPG", "привет.JPG");
```

- [ ] **Step 2: Add failing Explorer context tests**

Add pure descriptor cases:

```csharp
True(IsRename(
    new(UiControlKind.Edit, "", "Edit", "Win32"),
    new(UiControlKind.ListItem, "", "", "Win32")));
False(IsRename(
    new(UiControlKind.Edit, "SearchBox", "Edit", "Win32"),
    new(UiControlKind.Unknown, "Toolbar", "", "Win32")));
False(IsRename(
    new(UiControlKind.Document, "", "", "Chrome"),
    new(UiControlKind.ListItem, "", "", "Win32")));
```

Register `FileNameConverterTests` in the runner and run RED. Expected: compile failure because the filename helper and Explorer classifier method do not exist.

- [ ] **Step 3: Implement the pure filename helper**

```csharp
internal static string ConvertForRename(string fileName)
{
    ArgumentNullException.ThrowIfNull(fileName);
    var dot = fileName.LastIndexOf('.');
    if (dot == 0) return fileName;
    if (dot < 0) return LayoutConverter.Convert(fileName);
    return LayoutConverter.Convert(fileName[..dot]) + fileName[dot..];
}
```

- [ ] **Step 4: Implement Explorer classification**

Return true only when descriptor zero is `UiControlKind.Edit` and a later descriptor is `ListItem` or `DataItem`. In `FocusedContextDetector.Detect`, map confirmed `explorer.exe` rename to `ExplorerFileRename`; ambiguous/search/address contexts return `Default`.

- [ ] **Step 5: Select filename conversion after Copy**

Store the focused context before clipboard capture. Replace only the conversion call:

```csharp
var converted = focusedContext == FocusedTargetContext.ExplorerFileRename
    ? FileNameConverter.ConvertForRename(copyResult.Text)
    : LayoutConverter.Convert(copyResult.Text);
```

No filename or copied text is logged.

- [ ] **Step 6: Run GREEN, all tests, and solution build**

Expected: filename and context tests pass; Explorer search continues down the normal conversion branch; all prior tests remain green with zero warnings/errors.

- [ ] **Step 7: Commit**

```powershell
git add src/KeyFlip/FileNameConverter.cs src/KeyFlip/FocusedContextDetector.cs src/KeyFlip/KeyFlipContext.cs tests/KeyFlip.Tests/FileNameConverterTests.cs tests/KeyFlip.Tests/FocusedContextClassifierTests.cs tests/KeyFlip.Tests/Program.cs
git commit -m "fix: preserve file extensions during Explorer rename"
```

---

### Task 4: Clean verification and RC2 publish

**Files:**
- Generated, ignored output: `artifacts/KeyFlip-RC2/*`
- No source file changes expected.

**Interfaces:**
- Produces: `D:\progect\KeyFlip\artifacts\KeyFlip-RC2\KeyFlip.exe`.

- [ ] **Step 1: Verify scope and privacy**

Run `git diff --check`, inspect `git status --short`, confirm only user-owned `hih.txt` and `итоговые_правки.md` are untracked, and scan tracked `src/KeyFlip/*.cs` plus the project file for network/AI/telemetry/package references. Inspect logger calls to confirm no text, filename, UIA Name, command, or clipboard content is logged.

- [ ] **Step 2: Remove only a prior RC2 output after validating its absolute path**

Resolve `D:\progect\KeyFlip\artifacts\KeyFlip-RC2`, verify it is inside the workspace, and remove that directory if present. Do not touch `artifacts\KeyFlip-RC`.

- [ ] **Step 3: Clean and rebuild**

```powershell
dotnet clean KeyFlip.sln --configuration Debug -p:UseSharedCompilation=false -m:1
dotnet clean KeyFlip.sln --configuration Release -p:UseSharedCompilation=false -m:1
dotnet build KeyFlip.sln --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
dotnet run --project tests\KeyFlip.Tests\KeyFlip.Tests.csproj --configuration Release --no-build
```

Expected: zero errors, zero warnings, and the final assertion count printed by the runner.

- [ ] **Step 4: Publish RC2 without overwriting RC1**

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 `
    -OutputDirectory .\artifacts\KeyFlip-RC2
```

- [ ] **Step 5: Verify the artifact**

Confirm `KeyFlip.exe` exists, PE machine is `0x8664`, `Icon.ExtractAssociatedIcon` succeeds, compute SHA-256, and confirm tracked worktree cleanliness. Do not create or publish v1.0.0.
