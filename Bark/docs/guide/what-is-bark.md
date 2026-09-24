---
title: What is Bark?
description: A fast, lightweight Markdown documentation server
---

# What is Bark?

Bark is a .NET documentation server. It serves a folder of Markdown as a site with navigation, table of contents, breadcrumbs and search, from a single process.

Static site generators require a build step and a deploy of the output. Bark renders in memory at startup and re-renders when a file changes. There is no build step; the only deployable is Bark itself.

## How it compares

**Wikis.** Confluence stores pages in a database and edits them through a web form, with no review step before publishing. Bark content is Markdown in a folder; in git, docs follow the same pull request review as code.

**Static site generators.** [Hugo](https://github.com/gohugoio/hugo){target="_blank" rel="noopener"}, [MkDocs](https://github.com/mkdocs/mkdocs){target="_blank" rel="noopener"} and [VitePress](https://github.com/vuejs/vitepress){target="_blank" rel="noopener"} fit static hosting with a build step. Bark fits teams already running .NET.

## Performance

Pages are held in memory. Requests do not read from disk.

- File changes are debounced; a burst of saves triggers one rebuild.
- Responses carry an ETag; repeat requests receive `304 Not Modified`.

## Ready to try it out?

Next: [Getting Started](/guide/getting-started).
