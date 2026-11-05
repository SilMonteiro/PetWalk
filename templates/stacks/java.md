---
schemaVersion: 2
name: "Java"
skillName: "java"
tier: 1
category: "language"
detection:
  extensions: [".java"]
docUrls:
  - "https://dev.java/learn/"
  - "https://docs.oracle.com/en/java/javase/"
  - "https://docs.oracle.com/javase/specs/"
keywords: ["java", "jvm", "jdk", "javac", "maven", "gradle", "junit", "concurrency"]
validation:
  provider: "java"
  capability: "full-test"
---

# Java Stack

This project uses **Java**. Apply the guidance below only after inspecting the repository's actual JDK, build system, modules, frameworks, and validation conventions.

## Project evidence

Before proposing changes, inspect the repository's existing evidence:

- Identify the actual `.java` source files, module structure, and subprojects.
- Check for `pom.xml`, `build.gradle`, `build.gradle.kts`, `settings.gradle`, `settings.gradle.kts`, `gradlew`, `gradlew.bat`, `mvnw`, `mvnw.cmd`, and `module-info.java`.
- Inspect manifests, CI configuration, container definitions, and toolchain configuration.
- Determine the installed JDK version, source level, target level, release level, and runtime environment.
- Identify the actual framework, annotation processors, test runners, and deployment packaging.
- Do not assume Maven, Gradle, JUnit, Spring, or a Maven default layout.
- Preserve the build tool, conventions, and module structure already present in the repository.
- Do not create a new build file when the project already has one.

## Java and JDK compatibility

- Discover the minimum and maximum supported Java version from build files, CI, and deployment artifacts.
- Verify the compilation JDK, runtime JDK, and CI JDK separately.
- Respect `source`, `target`, `release`, `--release`, and toolchain declarations.
- Do not use APIs, language features, or bytecode versions introduced after the project's minimum supported version.
- Treat preview features as opt-in and version-gated.
- Check bytecode compatibility when changing source level or target version.
- Consult the official documentation for the specific Java version in use before applying version-sensitive guidance.
- Do not upgrade the JDK version implicitly.

## Build and dependencies

- Use the existing wrapper (`gradlew`, `mvnw`) when one is present in the repository.
- Do not replace the build system with a different one.
- Respect multi-module project structure, parent POMs, and composite builds.
- Preserve existing dependency scopes, configurations, and version catalogs.
- Analyze annotation processors, code generation plugins, generated sources, and BOMs when they are present.
- Avoid adding new dependencies without a demonstrated need.
- Do not edit lockfiles, version catalogs, or resolution files unrelated to the task.
- Verify the impact of changes on downstream modules and published APIs.

## APIs and language design

- Preserve public contracts, exported packages, and observable behavior unless a breaking change was explicitly requested.
- Respect existing nullability annotations and conventions.
- Use generics safely without introducing raw types or unnecessary casts.
- Preserve `equals`, `hashCode`, and `toString` contracts when modifying data classes.
- Handle exceptions at the appropriate abstraction level. Do not swallow exceptions silently.
- Preserve serialization compatibility when the type is serializable.
- Use records, sealed classes, pattern matching, and other modern features only when the project's Java version supports them.
- Avoid abstractions without a real consumer in the current codebase.

## Concurrency and resources

- Respect thread interruption and do not swallow `InterruptedException` without restoring the interrupt status.
- Avoid blocking operations on shared threads without clear justification.
- Prevent race conditions by using appropriate concurrent collections or synchronization.
- Manage executors, futures, and completable futures correctly with cleanup when they are no longer needed.
- Use `try-with-resources` for resources whose lifecycle the changed code owns; do not close borrowed, shared, or framework-managed resources.
- Close owned streams, files, sockets, and connections deterministically; shut down owned executors according to the supported Java version and repository conventions.
- Use virtual threads only when the Java version and deployment architecture support them.
- Avoid unbounded buffers, queues, caches, and thread pools.
- Make concurrent tests deterministic and time-bounded.

## Security and boundaries

- Validate paths, URLs, external input, and serialized data at trust boundaries.
- Prevent path traversal in file operations.
- Avoid exposing secrets, credentials, or personally identifiable information in logs, errors, or generated files.
- Avoid unsafe deserialization of untrusted data.
- Configure XML parsers to disable external entity processing when parsing untrusted input.
- Avoid command injection by preferring structured APIs over shell concatenation.
- Respect the project's existing permission and sandbox mechanisms. Treat Security Manager usage as version-sensitive legacy compatibility: it is deprecated since Java 17 and permanently disabled starting in Java 24; do not introduce new reliance on it.
- Do not introduce process execution when a pure in-process operation is sufficient.

## Implementation quality

- Prefer small, localized changes that follow the repository's existing architecture.
- Follow the project's naming conventions, package structure, and code style.
- Preserve public APIs and binary compatibility when relevant.
- Provide useful diagnostics without exposing sensitive information.
- Log at appropriate levels without leaking credentials or personal data.
- Keep comments focused on invariants, non-obvious constraints, and design intent.
- Do not create new dependencies, abstraction layers, or modules without a demonstrated need.
- Do not reformat unrelated files.

## Validation

- Discover the build tool and validation commands from the repository itself.
- Use the existing wrapper (`gradlew`, `mvnw`) when one is present.
- Execute the smallest relevant validation scope first: single module, package, class, or spec.
- Do not automatically run the complete project suite inside a scoped specification flow (`/spec-pipe`).
- Leave full-suite execution to `/spec-test`.
- Do not treat exit code zero as sufficient when the output contains load failures, cancelled tests, suites that were not constructed, or worker errors.
- Report validations that were not executed.
- Do not assume Maven, Gradle, Ant, Bazel, or JUnit. Discover the actual tooling from the repository.

## Official documentation

- Prefer DocsSearch and WebFetch using the official URLs declared in this package.
- Use WebSearch to locate version-specific official documentation, release notes, migration guidance, or recently changed behavior.
- Prefer `dev.java`, `docs.oracle.com`, and official OpenJDK sources for Java platform guidance.
- Consult Maven documentation only when Maven is detected in the repository.
- Consult Gradle documentation only when Gradle is detected in the repository.
- Do not substitute official documentation with generic blog posts when both are available.
- Confirm the project's Java version before applying version-dependent recommendations.
