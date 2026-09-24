---
title: Pagination
description: How Bark derives the pagination links at the bottom of each page
---

# Pagination

Every doc-layout page renders previous and next links, derived from navigation order: the `config.json` navigation, or the auto-generated tree when none is configured.

## How the order is determined

### Using config file (recommended)

Pagination follows the `sidebar`/`nav` order in `config.json`. Reordering entries reorders pagination.

### Without configuration

The auto-generated navigation tree is flattened into an ordered list. The pages immediately before and after the current page become previous and next.

## Link text

Link text is the target page title, resolved in the standard order: front matter `title`, then the navigation or filename fallback. There is no separate pagination label. See [Frontmatter](/reference/frontmatter-config).

Pagination links display the title and a directional arrow. The navigation region label and the visually hidden link prefixes (for example "Previous: Routing") use strings from the [locale files](/guide/configuration#translations) (`pagerAria`, `pagerPrevious`, `pagerNext`). Links carry `rel="prev"` and `rel="next"`.

## Disabling per page

`pagination: false` in frontmatter hides the links on that page. Typical use: changelogs, standalone landing pages, reference tables.

```yaml
---
pagination: false
---
```

The page remains in the navigation order; adjacent pages still link to it.

## Home pages

Pages with `layout: home` never render pagination. See [Layout](/reference/default-theme-layout).
