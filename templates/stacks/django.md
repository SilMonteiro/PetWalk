---
schemaVersion: 2
name: "Django"
skillName: "django"
tier: 1
category: "framework"
detection:
  extensions: [".py"]
  files: ["manage.py"]
docUrls:
  - "https://docs.djangoproject.com/en/stable/"
keywords: ["django", "python", "orm", "migrations"]
validation:
  provider: "python"
  capability: "full-test"
---

# Django Stack

This project uses **Django** (Python web framework). When answering questions about this codebase:

1. **Primary source**: Use WebFetch on `https://docs.djangoproject.com/en/stable/` — the official documentation.
2. **Detection**: `manage.py` file at project root + `.py` source files.
3. **Structure**: Models (ORM), Views, Templates (MVT pattern), URL routing.
4. **Database**: Django ORM with migrations (`python manage.py makemigrations`/`migrate`).

Prefer WebFetch on docs.djangoproject.com URLs over generic WebSearch.
