---
schemaVersion: 2
name: "Go"
skillName: "go"
tier: 1
category: "language"
detection:
  extensions: [".go"]
docUrls:
  - "https://go.dev/doc/"
  - "https://go.dev/ref/spec"
  - "https://go.dev/doc/modules/"
  - "https://pkg.go.dev/std"
keywords: ["go", "golang", "goroutine", "channel", "module", "gofmt", "testing", "concurrency"]
validation:
  provider: "go"
  capability: "full-test"
---

# Go Stack

This project uses **Go**. Apply the guidance below only after inspecting the repository's actual Go version, module boundaries, build constraints, platform targets, and validation conventions.

## Project evidence

Before proposing changes, inspect the repository's existing evidence:

- Identify the actual `.go` source files, package structure, test files, and module boundaries.
- Check for `go.mod`, `go.sum`, `go.work`, and `internal` directories.
- Inspect the `go` directive, `toolchain` directive, build tags, and platform-specific files.
- Locate `_test.go` files, generated code, CGO usage, and the `vendor` directory when present.
- Review CI configuration, containers, Makefiles, Taskfiles, and existing scripts.
- Identify the actual test runner, linters, and tooling conventions.
- Do not assume every project has a `go.mod`, a framework, Make, golangci-lint, or CGO.
- Do not modify generated code without identifying the generator.
- Preserve the repository's existing package borders, module conventions, and build constraints.

## Language and toolchain compatibility

- Discover the Go version supported by the repository from `go.mod`, CI, containers, and deployment artifacts.
- Respect the `go` and `toolchain` directives.
- Distinguish the language version from the local toolchain, CI toolchain, and production runtime.
- Do not upgrade the Go version implicitly.
- Do not use APIs, generics, or language features introduced after the project's minimum supported Go version.
- Verify build tags and platform-specific file constraints before proposing platform-dependent code.
- Check loop-variable capture semantics, which changed in Go 1.22.
- Confirm the module's Go version before relying on version-sensitive loop-variable, generic, standard-library, or toolchain behavior.
- Do not assume the latest Go release is installed or available.

## Packages and modules

- Respect existing package borders, module boundaries, and `internal` access restrictions.
- Preserve public import paths unless a breaking change is explicitly authorized.
- Check `replace`, `exclude`, `retract`, and workspace declarations when they are present.
- Do not run `go mod tidy` automatically as part of a code change.
- Do not alter `go.mod`, `go.sum`, or `go.work` without a direct relationship to the task.
- Avoid new dependencies without a demonstrated need.
- Avoid import cycles and preserve the existing architectural direction of dependencies.
- Do not create interfaces as a precaution. Prefer small, consumer-driven interfaces when a real consumer exists.
- Remember that Go commands may download dependencies or modify metadata as part of their normal operation.

## APIs, data, and errors

- Preserve exported APIs and observable behavior unless a breaking change was explicitly requested.
- Respect zero values and the distinction between nil slices and empty slices when it affects a contract.
- Avoid writing to nil maps.
- Be aware that an interface value holding a nil pointer is non-nil.
- Choose value or pointer receivers consistently and according to the type's intended use.
- Do not copy mutexes or synchronized values after first use.
- Do not use `panic` for ordinary operational errors.
- Add context to errors without destroying the original cause.
- Use `errors.Is` and `errors.As` when appropriate.
- Use `%w` wrapping only when the cause is part of the exported contract.
- Preserve serialization compatibility when the type is serialized.
- Avoid generics or reflection without a concrete, demonstrable need.

## Concurrency and cancellation

Goroutines share the same address space; channels coordinate communication but do not automatically protect unrelated shared state.

Make goroutine ownership, cancellation, completion, and error propagation explicit.

Only the component that owns the sending lifecycle should close a channel, and only when closure communicates a meaningful state transition.

Remember that deferred cleanup runs when the surrounding function returns, not at the end of a loop iteration.

- Avoid goroutine leaks. Every long-lived goroutine must have a clear termination condition.
- Propagate `context.Context` according to the project's conventions.
- Do not store contexts in structs without an architectural justification.
- Respect cancellation and deadlines. Check `ctx.Err()` and `ctx.Done()` at appropriate boundaries.
- Do not send on a closed channel.
- Do not close a channel that was received or borrowed.
- Do not treat a channel as automatic protection for unrelated variables.
- Protect shared memory with clear ownership, mutexes, atomics, or confinement.
- Use `sync.WaitGroup` and other primitives according to the supported Go version.
- Limit concurrency, queue depths, and resource usage.
- Treat the case where a worker never responds.
- Avoid permanent blocking on sends and receives.
- Make goroutine settlement and resource cleanup deterministic.
- Preserve the first relevant error. Do not silently discard concurrent errors.
- Check for data races when the toolchain and platform support it.

## Resources and security

- Close resources whose lifecycle the code owns; do not close borrowed or shared resources.
- Close owned response bodies, files, rows, sockets, and connections.
- Stop timers and tickers when they are no longer needed.
- Use `defer` considering the actual scope of the enclosing function.
- Avoid accumulating deferred calls inside long-running loops.
- Apply timeouts to HTTP requests and external operations when the architecture supports cancellation or deadlines.
- Avoid unbounded buffers, queues, caches, and collections.
- Validate paths, URLs, command arguments, and external input at trust boundaries.
- Prevent path traversal in file operations.
- Avoid command injection. Prefer `os/exec` with separate arguments over shell interpolation.
- Do not expose secrets, credentials, or sensitive data in logs or errors.
- Use appropriate permissions for temporary and sensitive files.
- Do not introduce subprocess execution when a pure in-process operation is sufficient.
- Treat `unsafe` code and CGO boundaries as risk surfaces.

## Implementation quality

- Prefer small, localized changes that follow the repository's existing architecture.
- Follow `gofmt` and the project's established conventions.
- Do not reformat unrelated files.
- Preserve public APIs unless the task explicitly authorizes a breaking change.
- Avoid premature abstractions and unnecessary dependencies.
- Comments should explain invariants and ownership, not restate the code.
- Names should follow Go conventions.
- Do not introduce getters, setters, or patterns from other languages without a demonstrated need.
- Keep diagnostics useful without exposing sensitive data.
- Respect generated code: do not edit build artifacts.

## Validation

- Discover validation commands from the repository itself.
- Execute the smallest relevant package or test first.
- Respect existing scripts, Makefiles, and CI definitions.
- Do not assume `go test ./...` as the first validation step.
- Do not automatically run the complete suite inside a scoped specification flow (`/spec-pipe`).
- Leave full-suite execution to `/spec-test`.
- Use the race detector only when the platform, CGO availability, time budget, and scope make it appropriate.
- Consider build tags, environment variables, and platform constraints.
- Do not treat exit code zero as sufficient when the output contains load failures, packages that did not compile, cancelled tests, or worker errors.
- Report validations that were not executed.
- Do not modify modules solely to run tests.

## Official documentation

- Prefer DocsSearch and WebFetch using the official URLs declared in this package.
- Use WebSearch to locate version-specific official documentation, release notes, migration guidance, or recently changed behavior.
- Prioritize `go.dev` and `pkg.go.dev` for Go platform guidance.
- Consult the specification for language semantics.
- Consult module documentation before modifying `go.mod`, `go.sum`, or `go.work`.
- Confirm the project's Go version before applying version-sensitive recommendations.
- Do not substitute official documentation with generic blog posts when both are available.
