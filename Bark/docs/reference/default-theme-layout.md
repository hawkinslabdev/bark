---
title: Layout
description: The page layouts Bark renders and what each one includes
---

# Layout

Bark has three layouts, selected by the front matter `layout` field:

1. Document layout (default)
2. Wide layout (`layout: wide`)
3. Home layout (`layout: home`)

The home layout is used for the optional landing page.

## Document

Pages without `layout: home` use the doc layout:

- Header nav bar (if `topNav` is configured).
- Left sidebar (auto-generated, or from `nav`/`sidebar` config). `sidebar: false` hides it per page.
- Breadcrumbs.
- Right-hand "On this page" table of contents; a disclosure below 1280px. `toc: false` hides it per page. See [Table of Contents](/reference/default-theme-toc).
- Rendered Markdown content.
- "Edit this page" link (if `editLink` is configured).
- "Last updated" stamp (if enabled).
- Prev/next pagination, derived from nav order.
- The footer (if `footer` is configured).

This means that the following attribute must be set in frontmatter:

```yaml
---
title: Configuration
---
```

Omitting `layout` selects the default layout.

### Prose pages

A document page without a left sidebar and without a table of contents renders as a centered reading column, 52rem wide. Use this for licenses, policies, announcements and other long-form text.

```yaml
---
title: License
sidebar: false
toc: false
---
```

The prose column applies to sites without navigation as well: every page without a table of contents uses it.

## Wide

`layout: wide` renders the document layout without a content width limit. The content fills the space between the sidebar and the table of contents. Without a sidebar and table of contents, the content fills the page width up to 1600px. Use this for wide tables, diagrams and dashboards.

```yaml
---
title: Compatibility matrix
layout: wide
---
```

`layout: wide` overrides the [prose column](#prose-pages).

### Content width

`width` sets the content width as a percentage of the available width. The available width is the space between the sidebar and the page edge, including the table of contents.

```yaml
---
title: Release notes
width: 80%
---
```

- Accepts whole numbers from `10%` to `100%`. The `%` sign is optional. Other values are ignored.
- Applies from 1025px viewport width. Narrower viewports use the full width.
- Overrides `layout: wide` and the prose column.

## Home

To enable the home layout, make sure to set the following attributes:

```yaml
---
layout: home
hero:
  name: Bark
  text: Markdown in, docs site out.
---
```

`layout: home` removes the sidebar, breadcrumbs and table of contents, and renders a full-width hero section and features grid. Schema for `hero` and `features`: [Home Page](../default-theme-home-page).

> [!NOTE]  
> Home pages never render "Edit this page", "Last updated" or pagination links, regardless of configuration. A landing page is outside the linear reading order.