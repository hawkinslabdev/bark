---
title: Footer
description: The content footer, configured via footer in config.json
---

# Footer

`footer` in `docs/config.json` renders at the bottom of every page, below the pagination links.

```json
{
  "footer": "Built with Bark · [EUPL-1.2](https://github.com/hawkinslabdev/bark/blob/main/LICENSE)"
}
```

`footer` is a Markdown string. Inline Markdown (links, emphasis, inline code) is supported:

```json
{
  "footer": "Copyright 2026. Questions? [Open an issue](https://github.com/hawkinslabdev/bark/issues)."
}
```

Without `footer` or `bottomNav`, no footer is rendered. There is no default text.

## Footer links

`bottomNav` adds a row of links. Items use the `topNav` shape (`text`, `link`); dropdown `items` are ignored.

```json
{
  "footer": "© {year} {brand}",
  "bottomNav": [
    { "text": "Privacy", "link": "/privacy" },
    { "text": "GitHub", "link": "https://github.com/hawkinslabdev/bark" }
  ]
}
```

| `bottomNav` | Layout |
|---|---|
| omitted | `footer` text centred. |
| `[]` | `footer` text left-aligned, no links. |
| one or more items | `footer` text left-aligned, links right-aligned. Wraps below the text on narrow screens. |

Internal links receive the base path and the current locale prefix. External links open in a new tab and carry an external-link icon. Link text passes through the locale string table, like `topNav`. The link list is a `<nav>` labelled by the `footerNavAria` locale string.

## Variables

Variables are substituted before Markdown rendering:

| Variable | Replaced with |
|----------|---------------|
| `{year}` | The current year (UTC) |
| `{brand}` | The brand text (`brand`, falling back to `title`) |
| `{title}` | The site `title` from `config.json` |

Example copyright line:

```json
{
  "footer": "© {year} {brand} · [EUPL-1.2](https://github.com/hawkinslabdev/bark/blob/main/LICENSE)"
}
```

::: note Rendering the footer
The footer is rendered per page through the same Markdown pipeline as page content. Inline elements (links, code spans, emphasis) are supported; headings and fenced code blocks are not.
:::

Home pages (`layout: home`) render the footer below the features grid. Hide it with [custom CSS](/guide/themes).