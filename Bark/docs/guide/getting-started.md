---
title: Getting Started
description: Run Bark locally in under a minute
---

# Getting Started

This guide runs Bark locally with Docker. Background: [What is Bark?](/guide/what-is-bark).

## Installation

Other install methods (Windows/IIS, Linux release zip, source build): [Deploy](/guide/deploy).

Create a `docker-compose.yml`:

```yaml
services:
  bark:
    image: ghcr.io/hawkinslabdev/bark:latest
    container_name: bark
    ports:
      - "8080:8080"
    volumes:
      - ./docs:/app/docs:ro,Z
```

The `./docs` volume holds the content: Markdown files and an optional `config.json`.

```bash
docker compose up -d
```

The site is served at `http://localhost:8080`.

## File structure

```
docs/
├── config.json                      ← Configuration file (optional)
├── index.md                         ← Homepage
├── guide/
│   ├── getting-started.md           ← Served at /guide/getting-started
│   ├── configuration.md
│   ├── routing.md
│   └── deploy.md
└── reference/
    ├── site-config.md
    ├── api-reference.md
    └── sitemap-generation.md
```

The folder layout determines navigation and URLs.

## What's next

- [Configuration](/guide/configuration): `config.json` options, themes, and branding.
- [Routing](/guide/routing): the exact rules for turning a file path into a URL.
- [Using Markdown](/guide/markdown): every Markdown extension Bark supports, with live examples.
- [Frontmatter](/reference/frontmatter-config): every field a page can set.
- [Deploy](/guide/deploy): Docker, IIS, Linux, or source, plus the production defaults Bark comes with.
