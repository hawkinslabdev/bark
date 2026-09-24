---
title: Routing
description: How a file path becomes a URL in Bark
---

# Routing

Routing is file-based: the URL derives from the path in `docs/`. A catch-all handler maps the request path to the page cache; there is no route table.

## From file path to URL

Transformation steps:

1. **Extension removed.** `.md` is not part of the URL.
2. **`index.md` collapsed.** It serves as the folder page, without an `/index` suffix.
3. **Lowercased and trimmed.** Slashes are trimmed and case is normalized; `/Guide/` and `/guide/` resolve to the same page.

| File path | Resulting URL |
|---|---|
| `docs/index.md` | `/` |
| `docs/guide/index.md` | `/guide` |
| `docs/guide/getting-started.md` | `/guide/getting-started` |
| `docs/Reference/API-Reference.md` | `/reference/api-reference` |

The content root is `docs/`, relative to the working directory, configurable with `Docs:RootPath`. Options: [Site Config](/reference/site-config).

## Linking between pages

Use root-relative links, matching generated navigation and breadcrumb links:

```md
See the [Configuration](/guide/configuration) guide.
```

Relative links such as `./configuration` resolve against the page URL, not the source location. Pages are served at directory-style URLs (`/guide/getting-started/`), so relative links to other folders typically break. Root-relative links are unaffected.

## Using a base path

A base path serves the site from a subdirectory, for example a GitHub Pages project site at `username.github.io/your-repo/` with base path `/your-repo`. It applies to incoming requests and generated links.

Structured front matter (`hero.image`, hero and feature action links) is prefixed automatically. Links and images in the page body are rendered as written and must include the prefix. With base path `/docs`: `/docs/guide/configuration`.

Images: [Asset Handling](/guide/assets#base-path).

## What happens on a request

1. The request path is trimmed and lowercased like file paths; `GET /Guide/Routing/` and `GET /guide/routing` hit the same cache entry.
2. `/` resolves to `Docs:DefaultPage` (default `index`).
3. A cache miss returns the 404 page.

::: note
URLs map 1:1 to file paths. Changing a URL requires renaming the file; use a [redirect](/reference/frontmatter-config#redirects) to preserve the old URL.
:::

## Building the sidebar

Without `sidebar` or `nav` in `config.json`, the sidebar is generated from the folder tree: sub-folders before files, alphabetical within each group. Grouped sections, header dropdowns and per-section sidebars: [Configuration](/guide/configuration).
