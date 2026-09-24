---
title: Sitemap & Crawlers
description: How Bark generates sitemap.xml, robots.txt, and llms.txt
---

# Sitemap & Crawlers

Three endpoints are generated from the in-memory page list and rebuilt when content changes. No separate command is required.

## `sitemap.xml`

```bash
curl http://localhost:5000/sitemap.xml
```

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url><loc>/</loc><priority>1.0</priority></url>
  <url><loc>/guide/getting-started</loc><lastmod>2026-06-20</lastmod><priority>0.8</priority></url>
  ...
</urlset>
```

Priority: `1.0` for the home page, `0.8` for other pages. `<lastmod>` is the Markdown file's last-write time; Git history is not consulted.

## `robots.txt`

```
User-agent: *
Allow: /
Sitemap: https://your-host/sitemap.xml
```

Generated per request. The `Sitemap:` URL uses `PublicBaseUrl` when set, otherwise the host of the request. See [Environment Variables](/guide/environment-variables#public-base-url).

::: note
Behind a reverse proxy without `PublicBaseUrl`, forwarded headers must be configured so the original scheme and host are used instead of the proxy's internal address. See [Deploy](/guide/deploy).
:::

## `llms.txt`

```
# Bark

- [Getting Started](https://your-host/guide/getting-started): Get Bark running locally in under a minute
- [Configuration](https://your-host/guide/configuration): appsettings.json options, docs/config.json, and theming
...
```

Lists every page with title, URL and description, as a navigation-free index for LLM agents.

All three files include every page in `docs/` without configuration.