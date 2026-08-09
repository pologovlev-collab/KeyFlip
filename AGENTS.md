# KeyFlip Agent Rules

- Use C# with .NET 8 and WinForms for Windows x64.
- Keep dependencies to built-in .NET and Win32 P/Invoke where possible.
- Do not add AI, APIs, network access, telemetry, analytics, or cloud features.
- Never log, persist, or expose selected text or clipboard contents.
- Exclude terminal processes by default; do not exclude Code.exe.
- Prioritize browser, VS Code, and Notepad compatibility through Copy/Paste.
- Preserve clipboard data as far as standard IDataObject support permits.
- Maintain the complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping.
- Run converter unit tests after converter changes.
- Run a solution build after Windows integration changes.
- Avoid overengineering and do not expand scope without a user request.
- Keep source files focused and compact.
- Work directly on main unless the user requests another branch.

