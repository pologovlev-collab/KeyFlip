# Contributing to KeyFlip

Thank you for helping improve KeyFlip.

## Before opening a pull request

- Keep the application local and offline. Do not add telemetry, analytics, cloud services, or network features.
- Never log, persist, or expose selected text, terminal commands, or clipboard contents.
- Keep dependencies to built-in .NET and Win32 APIs where practical.
- Preserve the complete physical US QWERTY ↔ Russian ЙЦУКЕН mapping.
- Add focused regression tests for conversion changes.

## Build and test

Requirements: Windows and the .NET 8 SDK.

```powershell
dotnet restore src/KeyFlip/KeyFlip.csproj --runtime win-x64 -p:NuGetAudit=false
dotnet restore tests/KeyFlip.Tests/KeyFlip.Tests.csproj --runtime win-x64 -p:NuGetAudit=false
dotnet build KeyFlip.sln -c Release --no-restore
dotnet run --project tests/KeyFlip.Tests/KeyFlip.Tests.csproj -c Release --no-build --no-restore
```

For a complete release build, including the standalone executable and checksum:

```powershell
.\build.ps1
```

Please keep pull requests focused and describe the user-visible behavior being changed.
