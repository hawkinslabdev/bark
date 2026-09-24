---
title: Global Meta Tags
description: Configuring page titles, meta descriptions, the HTML lang attribute, and any head tags in docs/config.json
---

# Global Meta Tags

`docs/config.json` controls the `<head>` metadata used by search engines, social previews and assistive technology.

## Page Titles

The page title comes from frontmatter `title`. A site-level `title` is appended as a suffix.

```json
{
  "title": "Bark"
}
```

Result: `Getting Started | Bark`. Without a site `title`, the page title is used alone.

`titleTemplate` defines the pattern with two placeholders:

| Placeholder | Replaced with |
|---|---|
| `:title` | The current page's title |
| `:siteName` | The value of `title` |

```json
{
  "title": "Bark",
  "titleTemplate": ":title · :siteName"
}
```

Result: `Configuration · Bark`. Placeholder order and separator are free-form.

::: info
Without `title`, `:siteName` resolves to an empty string. A fixed `titleTemplate` such as `"Bark Docs"` gives every page the same title.
:::

## Site Description

`description` sets the site-wide `<meta name="description">` fallback, used for pages without a frontmatter `description`.

```json
{
  "description": "A fast, lightweight documentation server for ASP.NET Core."
}
```

## Language

`lang` sets the `lang` attribute on `<html>` (default `"en"`). Screen readers use it to select pronunciation, and search engines to classify the page. Set it explicitly for non-English sites.

```json
{
  "lang": "fr"
}
```

Values are [BCP 47](https://www.ietf.org/rfc/bcp/bcp47.txt) tags, for example `"en"`, `"fr"`, `"de"`, `"ja"`, `"zh-CN"`, `"pt-BR"`.

## Automatic Canonical and Social Meta

Every page emits a canonical link and Open Graph / Twitter Card tags without configuration.

The canonical URL combines `PublicBaseUrl` (or the request scheme and host when unset) with the page path. `/guide/installation/` on `https://docs.example.com`:

```html
<link rel="canonical" href="https://docs.example.com/guide/installation/">
```

Open Graph and Twitter Card tags use the canonical URL, the page `title` and `description`, and site settings:

```html
<meta property="og:type" content="article">
<meta property="og:title" content="Installation">
<meta property="og:url" content="https://docs.example.com/guide/installation/">
<meta property="og:site_name" content="Bark">
<meta property="og:locale" content="en">
<meta property="og:description" content="How to install Bark.">
<meta name="twitter:description" content="How to install Bark.">
<meta property="og:image" content="https://docs.example.com/site-og.png">
<meta name="twitter:image" content="https://docs.example.com/site-og.png">
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="Installation">
```

Field sources:

- `og:site_name` from `brand` or `title`; `og:locale` from `lang`.
- `og:type` is `website` for `/` and `article` elsewhere; article pages include `article:modified_time`.
- Description tags are omitted when no description is available.

### Social Preview Image

Resolution order: frontmatter `image`, site `image` in `config.json`, then `brandImage`. Root-relative paths such as `/og.png` are made absolute against the request origin.

```json
{
  "image": "/site-og.png"
}
```

With an image, the Twitter card type is `summary_large_image`; without one, `summary`, and image tags are omitted. Per-page images: [Frontmatter Config](/reference/frontmatter-config).

### Structured Data (JSON-LD)

Every page emits an `application/ld+json` block: `WebSite` for the home page, `Article` (title, description, image, modified date) for other pages, plus `BreadcrumbList` when breadcrumbs exist.

No configuration is required. The block carries the page nonce and is compatible with a strict Content Security Policy.

::: info
Canonical, Open Graph, Twitter Card and JSON-LD output is generated per request. Matching tags added through `head` are emitted in addition to, not instead of, the generated ones.
:::

## Extra Head Tags

`head` injects arbitrary tags into `<head>` on every page, for example verification tags, extra structured data or third-party snippets.

Each entry has three fields:

| Field | Type | Description |
|---|---|---|
| `tag` | `string` | The HTML tag name, for example `"meta"`, `"link"`, or `"script"`. |
| `attrs` | `Record<string, string>?` | Attribute key-value pairs. Values are HTML-encoded automatically. |
| `content` | `string?` | Inner HTML for tags that wrap content, such as `<script>` or `<style>`. Void elements like `<meta>` and `<link>` do not use this field. |

Example with supplementary Open Graph fields and a structured data block:

```json
{
  "head": [
    { "tag": "meta", "attrs": { "property": "og:site_name", "content": "Bark" } },
    { "tag": "meta", "attrs": { "property": "og:image", "content": "https://bark.example.com/og-image.png" } },
    {
      "tag": "script",
      "attrs": { "type": "application/ld+json" },
      "content": "{\"@context\":\"https://schema.org\",\"@type\":\"WebSite\",\"name\":\"Bark\"}"
    }
  ]
}
```

`<meta>`, `<link>` and `<base>` render as void elements. Other tags render with the `content` value, if any, and a closing tag.

::: info
`head` entries apply to every page. Per-page metadata: [Frontmatter Config](/reference/frontmatter-config).
:::

## A Complete Example

```json
{
  "title": "Bark",
  "titleTemplate": ":title | :siteName",
  "description": "A fast, lightweight documentation server for ASP.NET Core.",
  "lang": "en",
  "head": [
    { "tag": "meta", "attrs": { "property": "og:site_name", "content": "Bark" } },
    {
      "tag": "script",
      "attrs": { "type": "application/ld+json" },
      "content": "{\"@context\":\"https://schema.org\",\"@type\":\"WebSite\",\"name\":\"Bark\"}"
    }
  ]
}
```

Canonical, Open Graph, Twitter Card and JSON-LD tags are generated automatically and are therefore absent from this example.
