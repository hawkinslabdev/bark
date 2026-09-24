---
title: API Reference
description: HTTP routes Bark exposes
---

# API Reference

HTTP routes exposed by Bark.

## `GET /{path}`

Returns the rendered HTML page for the given documentation path.

```bash
curl http://localhost:5000/guide/getting-started
```

Request handling:

1. The pre-rendered page is read from the in-memory cache (built at startup, rebuilt on file change).
2. A SHA-256 `ETag` is computed. A matching `If-None-Match` returns `304 Not Modified`.
3. Navigation, breadcrumbs, table of contents and pagination are built.
4. The assembled HTML is returned.

Unknown paths return the 404 page.

## `GET /raw/{path}`

Returns the Markdown source of a page. Used by the [page controls](/reference/site-config#site-metadata) "Copy page" and "View as Markdown" actions.

```bash
curl -O http://localhost:5000/raw/guide/getting-started
```

Default response: file download (`Content-Disposition: attachment`). `?view=true` returns inline `text/plain`, used by "View as Markdown".

Path normalization matches the page route (case-insensitive, trailing slash optional). Unknown paths and paths through symlinks return `404 Not Found`.

Rate limit: 30 requests per minute per IP address (shared policy with `/api/search`).

## `GET /api/search`

Returns a JSON array of results ranked by weighted score.

```bash
curl "http://localhost:5000/api/search?q=hot+reload"
```

| Query param | Required | Notes |
|---|---|---|
| `q` | yes | Search term. Queries under 2 characters return an empty array. |

| Match location | Weight |
|---|---|
| Title | 10 |
| Description | 5 |
| Keywords (frontmatter) | 4 |
| Heading | 3 |
| Body text | 1 |

The in-memory inverted index is rebuilt in full on every docs rebuild.

Rate limit: 30 requests per minute per IP address; excess requests receive `429 Too Many Requests`.

## `GET /api/build-version`

Returns an integer that increments when rendered content changes, not on every filesystem event.

```json
{ "version": 4 }
```

The hot-reload script polls this endpoint and reloads the page on a new value. Usable as a content-change hook.

## `GET /feed.xml`

Returns an RSS 2.0 feed of the 20 most recently modified pages, newest first. Title, description and base URL derive from `docs/config.json`.

```bash
curl http://localhost:5000/feed.xml
```

Rate limit: 30 requests per minute per IP address.

## `GET /sitemap.xml`

Returns an XML sitemap of every page; `<lastmod>` is the file's last-write time.

## `GET /robots.txt`

Returns `robots.txt`. The `Sitemap:` URL uses `PublicBaseUrl`, or the request host when unset (behind a reverse proxy, forwarded headers are required).

```
User-agent: *
Allow: /
Sitemap: https://your-host/sitemap.xml
```

Forwarded headers: [Deploy](/guide/deploy).

## `GET /llms.txt`

Returns a plain-text index of every page (title, URL, description) for LLM crawlers and agents.

See [Sitemap & Crawlers](/reference/sitemap-generation).
