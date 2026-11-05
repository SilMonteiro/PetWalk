---
schemaVersion: 2
name: "Python"
skillName: "python"
tier: 1
category: "language"
detection:
  extensions: [".py"]
  excludeIf:
    files: ["manage.py"]
docUrls:
  - "https://docs.python.org/3/"
keywords: ["python", "py", "pip", "venv"]
validation:
  provider: "python"
  capability: "full-test"
---

# Python Stack

This project uses **Python**. When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://docs.python.org/3/` — the official documentation.
2. **Extensions**: `.py` files.
3. **Package management**: pip (`pip install`), virtualenv/venv.
4. **Frameworks**: Check for Django (`manage.py`), Flask (`app.py`), FastAPI, or plain scripts.

Prefer WebFetch on docs.python.org URLs over generic WebSearch.
