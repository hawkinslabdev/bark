---
title: Asset Handling
description: How Bark serves images and other static files referenced from Markdown
---

# Asset Handling

Markdown content is stored in `docs/`. Images and other static files are stored in `wwwroot/`, which is served over HTTP.

## Where to put files

Reference assets in `wwwroot/` with a root-relative path:

```markdown
![Architecture diagram](/images/architecture.png)

[Download the sample config](/files/sample-config.json)
```

Files in `wwwroot/` are served at their path without processing, hashing or transformation.

::: tip
Non-Markdown files in `docs/` are copied to the build output but are not served over HTTP. Assets must be in `wwwroot/`.
:::

## Relative paths

Pages are served at directory-style URLs: `guide/assets.md` is served at `/guide/assets/`. A relative path such as `./diagram.png` resolves against that URL, not the source folder, and typically returns 404. Use root-relative paths.

## Base path

With `--base-path` (or `Docs:BasePath`) set, root-relative links in structured front matter fields such as `hero.image` and feature links are prefixed automatically.

::: note
Prefixing applies to front matter only. `![](...)` in the page body is rendered as written; include the base path explicitly. With base path `/docs`: `![Logo](/docs/images/logo.png)`.
:::

## External assets

Absolute URLs are rendered as written:

```markdown
![Diagram hosted elsewhere](https://cdn.example.com/diagram.png)
```

## Theme assets

`custom.css` and `custom.js` in `wwwroot/theme/` are loaded at startup without configuration. See [Themes](/guide/themes).
