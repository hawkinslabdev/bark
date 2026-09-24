---
title: Configuration
description: appsettings.json options, docs/config.json, and theming
---

# Configuration

Configuration is split across two files:

- **`appsettings.json`**: host settings (content root, hot reload, theme colors). Per deployment; applied on restart.
- **`docs/config.json`**: content settings (title, metadata, brand, navigation, footer, social links). Per project; applied through hot reload.

Content settings do not require deployment access.

This page covers the common settings. Field reference: [Site Config](/reference/site-config). Container variable names: [Environment Variables](/guide/environment-variables).

## Server settings

Defined in the `Docs` section of `appsettings.json`:

| Setting | Default | Description |
|---|---|---|
| `RootPath` | `docs` | Path to the Markdown files directory, relative to the app's working directory. |
| `DefaultPage` | `index` | Page served at `/`. |
| `EnableHotReload` | `true` | Watch `*.md` and `config.json` for changes and rebuild in the background. |
| `BasePath` | `null` | Prefix for every internal link and asset URL. Set this when Bark is served from a subpath instead of the domain root. |

```json
{
  "Docs": {
    "RootPath": "../../docs",
    "DefaultPage": "index",
    "EnableHotReload": true,
    "BasePath": "/your-repo"
  }
}
```

::: tip
For [static export](/guide/deploy#option-e-static-export-github-pages-etc), the `--base-path` flag typically replaces `BasePath`. Set `BasePath` when a reverse proxy mounts the server under a subpath.
:::

Colors, fonts, custom CSS and JavaScript: [Themes](/guide/themes).

## Site settings

`docs/config.json` defines the site title, HTML metadata, navigation, footer and social links. Navigation is covered below; other fields: [Site Config](/reference/site-config).

Navigation options (combinable):

1. **Auto-generated.** Without `nav`, `sidebar` or `topNav`, the sidebar is built from the folder structure.
2. **Single sidebar** (`nav`) for the whole site.
3. **Header navigation with dropdowns** (`topNav`) and **per-section sidebars** (`sidebar`, keyed by path prefix).

```json
{
  "brand": "Bark",
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
        "items": [
          { "title": "Getting Started", "path": "guide/getting-started" },
          { "title": "Configuration", "path": "guide/configuration" },
          { "title": "Routing", "path": "guide/routing" },
          { "title": "Deploy", "path": "guide/deploy" }
        ]
      }
    ],
    "/reference/": [
      {
        "title": "Reference",
        "items": [
          { "title": "Site Config", "path": "reference/site-config" },
          { "title": "API Reference", "path": "reference/api-reference" },
          { "title": "Sitemap & Crawlers", "path": "reference/sitemap-generation" }
        ]
      }
    ]
  },
  "socialLinks": [
    { "icon": "github", "url": "https://github.com/hawkinslabdev/bark", "title": "GitHub" }
  ]
}
```

A `topNav` item is a link (`text`, `link`) or a dropdown (`text`, `items`).

`sidebar` keys are path prefixes; the longest match wins, so `/guide/` and `/guide/advanced/` can coexist.

::: tip
`sidebar` takes priority over `nav` for matching pages. `nav` replaces the auto-generated navigation on every page. Neither merges with the folder tree.
:::

`footer` is rendered as Markdown. Social link `icon` values `"github"` and `"mastodon"` render as inline SVG; other values render as text.

## Translations

String tables are stored in `docs/locale/`. All interface text (search modal, table of contents heading, pager labels, ARIA labels, 404 page) comes from a string table. English is built in. `locale` selects a language file:

```json
{
  "locale": "nl"
}
```

This loads `docs/locale/nl.json`. Object form: `"locale": { "code": "nl" }`. Without `locale`, `lang` is used, then English.

`docs/locale/en.json` lists every key and serves as the template:

```json
{
  "searchPlaceholder": "Documentatie doorzoeken...",
  "tocTitle": "Op Deze Pagina",
  "pagerNext": "Volgende"
}
```

Rules:

- Missing keys fall back to English.
- Placeholders such as `{0}` must be kept as in the English source.
- Unknown keys are ignored and logged as a warning.
- Invalid JSON is skipped with a warning; the interface falls back to English.

Locale files apply through hot reload. They are not served over HTTP or listed by the API.

Bundled: `en.json`, `nl.json`. `locale` sets one interface language for the site; per-tree languages are configured under [Translated pages](#translated-pages).

## Translated pages

Page translations use one directory per language, declared in `config.json`:

```json
{
  "locales": {
    "root": { "label": "English", "lang": "en" },
    "nl": { "label": "Nederlands", "lang": "nl" }
  }
}
```

`root` is the untranslated tree at the top of `docs/`. Other keys are both directory name and locale code: `docs/nl/guide/install.md` is served at `/nl/guide/install/` with strings from `docs/locale/nl.json`.

```
docs/
  index.md                  → /
  guide/install.md          → /guide/install/
  nl/
    index.md                → /nl/
    guide/install.md        → /nl/guide/install/
```

Each tree has its own navigation, search index and `<html lang>`. The header includes a language switcher (globe icon) when two or more languages are configured. A `/nl/` `sidebar` key defines that tree's sidebar.

Pages with `layout: home` never render a translation notice.

Untranslated pages are served at the translated URL with the translated interface and a notice linking to the original. Their `canonical` points to the original, and they are excluded from that tree's search index.

When the original is newer than the translation (by timestamp), an outdated-translation notice is rendered.

::: tip
The language is determined by the URL. There is no redirect based on browser language, which keeps responses cacheable and compatible with static export.
:::

### Drafting a translation with LibreTranslate

A one-off generator drafts a translated tree through a self-hosted [LibreTranslate](https://libretranslate.com/) instance. Output is regular Markdown for review and editing; there is no runtime translation.

```bash
dotnet run --project src/Bark -- --translate nl --translate-endpoint http://localhost:5000
```

Flags:

| Flag | Meaning |
|---|---|
| `--translate <code>` | Target locale, and the directory it writes into (`docs/nl/`) |
| `--translate-endpoint <url>` | LibreTranslate base URL, default `http://localhost:5000` |
| `--translate-from <code>` | Source language, defaults to the site's own `locale` |
| `--translate-api-key <key>` | Sent as `api_key` when your instance requires one |
| `--translate-overwrite` | Retranslate files that already exist, which is off by default so your edits survive |

Not translated: fenced and indented code, inline code, link and image targets, HTML blocks, container markers, and front matter except `title` and `description`. Headings receive an explicit `{#anchor}` matching the original slug.

Generated files set `machineTranslated: true`, which renders a machine-translation notice. Remove the flag after human review.

::: warning
Machine-translated pages require review before publication.
:::

### Translating the menus

Text in `config.json` (menu labels, sidebar titles, brand, footer, promo bar, edit-link label) is written once. Translations are stored in the locale file as a dictionary keyed by the original text:

```json
{
  "tocTitle": "Op Deze Pagina",
  "config": {
    "Getting Started": "Aan de slag",
    "Configuration": "Configuratie",
    "Edit this page on GitHub": "Bewerk deze pagina op GitHub",
    "Built with Bark.": "Gebouwd met Bark."
  }
}
```

Navigation structure is shared across languages; only labels are translated. Missing entries render in the original language.

Markdown values use the exact source string, including links, as the key:

```json
"config": {
  "Built with Bark · [EUPL-1.2](/LICENSE).": "Gebouwd met Bark · [EUPL-1.2](/LICENSE)."
}
```

`config` is a reserved key in locale files. Other keys are interface strings; unknown keys are logged as a warning.
