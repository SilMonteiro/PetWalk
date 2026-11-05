---
schemaVersion: 2
name: ".NET / C#"
skillName: "dotnet"
tier: 1
category: "language"
detection:
  extensions: [".csproj", ".fsproj", ".vbproj", ".sln", ".slnx", ".slnf", ".cs"]
docUrls:
  - "https://learn.microsoft.com/en-us/dotnet/csharp/"
keywords: ["dotnet", "csharp", "csproj", "sln", "aspnet"]
validation:
  provider: "dotnet"
  capability: "full-test"
---

# .NET / C# Stack

This project uses **.NET with C#**. When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://learn.microsoft.com/en-us/dotnet/csharp/` — the official documentation.
2. **Extensions**: `.cs` (source), `.csproj` (project), `.sln` (solution).

Prefer WebFetch on learn.microsoft.com URLs over generic WebSearch.

## Commands

- `dotnet new <template>` scaffolds a project or solution (e.g. `dotnet new sln`, `dotnet new xunit`).
- `dotnet test <manifest> --nologo` runs the test suite for a solution or project; add `--no-restore` after an explicit `dotnet restore <manifest>`.
- `dotnet build <manifest> --nologo --no-restore` compiles without running tests.

## Test frameworks

- xUnit, NUnit, and MSTest are the supported frameworks.
- Name test classes and files with the `Tests` suffix (e.g. `FooTests.cs`); the lowercase `*.test.cs` shape is also recognized.
- Keep tests in a `<Name>.Tests` project next to the source project (no `src/` prefix required).

## Manifests: solution versus solution filter

- `.sln` selects the whole solution; `.slnf` (solution filter, JSON with a `solution.path` string) selects a subset.
- Prefer the `.sln` for full validation; use the `.slnf` for scoped work on large repos.

## SDK pin via global.json

- A root `global.json` pins the .NET SDK version for the whole repo; honor it and do not suggest a different SDK.
- Without `global.json`, use the installed SDK (`dotnet --list-sdks`).

## NuGet restore diagnostics

- Offline feed failure: restore errors mentioning unreachable sources or `NU1301` mean the NuGet feed is unreachable — check network/VPN or a local feed, not the tests.
- Broken-test failure: restore succeeds but `dotnet test` fails — the failure is in the code under test, not in restore.
