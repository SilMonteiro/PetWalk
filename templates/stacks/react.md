---
schemaVersion: 2
name: "React"
skillName: "react"
tier: 1
category: "framework"
detection:
  extensions: [".tsx", ".jsx"]
  excludeIf:
    files: ["angular.json"]
docUrls:
  - "https://react.dev/"
keywords: ["react", "jsx", "tsx", "hooks"]
validation:
  provider: "typescript-node"
  capability: "full-test"
---

# React Stack

This project uses **React**. When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://react.dev/` — the official documentation. Prefer WebFetch on react.dev URLs over generic WebSearch.
2. **Extensions**: `.tsx`, `.jsx` files (React components with JSX/TSX).
3. **Language**: TypeScript preferred (`.tsx`). If `.jsx`, plain JavaScript.
4. **Hooks**: useState, useEffect, useContext, useRef — functional components over class components.
5. **Test commands**: run `npm run test` for the project suite, `vitest run` for a single Vitest pass, or `npx jest` for Jest projects.
6. **Test convention**: colocate `*.test.tsx` / `*.spec.tsx` next to the component (forms `.jsx` follow the same rule: `*.test.jsx` / `*.spec.jsx`); shared cases may live under `__tests__/`.
7. **Testing Library**: use `@testing-library/react` with Testing Library queries (`screen`, `user-event`) instead of testing implementation details.
8. **`.tsx` vs `.jsx`**: prefer `.tsx` for typed components; `.jsx` is plain JavaScript and needs propTypes/JSDoc for contracts.
9. **multiple copies trap**: two copies of React break hooks (`Invalid hook call`); diagnose with `npm ls react` and fix with `npm dedupe`.
10. **Fast refresh**: instant update in dev runs via the `react-refresh` runtime; a full reload means the refresh boundary was broken, not a test failure.
