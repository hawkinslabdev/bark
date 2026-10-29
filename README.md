# 🌳 Bark

[![License](https://img.shields.io/badge/license-EUPL%201.2-blue)]( LICENSE)
[![Last commit](https://img.shields.io/github/last-commit/hawkinslabdev/bark)](https://github.com/hawkinslabdev/bark/commits/main)
[![Docker](https://img.shields.io/badge/ghcr.io-hawkinslabdev%2Fbark-blue?logo=docker)](https://github.com/hawkinslabdev/bark/pkgs/container/bark)

Meet Bark: your new favorite way to serve documentation. If you have a folder full of [Markdown](https://www.markdownguide.org/getting-started/) files, you have a beautiful, searchable documentation site ready to go.

Built on the modern .NET stack, Bark is designed for those who want a powerful, self-hosted solution without being tethered to the traditional Microsoft ecosystem.

<div>
      <p align="center"><strong>🔍 <a href="https://hawkinslabdev.github.io/bark/">See it in action!</a></strong></p>
</div>

![Screenshot of Bark](.github/images/preview.webp)

---

## Installation

Pick whichever path fits your environment. Docker is the fastest way to get running.

### Option A: Docker Compose

Prebuilt images are published to GHCR on every tagged release. Create a `docker-compose.yml` file with the following content:

```yaml
services:
  bark:
    image: ghcr.io/hawkinslabdev/bark:latest
    container_name: bark
    ports:
      - "8080:8080"
    volumes:
      - ./docs:/app/docs:ro,Z
    environment:
      PublicBaseUrl: https://docs.example.com
      AllowedHosts: docs.example.com
```

Mount your own `docs/` folder containing your `.md` files and optional `config.json`, and set `PublicBaseUrl` to the origin you serve from. When that is done, run `docker compose up -d` to start the container and browse to `http://localhost:8080`.

Running locally only? Drop the `environment:` block. Left unset, absolute URLs fall back to the request's `Host` header, which is fine on localhost but caller-controlled on a public host.

### Option B: Windows / IIS

For those running Windows, follow the following steps:

1. Download the latest `*-Windows_x64.zip` from [Releases](https://github.com/hawkinslabdev/bark/releases).
2. Extract it to your site folder (e.g. `C:\inetpub\bark`).
3. In IIS, create a site (or app) pointing at that folder, with the **No Managed Code** .NET CLR version.
4. The zip already includes a `web.config` wired for in-process hosting. No manual edits needed.
5. Make sure the [.NET 11 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) is installed on the server (gives IIS the ASP.NET Core Module).
6. Start the site and browse to it.

A Linux x64 self-contained-runtime build (`*-Linux_x64.zip`) is published alongside each release. See [Option C: Linux release zip](Bark/docs/getting-started/deploy#option-c-linux-release-zip) for installation instructions.

## Configuration

Keeping `docs/` in sync with a Git remote, including private-repos, is covered in [Deploy](Bark/docs/guide/deploy.md#keeping-docs-in-sync-with-git).

#### Writing documentation

Drop Markdown files into `docs/`. Folder structure becomes the navigation tree; `index.md` becomes a section's landing page.

```markdown
---
title: Installation
description: Get Bark running locally
---

# Installation

You can write your own documentation right here!
```

Front attributes such as `title`, `description` are entirely optional. Bark falls back to the filename.

#### Configuration

Drop a `docs/config.json` to set brand text, navigation, and social links:

```json
{
  "brand": "Bark",
  "nav": [
    {
      "title": "Getting Started",
      "items": [
        { "title": "Installation", "path": "getting-started/installation" }
      ]
    }
  ]
}
```

When `nav` is present it replaces the auto-generated folder-based navigation. Both content and config are hot-reloaded. No restart needed.

Theme overrides, such as CSS variables, dark mode and custom CSS, are handled through environment variables or `appsettings.json` instead.

## License

Licensed under the **EUPL 1.2** license. See [LICENSE](LICENSE) for details.
