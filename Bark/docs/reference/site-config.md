---
title: Site Config
description: Full reference for appsettings.json and docs/config.json
---

# Site Config

Configuration is split across two files:

1. **Environment settings** in `appsettings.json` or [environment variables](/guide/environment-variables): deployment settings such as content root, hot reload and base path.
2. **Site configuration** in `docs/config.json`: reader-facing settings such as site name, navigation, footer and social links. Stored with the content.

`config.json` can be versioned with the docs independently of server deployment.

Guide: [Configuration](/guide/configuration).

## Server settings

Source: `appsettings.json`, section `Docs`.

Applied at startup; changes require a restart. Every field can be set as an environment variable; see [Environment Variables](/guide/environment-variables).

| Option | Type | Default | Description |
|---|---|---|---|
| `RootPath` | `string` | `docs` | Path to the Markdown files directory, relative to the app's working directory. |
| `DefaultPage` | `string` | `index` | Page served at `/`. |
| `EnableHotReload` | `bool` | `true` | Watch `*.md` and `config.json` for changes and rebuild in the background. Disable in production when content ships with the deployment. |
| `BasePath` | `string?` | `null` | Prefix every internal link, theme asset URL, and API call with this path segment. This is the setting to reach for when Bark is not served from the domain root, for example a GitHub Pages project page at `you.github.io/your-repo/` or a reverse proxy mounting Bark under `/docs`. A CLI `--base-path` flag overrides this value at runtime, which is how [static export](/guide/deploy#option-e-static-export-github-pages-etc) adjusts it without requiring a config edit. |
| `ContentSecurityPolicy` | `string?` | `null` | A custom `Content-Security-Policy` header value. Replaces the built-in default entirely; it does not extend it. Override only when required, for example to allow an external font host. The default policy disallows inline scripts and styles that do not have its per-request nonce, restricts every fetch directive to `'self'`, and disables framing. |

## Themes

Source: `appsettings.json`, section `Docs:Themes`.

**About Theming**

Theme sources in `wwwroot/theme/`, loaded at startup: `custom.css` (after the built-in styles), `custom.js` (before `</body>`), and `theme.json` (CSS variable overrides).

`Docs:Themes` in `appsettings.json` accepts the same options. When present, it replaces `theme.json`; the sources are not merged.

Guide: [Themes](/guide/themes).

**Configuration**

All fields are optional CSS variable overrides or feature toggles; unset fields use theme defaults. `wwwroot/theme/theme.json` accepts the same fields in camelCase.

| Option | Type | Maps to | Description |
|---|---|---|---|
| `Name` | `string` | n/a | Built-in theme name. Overrides `config.json`'s `theme`; the `--theme` flag overrides both. |
| `PrimaryColor` | `string` | `--primary-color`, `--accent` | Accent color used for links and highlights. Applies to both light and dark mode. |
| `BgColor` | `string` | `--bg-color` | Page background. |
| `SidebarBg` | `string` | `--sidebar-bg` | Sidebar background, which can differ from the page background. |
| `TextColor` | `string` | `--text-color` | Primary text color. |
| `TextMuted` | `string` | `--text-muted` | Secondary text: descriptions, timestamps, muted labels. |
| `BorderColor` | `string` | `--border` | Hairline borders throughout the layout. |
| `CodeBg` | `string` | `--code-bg` | Background for inline code and fenced code blocks. |
| `AccentLight` | `string` | `--accent-light` | Light tint of the accent color, used for active and highlighted states. |
| `FontSans` | `string` | `--font-sans` | Body font stack. |
| `FontMono` | `string` | `--font-mono` | Code font stack. |
| `CustomCssUrl` | `string` | n/a | Injects an extra `<link rel="stylesheet">`, loaded after Bark's built-in styles. Takes priority over an auto-detected `wwwroot/theme/custom.css` if both are present. |
| `BrandText` | `string` | n/a | Sidebar brand label. `config.json`'s `brand` field takes priority if set; if `brand` is absent, `config.json`'s `title` is used as a fallback. |
| `DarkMode` | `bool` | n/a | Enables the `prefers-color-scheme: dark` variant and the in-page dark mode switch. Defaults to `true`. |
| `ShowScrollIndicator` | `bool` | n/a | Shows the thin scroll-progress bar pinned to the top of the viewport. Defaults to `true`. |

## Site metadata

Source: `docs/config.json`.

Site identity and `<head>` output. Details: [Global Meta Tags](/reference/site-metadata).

| Option | Type | Description |
|---|---|---|
| `title` | `string?` | Site name. Appended to every page title as `Page Title \| Site Name`. See [HTML Metadata](/reference/site-metadata). |
| `titleTemplate` | `string?` | Custom title pattern using `:title` and `:siteName` as placeholders. For example, `":title · :siteName"` produces `Getting Started · Bark`. Overrides the default suffix format when set. |
| `description` | `string?` | Site-wide fallback `<meta name="description">`. Per-page frontmatter descriptions take priority over this value. |
| `lang` | `string?` | `lang` attribute on `<html>`. Defaults to `"en"`. |
| `theme` | `string?` | Built-in theme name: `default`, `forest`, `signal`, `blueprint`, `ocean`, `space`, `solarized`, `laserwave` or `limelight`. Defaults to `default`. Unknown names log a warning and fall back to the default. Overridden by `Docs:Themes:Name` and the `--theme` flag. See [Themes](/guide/themes#picking-a-theme). |
| `head` | `HeadTag[]?` | Extra tags injected into `<head>` on every page, for example verification tags or structured data. Canonical, Open Graph, Twitter Card and JSON-LD tags are generated automatically. |
| `brand` | `string?` | Sidebar and header brand label. Falls back to `title` if unset, then to `Docs:Themes:BrandText`. |
| `brandImage` | `string?` | An image URL or path to display alongside the brand label in the header, placed to the left of the text. |
| `image` | `string?` | Default social preview image (`og:image` / `twitter:image`) for pages that do not set their own `image` in frontmatter. Falls back to `brandImage` when unset. See [Global Meta Tags](/reference/site-metadata). |
| `footer` | `string?` | Rendered as Markdown inside the page footer. Links and inline formatting are fully supported, plus `{year}`, `{brand}` and `{title}` variables. See [Footer](../default-theme-footer). |
| `bottomNav` | `TopNavItem[]?` | Footer links. Omitted centres the `footer` text; an array, including `[]`, left-aligns it with the links opposite. See [Footer](../default-theme-footer#footer-links). |
| `favicon` | `string?` | A URL or path to an icon file, or a single emoji character to use as an inline SVG favicon. |
| `repo` | `string?` | Repository URL, for example `"https://github.com/you/project"`. Renders a repository link icon in the header. For GitHub, Gitea and Forgejo hosts, the latest release tag is fetched, cached for six hours, and set as the icon tooltip. Requests to non-public addresses are refused. |
| `lastUpdated` | `bool` | Site-wide toggle for the "Last updated" timestamp. Off by default. When enabled, the date shown for each page comes from the file's last-modified time on disk unless the page sets `date` or `updated` in its frontmatter, which takes priority. See [Last Updated Timestamp](../default-theme-last-updated) and [Frontmatter Config](/reference/frontmatter-config#dates). |
| `editLink` | `EditLinkConfig?` | "Edit this page" link displayed near the pagination footer. See [Edit Link](../default-theme-edit-link). |
| `pageControls` | `PageControlsConfig?` | Per-page action menu shown in the breadcrumb bar. When configured, a small button appears that opens a dropdown with actions like downloading the page's Markdown source or subscribing to the RSS feed. |
| `redirectHosts` | `string[]?` | Allowlist of external hosts that a page's frontmatter `redirect:` may point to, for example `["docs.example.org"]`. The site's own host is always allowed. Redirects to any other host are ignored and logged as a warning. See [Frontmatter Config](/reference/frontmatter-config#redirects). |

**`HeadTag`**

| Field | Type | Description |
|---|---|---|
| `tag` | `string` | HTML tag name, for example `"meta"`, `"link"`, or `"script"`. |
| `attrs` | `Record<string, string>?` | Attribute key-value pairs. Values are HTML-encoded automatically. |
| `content` | `string?` | Inner content for non-void tags. For `<script>` and `<style>` the value is written as-is; for any other tag it is HTML-encoded. Void elements (`<meta>`, `<link>`, `<base>`) do not use this field. |

## Navigation

Source: `docs/config.json`.

`topNav` defines the header navigation. `sidebar` maps path prefixes to navigation trees; the longest matching prefix wins. `nav` defines one sidebar for every page. For pages matching a `sidebar` prefix, `sidebar` takes priority over `nav`.

| Option | Type | Description |
|---|---|---|
| `topNav` | `TopNavItem[]?` | Header nav bar. See [Nav](../default-theme-nav). |
| `sidebar` | `Record<string, NavEntry[]>?` | Path-prefix-keyed sidebars. The longest matching prefix for the current page wins; an empty-prefix key (`"/"`) acts as a catch-all. Takes priority over `nav`. See [Sidebar](../default-theme-sidebar). |
| `nav` | `NavEntry[]?` | Single flat sidebar, shared by every page. Ignored for any page that matches a `sidebar` prefix. |

**`TopNavItem`**

| Field | Type | Description |
|---|---|---|
| `text` | `string` | Label shown in the nav bar. |
| `link` | `string?` | Direct link. Omit this field to make the item a dropdown instead. |
| `items` | `TopNavItem[]?` | Dropdown children. Omit `link` when using this field. |

A `TopNavItem` is a link (`text`, `link`) or a dropdown (`text`, `items`), never both.

**`NavEntry`**

| Field | Type | Description |
|---|---|---|
| `title` | `string` | Link text or group heading. |
| `path` | `string?` | Leaf link target. Omit this to make the entry a group. |
| `items` | `NavEntry[]?` | Child entries. Set this (and omit `path`) to create a group. Groups nest to any depth. |
| `collapsed` | `bool?` | Group-only. Omitting this field means the group is not collapsible. `false` means collapsible and starts expanded. `true` means collapsible and starts collapsed. |

**`EditLinkConfig`**

| Field | Type | Description |
|---|---|---|
| `pattern` | `string` | URL template with a `:path` placeholder, which Bark replaces with the file's path relative to your docs root, preserving original casing. |
| `text` | `string` | Link label. Defaults to `"Edit this page"`. |

**`PageControlsConfig`**

| Field | Type | Description |
|---|---|---|
| `downloadMarkdown` | `bool` | When `true`, the menu gains two actions: **Copy page** copies the Markdown source to the clipboard; **View as Markdown** opens it inline in a new tab via `GET /raw/{path}?view=true`. |
| `subscribeRss` | `bool` | When `true`, the menu includes a **Copy RSS feed URL** action that copies the absolute feed URL to the clipboard, and adds `<link rel="alternate" type="application/rss+xml">` to every page for feed discovery. |
| `editLink` | `PageControlsEditLinkConfig?` | When set, adds an entry at the bottom of the menu that links to the page's source using the top-level `editLink.pattern`. The label and icon are configured here independently from the `editLink` footer link. |

**`PageControlsEditLinkConfig`**

| Field | Type | Description |
|---|---|---|
| `label` | `string` | Menu item label. Defaults to `"Edit this page"`. |
| `icon` | `string?` | Named icon to display alongside the label. Accepts the same icon names as `socialLinks`, for example `"github"`. Falls back to a generic external-link icon when omitted. |

**Full example** (this site's configuration):

```json
{
  "title": "Bark",
  "description": "A fast, lightweight Markdown documentation server built on .NET.",
  "lang": "en",
  "brand": "Bark",
  "brandImage": "/brand-image.svg",
  "footer": "Built with Bark · [EUPL-1.2](LICENSE)",
  "favicon": "🌳",
  "lastUpdated": true,
  "editLink": {
    "pattern": "https://github.com/hawkinslabdev/bark/edit/main/docs/:path",
    "text": "Edit this page on GitHub"
  },
  "topNav": [
    { "text": "Home", "link": "/" },
    { "text": "Guide", "link": "/guide/getting-started" },
    { "text": "Reference", "link": "/reference/site-config" },
    {
      "text": "More",
      "items": [
        { "text": "GitHub", "link": "https://github.com/hawkinslabdev/bark" },
        { "text": "Releases", "link": "https://github.com/hawkinslabdev/bark/releases" }
      ]
    }
  ],
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
          { "title": "Site Config", "path": "reference/site-config" },
          {
            "title": "Customisation",
            "items": [
              { "title": "Nav", "path": "reference/default-theme-nav" },
              { "title": "Sidebar", "path": "reference/default-theme-sidebar" }
            ]
          }
        ]
      }
    ]
  },
  "socialLinks": [
    { "icon": "github", "url": "https://github.com/hawkinslabdev/bark", "title": "GitHub" },
    { "icon": "mastodon", "url": "https://fosstodon.org/@example", "title": "Mastodon" }
  ],
  "pageControls": {
    "downloadMarkdown": true,
    "subscribeRss": true,
    "editLink": {
      "label": "Edit on GitHub",
      "icon": "github"
    }
  }
}
```

## Social links

Source: `docs/config.json`.

Social links render in the header; at 768px and below they move into the sidebar drawer. Each entry requires `icon` and `url`.

| Field | Type | Description |
|---|---|---|
| `icon` | `string` | `"github"` and `"mastodon"` render as inline SVGs. Any other value renders as plain text, which works well for short labels like `"npm"` or `"discord"`. |
| `url` | `string` | Link target. Opens in a new tab. |
| `title` | `string?` | Accessible label and tooltip text. Falls back to the `icon` value if omitted. |

## CLI flags

Command-line flags override configuration for a single run. Used mainly for [static export](/guide/deploy#option-e-static-export-github-pages-etc); also valid in server mode.

| Flag | Overrides | Description |
|---|---|---|
| `--export <dir>` | | Writes a static HTML export to the given directory and exits. Disables hot reload automatically. |
| `--base-url <origin>` | | The public origin used when building absolute URLs in `robots.txt` and `llms.txt`. |
| `--base-path </prefix>` | `Docs:BasePath` | Prefixes all links, theme assets, and API routes with this path segment. Used for GitHub Pages project sites and reverse proxies that mount Bark under a subpath. |
| `--theme <name>` | `Docs:Themes:Name`, `config.json` `theme` | Forces a built-in theme for this run, for example to preview a theme or export variants. |

## What Bark does not configure

There are no build pipeline options, bundler settings or lifecycle hooks; Bark has no client-side build step.

Math rendering, syntax highlighting and custom containers are always enabled.

Custom asset pipelines and structural layout changes require modifying the source. Feature requests: [issue tracker](https://github.com/hawkinslabdev/bark/issues).