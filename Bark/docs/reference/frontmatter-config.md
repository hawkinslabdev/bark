---
title: Frontmatter Config
description: Reference for page-level YAML frontmatter fields
---

# Frontmatter Config

Every Markdown file accepts an optional YAML [frontmatter](https://www.markdownlang.com/advanced/frontmatter.html){target="_blank" rel="noopener"} block. Omitted fields use defaults; a file without frontmatter is valid.

```yaml
---
title: Configuration
description: appsettings.json options, docs/config.json, and theming
---
```

::: note
Keys are camelCase (`lastUpdated`, not `last_updated`). Unrecognized keys are ignored.
:::

## Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `title` | `string` | filename or nav-configured title | Page title. Used in the document title, breadcrumbs and pagination links. |
| `description` | `string` | none | Meta description. Also used in search results and `llms.txt`. |
| `image` | `string` | site `image`, then `brandImage` | Social preview image for this page (`og:image` / `twitter:image`). An absolute URL, or a root-relative path such as `/og.png`, resolved against the request origin. When set, the Twitter card type is `summary_large_image`. |
| `layout` | `string` | none | `home` renders a hero and features grid instead of the doc interface. See [Home Page](/reference/default-theme-home-page). |
| `hero` | `object` | none | Hero content. Only used when `layout: home`. |
| `features` | `array` | none | Feature cards. Only used when `layout: home`. |
| `keywords` | `string[]` | none | Page keywords. Emitted as `<meta name="keywords">` (capped at 20 entries) and indexed by search at weight 4. |
| `lastUpdated` | `bool` | inherits site-wide setting | `false` hides the "Last updated" stamp, overriding the site-wide setting. See [Last Updated Timestamp](/reference/default-theme-last-updated). |
| `pagination` | `bool` | `true` | `false` hides the previous and next links on this page. |
| `toc` | `bool` | `true` | `false` hides the table of contents on this page. |
| `redirect` | `string` | none | Redirects to the given URL instead of rendering the page. See [Redirects](#redirects). |
| `date` | `string` (ISO 8601) | none | Content creation date. Overrides the file system timestamp for the "Last updated" display when `updated` is not also set. |
| `updated` | `string` (ISO 8601) | none | Last-modified date. Takes priority over `date` and the file system timestamp for the "Last updated" display. |

`title` and `description` apply to every page. `hero` and `features` apply only with `layout: home`. `lastUpdated`, `pagination` and `toc` take effect only when the corresponding site-wide feature is active.

::: tip
There is no per-page `sidebar: false`. Every doc-layout page renders a sidebar. Sidebar scoping: [Sidebar](/reference/default-theme-sidebar).
:::

## Redirects

A page with `redirect` returns a temporary (302) redirect to the target instead of rendering. The file body is ignored.

Root-relative targets (starting with `/`) are prefixed with the base path.

Absolute `http://` and `https://` targets are allowed only for the site's own host or hosts listed in `redirectHosts` in [`config.json`](/reference/site-config). Other targets are ignored with a logged warning, and the page renders normally. This prevents content contributors from creating an open redirect.

Relative targets containing a backslash, whitespace or a control character are also ignored, because browsers resolve `/\example.com` to an external host.

```yaml
---
redirect: /guide/getting-started
---
```

Renaming pattern: after moving `old-name.md` to `new-name.md`, keep `old-name.md` with a `redirect` to the new path to preserve existing links.

## Dates

The "Last updated" date defaults to the file's last-modified time. Moving, renaming or freshly deploying files resets that time.

`date` and `updated` set the date explicitly. Precedence: `updated`, then `date`, then the file's last-modified time.

```yaml
---
date: 2025-03-01
updated: 2025-06-28
---
```

Convention: set `date` on first publish and `updated` on significant revisions. Pages without either field use the file time.

## Title fallback order

Without a frontmatter `title`, the title resolves as follows:

1. The filename, title-cased (`getting-started.md` becomes "Getting Started").
2. If the file is `index.md`, the parent folder name is used instead.
3. If `config.json`'s `nav` or `sidebar` configures a title for this page's path, that title takes priority over the filename.

A frontmatter `title` overrides all of the above.
