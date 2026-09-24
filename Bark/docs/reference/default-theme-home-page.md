---
title: Homepage
description: layout:home frontmatter, hero and features
---

# Homepage

`layout: home` replaces the doc interface (sidebar, table of contents, breadcrumbs) with a hero section and a features grid. This site's `index.md` uses it.

```yaml
---
layout: home
hero:
  name: Bark
  text: Markdown in, docs site out.
  tagline: Drop .md files in a folder. Get a searchable, navigable docs site.
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/hawkinslabdev/bark
features:
  - icon: ⚡
    title: Hot reload
    details: Edit a file, see it live.
---
```

## Hero

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | no | Small heading above `text`, typically the product name. |
| `text` | `string` | no | The big headline. |
| `tagline` | `string` | no | Supporting subtext below `text`. |
| `image` | `string` | no | A URL (rendered as `<img>`) or a single emoji/character (rendered as text) shown above the headline. |
| `actions` | `HeroAction[]` | no | Call-to-action buttons. |

**`HeroAction`**

| Field | Type | Required | Description |
|---|---|---|---|
| `theme` | `"brand"` \| `"alt"` | no | `brand` is the filled button, `alt` is the outline button. Defaults to `brand`. |
| `text` | `string` | yes | Button label. |
| `link` | `string` | yes | Button target. Internal paths and external URLs are supported. |

## Features

Array of cards rendered in a responsive grid below the hero.

| Field | Type | Required | Description |
|---|---|---|---|
| `icon` | `string` | no | An emoji, short text, inline SVG string (`<svg>...</svg>`), or image URL shown above the title. Paths starting with `/` or `http(s)://` render as `<img>` tags; inline SVG strings are rendered directly. |
| `iconImage` | `FeatureIconConfig` | no | A themed icon object for separate light and dark variants. Takes priority over `icon` when both are set. |
| `title` | `string` | yes | Card heading. |
| `details` | `string` | yes | Card body text. |
| `link` | `string` | no | Makes the whole card a link. |

**`FeatureIconConfig`**

| Field | Type | Description |
|---|---|---|
| `src` | `string?` | A single image URL used in both light and dark mode. |
| `light` | `string?` | Image URL shown in light mode. Pair with `dark` for full theming. |
| `dark` | `string?` | Image URL shown in dark mode. Pair with `light` for full theming. |
| `alt` | `string?` | Alt text for the image. Defaults to an empty string, treating the icon as decorative. |

> [!NOTE]  
> `hero.image` accepts a URL or a single emoji/character. The `iconImage` format is not supported.

Markdown below the front matter renders beneath the features grid.
