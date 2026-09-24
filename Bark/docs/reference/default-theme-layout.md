---
title: Layout
description: The two page layouts Bark renders and what each one includes
---

# Layout

Bark has two layouts, selected by the front matter `layout` field:

1. Document layout (default)
2. Home layout

The latter is only used for the initial landing page, which is set optionally.

## Document

Pages without `layout: home` use the doc layout:

- Header nav bar (if `topNav` is configured).
- Left sidebar (auto-generated, or from `nav`/`sidebar` config).
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