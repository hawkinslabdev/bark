---
title: Badge
description: Small inline labels for flagging new features, version requirements, or unstable APIs
---

# Badge

An inline pill-shaped label for flags such as "new", "deprecated" or "requires 3.0+".

```md
## Some heading <Badge type="tip">3.0+</Badge>
```

`<Badge type="...">text</Badge>` is plain HTML in Markdown, styled by the Bark stylesheet. Markdig passes the tag through as raw HTML, and the parser lowercases it to `<badge>`. The closing tag is required.

## Types

| `type` | Color | Maps to |
|---|---|---|
| `tip` | Green | `--alert-tip` (default if `type` is omitted) |
| `info` | Blue | `--alert-note` |
| `warning` | Amber | `--alert-warning` |
| `danger` | Red | `--alert-caution` |

The colors match [Alerts](/guide/markdown#alerts).

<Badge type="tip">tip</Badge>
<Badge type="info">info</Badge>
<Badge type="warning">warning</Badge>
<Badge type="danger">danger</Badge>

## Usage

Inline:

```md
Supports `Ctrl+K` <Badge type="tip">3.0+</Badge> on every page.
```

After a heading:

```md
## Customization <Badge type="warning">beta</Badge>
```

> [!WARNING]
> Self-closing syntax (`<Badge text="x" />`) is not supported. HTML ignores `/>` on unknown elements, so the `<badge>` stays open and absorbs the rest of the paragraph. Use `<Badge type="...">text</Badge>`.
