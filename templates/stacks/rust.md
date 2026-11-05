---
schemaVersion: 2
name: "Rust"
skillName: "rust"
tier: 1
category: "language"
detection:
  extensions: [".rs"]
docUrls:
  - "https://doc.rust-lang.org/book/"
  - "https://doc.rust-lang.org/reference/"
  - "https://doc.rust-lang.org/cargo/"
  - "https://doc.rust-lang.org/std/"
keywords: ["rust", "cargo", "crate", "ownership", "borrowing", "lifetime", "trait", "async"]
validation:
  provider: "rust"
  capability: "full-test"
---

# Rust Stack

This project uses **Rust**. Apply the guidance below only after inspecting the repository's edition, minimum supported Rust version, toolchain channel, workspace, targets, feature sets, unsafe boundaries, and validation conventions.

## Project evidence

Before proposing changes, inspect the repository's existing evidence:

- Identify the actual `.rs` source files, crate structure, and module hierarchy.
- Check for `Cargo.toml`, `Cargo.lock`, workspace members, default-members, and exclude declarations.
- Inspect `rust-toolchain`, `rust-toolchain.toml`, edition, `rust-version`, compilation targets, and features.
- Locate `build.rs`, procedural macros, generated code, `no_std` declarations, and `unsafe` usage.
- Review FFI declarations, target triples, CI configuration, containers, Cargo configurations, aliases, and scripts.
- Identify the actual async runtime, test runner, and tooling conventions.
- Do not assume Tokio, async, `std`, nightly, Clippy, rustfmt, or cargo-nextest.
- Do not edit generated code without locating its generator.
- Preserve the repository's workspace organization, crate boundaries, and existing conventions.

## Language, edition, and MSRV

- Discover the edition and minimum supported Rust version from `Cargo.toml`, CI, and deployment artifacts.
- Distinguish the MSRV from the pinned toolchain, CI toolchain, and production runtime.
- Respect `rust-version` and `rust-toolchain.toml`.
- Distinguish stable, beta, and nightly channels.
- Do not introduce unstable features or nightly-only APIs without explicit justification.
- Do not upgrade the edition or MSRV implicitly.
- Do not use APIs stabilized after the MSRV.
- Consider target triples, standard library availability, and conditional compilation.
- Check feature gates and `#[cfg]` attributes.
- Do not assume the latest toolchain is installed or available.
- Confirm the crate's edition, rust-version, toolchain channel, targets, and enabled features before applying version-sensitive guidance.

## Crates, workspaces, and features

- Respect existing crate and module boundaries. Preserve public paths.
- Check workspace members, exclude, and default-members. Verify the resolver and feature unification strategy.
- Do not assume `--all-features` represents a valid configuration.
- Do not assume features are mutually compatible. Preserve default features.
- Check optional dependencies and conditional compilation.
- Do not alter `Cargo.toml` or `Cargo.lock` without a direct relationship to the task.
- Do not auto-update dependencies.
- Preserve patches, registries, git dependencies, and path dependencies.
- Consider the project's policy on Cargo.lock versioning.
- Avoid new dependencies without a demonstrated need.
- Avoid dependency cycles and inverted architectural dependency direction.
- Remember that build scripts and procedural macros execute code.
- Cargo commands may download dependencies and execute build scripts or procedural macros; treat them as code-execution boundaries.

## Ownership, borrowing, and API design

- Understand the ownership model before modifying lifetimes or borrow patterns.
- Determine whether the existing API takes owned values, shared borrows, or mutable borrows.
- Avoid unnecessary clones and allocations.
- Do not use `Box::leak` to circumvent lifetime constraints.
- Do not use `Rc<RefCell<_>>`, `Arc<Mutex<_>>`, or interior mutability merely to satisfy the compiler.
- Limit borrow scope when that correctly resolves the contract.
- Do not add unnecessary lifetime annotations.
- Use types that express invariants.
- Preserve public APIs and consider SemVer compatibility.
- Do not create traits without a real consumer. Prefer small, consumer-driven traits.
- Consider object safety when trait objects are used.
- Avoid unnecessarily exposing concrete types.
- Consider auto traits such as `Send`, `Sync`, `Unpin`, and unwind safety where they matter.
- Do not add clones, allocations, interior mutability, or unsafe code merely to silence the borrow checker; first determine the intended ownership contract.

## Errors and failure semantics

- Use `Result` for recoverable failures and `Option` for legitimate absence.
- Avoid `unwrap` and `expect` in production paths unless the invariant is truly proved.
- Do not use `panic` for ordinary operational errors.
- Preserve error causes and context. Do not prematurely convert all errors to strings.
- Preserve `source` chains and error compatibility.
- Consider compatibility of public error enums.
- Use `#[non_exhaustive]` only when appropriate for the contract.
- Distinguish programming, domain, I/O, network, and infrastructure errors.
- Do not silently discard errors from cleanup.
- Preserve the first relevant error without hiding important subsequent failures.
- Consider panic and unwind behavior at interfaces and across FFI boundaries.
- Do not expose sensitive data in error messages or logs.

