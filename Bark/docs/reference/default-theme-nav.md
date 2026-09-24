---
title: Navigation
description: The header navigation bar, configured via topNav in config.json
---

# Navigation

The header navigation bar is defined by `topNav` in `docs/config.json`. Without `topNav`, the header contains only the brand and search box.

```json
{
  "topNav": [
    { "text": "Home", "link": "/" },
    { "text": "Guide", "link": "/guide/getting-started" },
    { "text": "Reference", "link": "/reference/" },
    {
      "text": "More",
      "items": [
        { "text": "GitHub", "link": "https://github.com/hawkinslabdev/bark" },
        { "text": "Releases", "link": "https://github.com/hawkinslabdev/bark/releases" }
      ]
    }
  ]
}
```

## Item shapes

Each `topNav` entry has one of two shapes:

| Shape | Fields | Renders as |
|---|---|---|
| Link | `text`, `link` | A direct link. |
| Dropdown | `text`, `items` | A button that opens a menu of links on hover or focus. |

`link` defines a link; `items` (an array of the same shapes) defines a dropdown. An entry must not set both.

Links starting with `http://` or `https://` are external: they open in a new tab with `rel="noopener noreferrer"` and an external-link icon. Other links are normalized to absolute paths and participate in active-link highlighting.

## Active state

A `topNav` link receives the `active` class when `link` matches the current path exactly. Dropdown triggers have no active state, including when a child matches.

## Mobile

At 768px and below, the header navigation is hidden and its items render at the top of the sidebar drawer. Dropdowns render as native `<details>` disclosures and work without JavaScript. `topNav` configures both layouts.
