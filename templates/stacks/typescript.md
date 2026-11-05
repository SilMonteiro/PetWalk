---
schemaVersion: 2
name: "TypeScript"
skillName: "typescript"
tier: 1
category: "language"
detection:
  extensions: [".ts", ".tsx", ".mts", ".cts"]
docUrls:
  - "https://www.typescriptlang.org/docs/"
  - "https://www.typescriptlang.org/tsconfig/"
  - "https://devblogs.microsoft.com/typescript/"
keywords: ["typescript", "ts", "tsconfig", "tsc", "type-safety", "esm", "commonjs"]
validation:
  provider: "typescript-node"
  capability: "full-test"
---

# TypeScript Stack

This project uses **TypeScript**. Apply the guidance below when analyzing, implementing, reviewing, or validating changes.

## Project evidence

Before proposing changes, inspect the project's existing evidence:

- Read the canonical `tsconfig.json` and any configuration it extends.
- Inspect `package.json`, the available lockfile, existing scripts, and the installed TypeScript version.
- Identify the actual runtime, bundler, module system, test runner, and monorepo structure.
- Check `module`, `moduleResolution`, `target`, `lib`, `strict`, `include`, `exclude`, `files`, `composite`, and project references when relevant.
- Do not assume npm, pnpm, yarn, Bun, Node.js, a particular bundler, or a particular test framework.
- Do not replace an existing project convention without repository evidence.
- Do not create a new `tsconfig.json` when the project already has a canonical configuration.

## Type safety

- Preserve public types, external contracts, and observable behavior unless a breaking change was explicitly requested.
- Prefer `unknown` to `any` at untrusted or external boundaries, then narrow before use.
- Use discriminated unions when distinct states require distinct data.
- Use exhaustive checks when they materially protect state handling.
- Avoid type assertions and non-null assertions unless repository evidence makes them safe.
- Do not use `@ts-ignore` to hide an error. When suppression is unavoidable, prefer a narrowly scoped `@ts-expect-error` with a short justification.
- Derive types from existing contracts when practical instead of duplicating them manually.
- When runtime validation and TypeScript types describe the same shape, compare them field by field and keep them synchronized.
- Do not impose a specific runtime validation library.

## Modules and runtime

- Determine ESM or CommonJS behavior from `package.json`, TypeScript configuration, runtime, and build tooling.
- Respect the semantic differences between `.ts`, `.tsx`, `.mts`, and `.cts`.
- Do not change `module` or `moduleResolution` in isolation.
- Verify compatibility among TypeScript, the runtime, bundler, loader, package exports, and test executor.
- Preserve the repository's established import and export conventions.
- Do not mix `require` and `import` without a demonstrated interoperability requirement.
- Respect package `exports` and `imports` when the project uses them.

## Compiler configuration

- Consult the official TSConfig Reference before changing compiler options.
- Do not copy a generic configuration without considering the target runtime and toolchain.
- Evaluate `include`, `exclude`, `files`, project references, and emitted artifacts together.
- Preserve `incremental`, `composite`, and declaration settings when existing workflows depend on them.
- Treat stricter compiler options as a controlled migration rather than silently weakening types or changing the whole project at once.
- Do not disable diagnostics globally merely to make type checking pass.
- Before upgrading TypeScript, identify the installed version and review relevant official release notes and breaking changes.
- Do not assume internal APIs or the Compiler API remain compatible across major versions.

## Implementation quality

- Prefer minimal, localized changes that follow the repository's existing architecture.
- Preserve public APIs unless the task explicitly authorizes a breaking change.
- Avoid abstractions without a real current consumer.
- Do not add dependencies when the existing platform can solve the problem clearly.
- Follow repository conventions for naming, exports, errors, asynchronous behavior, resource cleanup, and test placement.
- Distinguish domain, programming, and infrastructure failures when the project already makes that distinction.
- Keep comments focused on intent, invariants, and non-obvious constraints.

## Validation

- Discover validation scripts and tools from the project instead of assuming them.
- Run the smallest relevant typecheck, lint, build, and test scope first.
- Prefer tests focused on the files and behavior changed during the fast development loop.
- Do not assume a particular package manager or test runner.
- Do not automatically run the complete project suite inside a scoped specification flow.
- Report validations that were not executed.
- Do not treat exit code zero as sufficient when output contains failures, unconstructed suites, cancelled tests, or worker errors.

## Official documentation

- Prefer DocsSearch and WebFetch using the official URLs declared in this package.
- Use WebSearch when locating version-specific official documentation, release notes, migration guidance, or recently changed behavior.
- For version-sensitive TypeScript guidance, restrict authoritative sources to `typescriptlang.org` and `devblogs.microsoft.com/typescript`.
- Confirm the project's installed TypeScript version before applying release-note guidance.
- Prefer official documentation over generic blog posts when both are available.
