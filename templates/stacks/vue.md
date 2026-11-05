---
schemaVersion: 2
name: "Vue.js"
skillName: "vue"
tier: 1
category: "framework"
detection:
  extensions: [".vue"]
docUrls:
  - "https://vuejs.org/guide/"
keywords: ["vue", "vuejs", "vite", "pinia"]
validation:
  provider: "typescript-node"
  capability: "full-test"
---

# Vue.js Stack

This project uses **Vue.js**. When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://vuejs.org/guide/` — the official guide. Prefer WebFetch on vuejs.org/guide URLs over generic WebSearch.
2. **Extensions**: `.vue` (Single File Components).
3. **Style**: Composition API preferred over Options API — `<script setup>` vs Options API (check `<script setup>` usage).
4. **Ecosystem**: Vite as build tool, Pinia for state management, Vue Router for routing.
5. **Test commands**: run `npm run test` for the project suite, `vitest run` for a single Vitest pass, or `npx jest` for Jest projects.
6. **Test convention**: colocate `*.test.ts` / `*.spec.ts` next to the component (forms `.js` follow the same rule: `*.test.js` / `*.spec.js`); shared cases may live under `__tests__/`.
7. **Vue Test Utils**: use `@vue/test-utils` with `mount` to mount the component under test.
8. **Testing Library/Vue**: use `@testing-library/vue` with Testing Library queries (`screen`, `user-event`) instead of testing implementation details.
9. **SFC rule**: every `.vue` file is mounted via wrapper — never import raw: never import a raw `.vue` SFC directly in a test, always mount it through `mount` from `@vue/test-utils` or `render` from `@testing-library/vue`.
10. **Vite/Vitest config**: `vitest.config.ts` with environment `jsdom` / `happy-dom`.
11. **Mocks**: Pinia via `createTestingPinia`, Vue Router via `vue-router-mock`.
12. **Validation sharing**: this stack shares the `typescript-node` provider intentionally (no dedicated provider); selection resolves via `package.json` + test script.
