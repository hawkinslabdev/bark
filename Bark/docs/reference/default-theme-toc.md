---
title: Table of Contents
description: The "On This Page" outline Bark builds from each page's headings
---

# Table of Contents

Every doc-layout page renders an "On this page" outline built from its headings.

## Content

Headings `##` and deeper become entries; the page title (`#`) is excluded. Nesting is capped at three levels: `#####` renders at the `###` indent.

A page without subheadings gets a single entry for its `#` heading. Anchors use the same slug generation as heading IDs.

## Disabling per page

`toc: false` in frontmatter removes the outline; the content uses the full width.

```yaml
---
toc: false
---
```

There is no site-wide setting.

## Rendering

| Viewport width | Rendering |
|---|---|
| 1280px and wider | Sticky column beside the content |
| 769px to 1279px | "On this page" `<details>` disclosure above the content; works without JavaScript |
| 768px and narrower | Not rendered |

A toggle button beside the sticky column hides and restores the outline; the content column widens while it is hidden. The button exposes `aria-expanded` and `aria-controls`, and its label comes from the `tocCollapse` and `tocExpand` locale strings. The hidden state sets the `hidden` attribute on the outline, removing it from the tab order and accessibility tree. The state persists in `localStorage` under `bark-toc-collapsed`.


## Home pages

Pages with `layout: home` never render an outline, regardless of `toc`. See [Layout](/reference/default-theme-layout).
