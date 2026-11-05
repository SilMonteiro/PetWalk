---
schemaVersion: 2
name: "Delphi 11"
skillName: "delphi"
tier: 1
category: "language"
detection:
  extensions: [".dpr"]
docUrls:
  - "https://docwiki.embarcadero.com/RADStudio/en/Main_Page"
  - "https://docwiki.embarcadero.com/Libraries/en/Vcl"
keywords: ["delphi", "pascal", "vcl", "firemonkey", "embarcadero"]
---

# Delphi Stack

This project uses **Delphi 11 Alexandria** (Object Pascal). When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://docwiki.embarcadero.com/RADStudio/en/Main_Page` — the official documentation at docwiki.embarcadero.com first. Prefer WebFetch on docwiki URLs over generic WebSearch.
2. **Extensions**: `.pas`, `.dpr`, `.dfm` files.
3. **Alexandria baseline**: IDE services DelphiLSP and GetIt package manager, macOS ARM64 target shipped as universal binary, ASLR/DEP protections on by default, data access via FireDAC.
4. **Manifests**: project `.dpr` plus `.dproj` (MSBuild XML) plus `.groupproj` project group plus `.dpk` package.
5. **Build distinction**: `.dproj`-for-MSBuild vs `.dpr`-for-`dcc32` after running `rsvars.bat` to set the toolchain environment.
6. **DUnitX attributes**: `[TestFixture]` / `[Test]` / `[TestCase]` with console runner `TDUnitXConsoleLogger` and NUnit/JUnit XML output for CI.
7. **Test convention**: recommended `u*Tests.pas` / `Test*.pas` units under `tests/` (convention only, not framework-enforced).
8. **DFM rule**: `.dfm` stays as texto versionavel, never edited cru — always edit through the form designer.
9. **Pascal anchor rule**: the `.pas` collision with Free Pascal is resolved by the anchor rule — a unit counts as Delphi only under a `.dpr` / `.dproj` / `.groupproj` anchor (keyword anchor).
10. **UI and data**: VCL vs FireMonkey for UI, data access via TDataSet/TQuery.
11. **Local and CI command**: licensed IDE plus Windows required — documented here as instruction to the human, never as an executable recipe for the sandbox.
12. **No executable target by design** — this stack intentionally carries no dedicated runner entry and stays informational only.
