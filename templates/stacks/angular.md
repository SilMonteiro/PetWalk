---
schemaVersion: 2
name: "Angular"
skillName: "angular"
tier: 1
category: "framework"
detection:
  files: ["angular.json"]
docUrls:
  - "https://angular.io/docs"
keywords: ["angular", "ng", "typescript"]
validation:
  provider: "typescript-node"
  capability: "full-test"
---

# Angular Stack

This project uses **Angular**. When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://angular.io/docs` — the official documentation.
2. **Configuration**: `angular.json` at project root.
3. **Language**: TypeScript with decorators and dependency injection.
4. **CLI**: Angular CLI (`ng generate`, `ng serve`, `ng build`).

Prefer WebFetch on angular.io URLs over generic WebSearch.
