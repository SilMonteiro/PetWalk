---
schemaVersion: 2
name: "Visual Basic 6"
skillName: "vb6"
tier: 1
category: "language"
detection:
  extensions: [".vbp"]
docUrls:
  - "https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-basic-6/"
keywords: ["vb6", "visual basic", "vbp", "frm", "bas"]
---

# Visual Basic 6 Stack

This project uses **Visual Basic 6**. When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://learn.microsoft.com/en-us/previous-versions/visualstudio/visual-basic-6/` — the official documentation.
2. **Extensions**: `.vbp` (project), `.frm` (form), `.bas` (module), `.cls` (class).
3. **VB6 vs VB.NET**: These are different languages. This project uses VB6 (COM-based), not VB.NET (CLR-based).
4. **Common patterns**: COM components, Windows API calls, ADO for data access.

Prefer WebFetch on learn.microsoft.com URLs over generic WebSearch.