## Unsafe code and FFI

- Minimize the `unsafe` surface. Do not introduce `unsafe` to avoid a legitimate redesign.
- Compiler acceptance does not prove soundness.
- A successful compile does not prove unsafe code sound; every unsafe block and unsafe impl must have an explicit, reviewable safety invariant.
- Each `unsafe impl` must justify the safety contracts it assumes.
- Review aliasing, validity, alignment, initialization, provenance, and lifetimes.
- Avoid `transmute` without demonstrated necessity.
- Validate raw pointers, sizes, and ownership at trust boundaries.
- Respect layout and ABI constraints.
- Define clearly who allocates and who frees memory in FFI.
- Avoid unwinding across FFI boundaries without a compatible contract.
- Handle strings, buffers, and null termination correctly.
- Verify thread safety of external libraries.
- Treat build scripts and native dependencies as risk boundaries.

## Concurrency, async, and cancellation

- Discover the actual async runtime from repository evidence. Do not introduce a new one.
- Do not mix executors without a demonstrated need.
- Avoid blocking work inside async executors.
- Do not hold a synchronous lock guard across an await point unless the chosen primitive and runtime explicitly make that safe and intentional.
- Handle poisoning according to the project's policy.
- Limit tasks, threads, channels, and queue depths. Implement backpressure.
- Avoid deadlocks. Preserve lock ordering.
- Handle `JoinHandle` and task failures. Do not silently discard tasks.
- Consider `Send` and `'static` bounds only when they are genuinely required.
- Avoid `Arc<Mutex<_>>` as an automatic solution.
- Consider memory ordering when using atomics.
- Treat cancellation as partial drop of futures.
- Cancellation can drop futures at suspension points; preserve invariants and resource cleanup under partial progress.
- Make cleanup deterministic.
- Apply timeouts when the architecture supports cancellation or deadlines.
- Make concurrent tests bounded and deterministic.

## Resources and security

- Use RAII and `Drop` consistently with Rust's ownership model.
- Do not assume the exact timing of `Drop` when the contract depends on it.
- Do not close or consume borrowed resources.
- Close resources owned by the code.
- Avoid reference cycles.
- Limit buffers, channels, caches, collections, and task counts.
- Validate paths, URLs, command arguments, and external data at trust boundaries.
- Prevent path traversal in file operations.
- Avoid command injection. Prefer structured arguments over shell interpolation.
- Protect secrets. Do not expose them in logs, errors, or generated artifacts.
- Use appropriate permissions for temporary and sensitive files.
- Validate untrusted serialized formats.
- Do not apply `unsafe` to external data without validation.
- Consider integer overflow behavior according to profiles and operations.
- Do not introduce subprocess execution when an in-process solution is sufficient.

## Implementation quality

- Prefer small, localized changes that follow the repository's existing architecture.
- Follow `rustfmt` conventions only on the files that were changed.
- Preserve the project's established conventions.
- Avoid premature abstractions, traits without consumers, and unnecessary generics.
- Do not reformat unrelated files.
- Preserve public APIs unless the task explicitly authorizes a breaking change.
- Comments should explain invariants, ownership, and safety contracts.
- Provide public documentation according to the project's convention.
- Avoid global lint suppressions. Do not alter generated code or build artifacts in `target`.
- Avoid new dependencies without a demonstrated need.

## Validation

- Discover validation commands from the repository itself.
- Start with the smallest relevant crate, package, target, or test.
- Respect existing aliases, build scripts, and CI definitions.
- Consider features, targets, profiles, and build flags.
- Do not assume `cargo test --workspace` or `--all-features`.
- Do not assume Clippy, rustfmt, or nextest.
- Do not automatically run the complete suite inside a scoped specification flow (`/spec-pipe`).
- Leave full-suite execution to `/spec-test`.
- Do not update dependencies just to run tests.
- Remember that Cargo may download and execute code.
- Do not treat exit code zero as sufficient when the output contains crates that did not compile, cancelled tests, build script failures, or worker errors.
- Report validations that were not executed.
- Consider platform and toolchain constraints.

## Official documentation

- Prefer DocsSearch and WebFetch using the official URLs declared in this package.
- Use WebSearch to locate version-specific official documentation, release announcements, or migration guidance.
- Prioritize `doc.rust-lang.org` for Rust platform guidance.
- Use the Rust Reference for language semantics.
- Use the Cargo Book for manifests, workspaces, and features.
- Use the standard library documentation for API contracts.
- Consult the Rust Book for ownership and concurrency fundamentals.
- Confirm the project's edition and MSRV before applying version-sensitive recommendations.
- Do not substitute official documentation with generic blog posts when both are available.
