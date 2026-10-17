---
title: Homepage
description: Landing page layouts, home for a project landing and help for a help center
---

# Homepage

Two layouts replace the doc interface (sidebar, table of contents, breadcrumbs) with a landing page:

- `layout: home` is a project landing page: a hero with actions, then feature cards. This site's [index](/) uses it; [Banner home](/examples/banner-home) shows the `banner` variant.
- `layout: help` is a help center: a search field, topics and popular articles. See [Help center](#help-center) and the [example](/examples/help-center).

```yaml
---
layout: home
hero:
  name: Bark
  text: Markdown in, docs site out.
  tagline: Drop .md files in a folder. Get a searchable, navigable docs site.
  image: /brand-image.svg
  actions:
    - theme: brand
      text: Get Started
      link: /guide/getting-started
    - theme: minimal
      text: View on GitHub
      link: https://github.com/hawkinslabdev/bark
features:
  - icon: ⚡
    title: Hot reload
    details: Edit a file, see it live.
---
```

With an image file, the hero sits beside it; with an emoji or no image, the hero is centered. Add `variant: banner` under `hero` for a full-width band in the brand colour.

## Hero

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | no | Small heading above `text`, typically the product name. |
| `text` | `string` | no | The big headline. |
| `tagline` | `string` | no | Supporting subtext below `text`. |
| `image` | `string` | no | An image path or URL, shown beside the headline (below it on phones), or a single emoji/character, shown above a centered headline. |
| `actions` | `HeroAction[]` | no | Call-to-action buttons. |
| `variant` | `"banner"` | no | Renders the hero as a full-width band in the brand colour, with features as centered columns and a dark footer below. |

**`HeroAction`**

| Field | Type | Required | Description |
|---|---|---|---|
| `theme` | `"brand"` \| `"alt"` \| `"minimal"` | no | `brand` is the filled button with an arrow, `alt` the outline button, `minimal` a text link (with an external-link icon when `external` is `true`). Defaults to `brand`. |
| `text` | `string` | yes | Button label. |
| `link` | `string` | yes | Button target. Internal paths and external URLs are supported. |
| `external` | `boolean` | no | Opens the link in a new tab. Defaults to `true` for absolute `http://` and `https://` URLs. |

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

## Help center

`layout: help` is a search-first landing page for end-user documentation: a title, a large search field, a list of topics with article counts, popular articles and a closing contact line.

```yaml
---
layout: help
hero:
  text: How can we help?
  tagline: Guides and answers for your product.
features:
  - title: Running payroll
    details: Pay runs, corrections and holiday pay.
    link: /running-payroll/
popular:
  - text: Run your first payroll
    link: /running-payroll/first-run
contact: "Can't find what you need? [Contact support](mailto:support@example.com)"
---
```

| Field | Description |
|---|---|
| `hero.text`, `hero.tagline` | Title and subtitle above the search field. Other hero fields are ignored. |
| `features` | Topics, listed in order. The article count is the number of pages under each topic's `link`; it's hidden when there are none. `icon` is ignored. |
| `popular` | Links (`text`, `link`) shown in two columns. Optional. |
| `contact` | One line of Markdown closing the page. Optional. |

The search field opens the site search. Markdown below the front matter renders after these blocks.

## Examples

Both example pages ship with these docs. Copy one into your `docs/` folder as a starting point.

- [Banner home](/examples/banner-home): `layout: home` with `variant: banner`.
- [Help center](/examples/help-center): `layout: help`, with topics pointing at this site's own sections.

::: code-group
```md [examples/banner-home/index.md]
---
title: Banner home
description: Example of the home layout with a banner hero.
layout: home
hero:
  variant: banner
  name: Bark
  text: Serve beautiful docs straight from your Markdown
  tagline: A folder of Markdown files in, a searchable documentation site out.
  image: /brand-image.svg
  actions:
    - theme: brand
      text: Get started
      link: /guide/getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/hawkinslabdev/bark
features:
  - title: Easy to use
    details: Drop Markdown files in a folder. Navigation, breadcrumbs and search follow your structure.
  - title: Focus on what matters
    details: No build step and no framework to learn. Save a file and the live site updates.
  - title: Runs anywhere
    details: Docker, IIS or systemd, or a static export for GitHub Pages.
---
```
```md [examples/help-center/index.md]
---
title: Help center
description: Example of the help-center home layout.
layout: help
hero:
  text: How can we help?
  tagline: Guides and answers for running Bark.
features:
  - title: Guide
    details: Install Bark, write Markdown and deploy your site.
    link: /guide/
  - title: Reference
    details: Every setting in config.json and frontmatter.
    link: /reference/
  - title: Example API
    details: Pages generated from an OpenAPI spec.
    link: /examples/api/
popular:
  - text: Getting started
    link: /guide/getting-started
  - text: Deploy
    link: /guide/deploy
  - text: Site config
    link: /reference/site-config
  - text: Themes
    link: /guide/themes
contact: "Can't find what you need? [Open an issue on GitHub](https://github.com/hawkinslabdev/bark/issues)."
---
```
:::
