---
title: Edit Link
description: Add an "Edit this page" link via editLink in config.json
---

# Edit Link

`editLink` in `docs/config.json` adds an "Edit this page" link below the content of every doc-layout page.

```json
{
  "editLink": {
    "pattern": "https://github.com/hawkinslabdev/bark/edit/main/docs/:path",
    "text": "Edit this page on GitHub"
  }
}
```

| Field | Type | Default | Description |
|---|---|---|---|
| `pattern` | `string` | none, required | A URL template. The literal text `:path` gets replaced with the current page's path. |
| `text` | `string` | `"Edit this page"` | Link label. |

`:path` resolves to the lowercased URL path plus `.md`: `guide/configuration` becomes `guide/configuration.md`. Bark lowercases every served file path.

:::danger Important
Filenames with capital letters (`Configuration.md`) produce broken edit links. Use lowercase filenames (`configuration.md`) in `docs/`.
:::

Without `editLink`, the link is not rendered.