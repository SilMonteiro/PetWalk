---
schemaVersion: 2
name: "Node.js"
skillName: "nodejs"
tier: 1
category: "runtime"
detection:
  files: ["package.json"]
  extensions: [".js", ".mjs", ".cjs", ".mts", ".cts"]
docUrls:
  - "https://nodejs.org/docs/latest/api/"
  - "https://nodejs.org/en/learn"
  - "https://nodejs.org/en/about/previous-releases"
keywords: ["nodejs", "node", "javascript", "runtime", "esm", "commonjs", "async", "streams"]
validation:
  provider: "typescript-node"
  capability: "full-test"
---

# Node.js Runtime

This project has evidence of a **Node.js runtime**. Apply the guidance below only after confirming whether Node.js is the production runtime, a development toolchain, or both.

## Project evidence

Before proposing changes:

- Inspect `package.json`, the lockfile, existing scripts, runtime version declarations, entry points, and package boundaries.
- Check `engines`, `type`, `main`, `module`, `exports`, `imports`, `bin`, and workspace configuration when present.
- Look for version files and repository-specific runtime setup without assuming a particular version manager.
- Determine whether Node.js is the execution target or only supports building, testing, or tooling for another runtime.
- Identify the package manager from repository evidence. Do not assume npm, pnpm, yarn, or Bun.
- Identify the actual framework, loader, transpiler, bundler, and test runner.
- Preserve the project's existing conventions unless the task explicitly requires migration.

## Runtime compatibility

- Confirm the supported Node.js version before using recently introduced APIs.
- Respect the version range declared by the project and its deployment environment.
- Consult official release and API documentation for version-sensitive behavior.
- Do not assume the latest release is available in production.
- Avoid relying on experimental APIs unless the project already accepts that risk and the task requires them.
- Consider operating-system differences in paths, signals, process trees, shells, permissions, and filesystem behavior.
- Preserve compatibility with the project's supported platforms.

## Modules and packages

- Determine ESM or CommonJS semantics from `package.json`, file extensions, tooling, and runtime version.
- Respect `.mjs`, `.cjs`, `.mts`, and `.cts` semantics.
- Do not mix `require` and `import` without a demonstrated interoperability requirement.
- Preserve package `exports` and `imports` contracts.
- Avoid deep imports that bypass an exported public API.
- Treat changes to entry points, conditional exports, or package type as compatibility-sensitive.
- Do not rewrite module configuration merely to satisfy a local tool without checking runtime and consumer impact.

## Async and resources

- Await or deliberately manage every promise that can affect observable behavior.
- Handle stream completion, backpressure, errors, cancellation, and partial output when relevant.
- Clean up timers, intervals, listeners, file descriptors, subprocesses, sockets, and temporary resources.
- Avoid unbounded buffering of process, stream, log, or test output.
- Define timeouts for operations that can wait indefinitely when the surrounding architecture supports cancellation or deadlines.
- Prevent multiple event handlers from settling the same lifecycle more than once.
- Preserve useful diagnostics when handling failures.

## Security and boundaries

- Treat paths, environment variables, command arguments, URLs, serialized data, and external input as untrusted boundaries.
- Validate paths before filesystem or process operations.
- Prefer argument arrays and direct process invocation over shell interpolation when process execution is required.
- Do not expose secrets through logs, errors, command output, or generated files.
- Avoid prototype pollution, unsafe object merging, and implicit trust in parsed JSON.
- Respect repository permission and sandbox mechanisms.
- Do not add process execution when a pure in-process operation is sufficient.

## Implementation quality

- Prefer small changes that follow the repository's established architecture.
- Preserve public APIs and observable behavior unless a breaking change was explicitly requested.
- Avoid new dependencies without a demonstrated need.
- Use built-in Node.js APIs when they provide a clear and maintainable solution.
- Distinguish programming, domain, process, network, and infrastructure failures when the project already does so.
- Avoid blocking synchronous work in latency-sensitive paths unless repository evidence justifies it.
- Keep comments focused on invariants and non-obvious runtime constraints.

## Validation

- Discover build, lint, typecheck, and test commands from the repository.
- Start with the smallest relevant validation scope.
- Do not assume a particular test runner or package manager.
- Do not automatically execute the complete project suite inside a scoped specification flow.
- Report validations that were not executed.
- Verify process exit status and inspect output for failures, worker errors, cancelled tests, or suites that were not constructed.
- Ensure tests involving processes, streams, or timers have deterministic cleanup and bounded duration.

## Official documentation

- Prefer DocsSearch and WebFetch using the official URLs declared in this package.
- Use WebSearch to locate version-specific official API documentation, migration guidance, release status, or recently changed behavior.
- Restrict authoritative Node.js runtime sources to `nodejs.org` when official documentation is available.
- Confirm the project's supported Node.js version before applying version-specific guidance.
- Do not treat generic framework advice as a substitute for Node.js runtime documentation.
