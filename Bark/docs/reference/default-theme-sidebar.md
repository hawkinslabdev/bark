---
title: Sidebar
description: The left navigation tree, auto-generated or fully configured via sidebar in config.json
---

# Sidebar

Sidebar sources, in priority order:

1. **`sidebar` in `config.json`**, matched by path prefix. Highest priority.
2. **`nav` in `config.json`**, one flat tree shared by every page. Used only when no `sidebar` prefix matches.
3. **Folder structure**, auto-generated. Used when neither `sidebar` nor `nav` is set.

Option 1 suits sites with several sections, for example a guide and a reference.

## Multi-sidebar config

```json
{
  "sidebar": {
    "/guide/": [
      {
        "title": "Introduction",
        "collapsed": false,
        "items": [
          { "title": "Getting Started", "path": "guide/getting-started" },
          { "title": "Configuration", "path": "guide/configuration" }
        ]
      }
    ],
    "/reference/": [
      {
        "title": "Reference",
        "items": [
          { "title": "Site Config", "path": "reference/site-config" }
        ]
      }
    ]
  }
}
```

Each key is a path prefix; the **longest matching** key applies. `/guide/` and `/guide/advanced/` can coexist, with the more specific key winning. An empty key (`""` or `/`) is the catch-all.

## Entries

Each entry is a link or a group. Groups nest to any depth:

| Field | Type | Description |
|---|---|---|
| `title` | `string` | Link text or group heading. |
| `path` | `string` | Leaf link target: a docs page path, or a full `http://`/`https://` URL. Omit it to make this entry a group. |
| `items` | `array` | Child entries. Set this (and omit `path`) to make this entry a group. |
| `collapsed` | `bool` | Groups only. See [Collapse behavior](#collapse-behavior). |

A group without `title` renders as a heading-less cluster. See [Grouping links without a heading](#grouping-links-without-a-heading).

## External links

A `path` starting with `http://` or `https://` is an external link:

```json
{
  "sidebar": {
    "": [
      {
        "title": "Resources",
        "items": [
          { "title": "Getting Started", "path": "guide/getting-started" },
          { "title": "Source on GitHub", "path": "https://github.com/hawkinslabdev/bark" }
        ]
      }
    ]
  }
}
```

External entries render with an outbound arrow and open in a new tab (`target="_blank"`, `rel="noopener noreferrer"`), like external [top-nav](/reference/default-theme-nav/) items. They are never marked active, never auto-expand their group, and are excluded from [prev/next pagination](/reference/default-theme-prev-next-links/).

Only `http` and `https` are recognized; other values are treated as page paths.

## Collapse behavior

`collapsed` sets whether a group is collapsible and its initial state:

| Value | Behavior |
|---|---|
| omitted | Not collapsible. Always expanded |
| `false` | Collapsible, starts expanded. |
| `true` | Collapsible, starts collapsed. |

A group containing the current page always renders expanded. Collapsing uses native `<details>`/`<summary>` and works without JavaScript.

> [!NOTE] 
> Static groups (no `collapsed`) suit reference material read top to bottom, such as this site's `/reference/` sidebar. Collapsible groups suit long guides.

## Grouping links without a heading

A group without `title` renders its links as one cluster without a heading. Top-level links would otherwise each be separated by a divider.

```json
{
  "sidebar": {
    "": [
      {
        "items": [
          { "title": "Config & API Reference", "path": "reference/site-config" },
          { "title": "Changelog", "path": "more/changelog" }
        ]
      }
    ]
  }
}
```

The cluster is separated from the previous section by a divider. Heading-less groups are always expanded and not collapsible.