---
title: Deploy
description: Docker, Windows/IIS, Linux release, or build from source
---

# Deploy

Prerequisite: a documentation folder with content (see [Getting Started](/guide/getting-started)). Docker is the quickest option.

## Option A: Docker Compose

Prebuilt images are published to GHCR on every tagged release.

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

Mount `docs/` (Markdown files and an optional `config.json`), set `PublicBaseUrl` to the public origin, and start the container:

```bash
docker compose up -d
```

The site is served at `http://localhost:8080`.

### Keeping `docs/` in sync with Git

Bark can clone and pull `docs/`, configured entirely through environment variables. Credentials are never stored in the remote URL.

```yaml [docker-compose.yml]
services:
  bark:
    image: ghcr.io/hawkinslabdev/bark:latest
    env_file: .env
    volumes:
      - ./docs:/app/docs:Z
```

```bash [.env]
GIT_ENABLED=true
GIT_URL=https://github.com/you/your-docs.git
GIT_USERNAME=you
GIT_PASSWORD=your-token
GIT_CRON=*/5 * * * *
```

Disabled by default. If `docs/` is not a checkout, `GIT_URL` is cloned into it at startup. `git pull --ff-only` then runs on the `GIT_CRON` schedule (5-field cron expression); the file watcher applies the changes. A failed clone or pull logs a warning without stopping the site. Requires a writable `docs/` mount (no `:ro`) owned by the container's non-root user.

`GIT_USERNAME`/`GIT_PASSWORD` are sent as an HTTP Basic auth header per git invocation and never written to the remote URL or `.git/config`. For token-only hosts (for example a GitHub PAT), set `GIT_PASSWORD` to the token and `GIT_USERNAME` to any non-empty value.

Required only when `docs/` is not the repository root:

```
your-repo/
  docs/
  theme/    # picked up automatically, no separate mount
```

```bash [.env]
GIT_ROOT=repo
DOCS_ROOT_PATH=repo/docs
```

```yaml [docker-compose.yml]
volumes:
  - ./repo:/app/repo:Z
```

Symlinks inside `docs/` are excluded from serving, indexing and export. This prevents a committed link from exposing files outside the docs root.

## Option B: Windows / IIS

1. Install the [.NET 10 Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/10.0){target="_blank" rel="noopener"} on the server. It provides the ASP.NET Core Module for IIS.
2. Download the latest `*-Windows_x64.zip` from [Releases](https://github.com/hawkinslabdev/bark/releases){target="_blank" rel="noopener"} and extract it to the site folder, for example `C:\inetpub\bark`.
3. Create an IIS site for that folder with .NET CLR version **No Managed Code**. Bark runs through the ASP.NET Core Module.
4. Start the site.

The included `web.config` configures in-process hosting.

## Option C: Linux release zip

Every release includes a self-contained Linux x64 build.

1. Download the latest `*-Linux_x64.zip` from [Releases](https://github.com/hawkinslabdev/bark/releases){target="_blank" rel="noopener"} and extract it:

```bash
mkdir -p /srv/bark && unzip Bark-*-Linux_x64.zip -d /srv/bark
mkdir -p /srv/bark/docs   # your .md files go here
```

2. Run it; the site is served at `http://localhost:8080`:

```bash
cd /srv/bark && ./Bark
```

::: note
`docs/` is resolved relative to the working directory, not the executable. `Docs:RootPath` overrides it. See [Environment Variables](/guide/environment-variables).
:::

Persistence across reboots: [Running as a service](#running-as-a-service-source-builds).

## Option D: Build from source

For contributing to Bark or running without a container image. Requires the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0){target="_blank" rel="noopener"}.

```bash
cd Bark
dotnet publish src/Bark -c Release -o ./publish
cd publish && dotnet Bark.dll
```

`docs/` is copied into the publish output. The target machine requires the .NET runtime unless published with `--self-contained true -r <rid>`.

For development, `dotnet watch --project src/Bark` adds C# hot reload.

## Option E: Static export (GitHub Pages, etc.)

Exports static HTML, CSS and JavaScript for any static host. Requires a source build.

```bash
dotnet publish src/Bark -c Release -o ./publish
cd publish && ./Bark --export ./output --base-url https://you.github.io --base-path /your-repo
```

| Flag | Purpose |
|---|---|
| `--export <dir>` | Writes every page, plus `404.html`, `robots.txt`, `llms.txt`, `sitemap.xml`, and `wwwroot` to the given directory. |
| `--base-url <origin>` | The real public origin used for absolute URLs in `robots.txt` and `llms.txt`. |
| `--base-path </prefix>` | Required when the site lives under a subpath, such as a GitHub project page (`you.github.io/your-repo/`). Overrides `Docs:BasePath` at runtime. See [Site Config](/reference/site-config). |

::: note
Run the binary from the publish folder (`cd publish`); `docs/` is resolved relative to the working directory. `--export` disables hot reload and `/api/build-version` polling. Search queries a prebuilt `search-index.json` client-side.
:::

GitHub Actions example: `.github/workflows/bark-deployment.yml`. Requires **Settings → Pages → Source → GitHub Actions** once per repository.

## What you get by default

Enabled without configuration:

* **Compression.** Brotli or Gzip on all traffic, including HTTPS.
* **DoS limits.** Caps on request body size, header size, simultaneous connections, and keep-alive timeouts.
* **Console logging.** Verbosity is configurable per environment.
* **[ETags](https://en.wikipedia.org/wiki/HTTP_ETag){target="_blank" rel="noopener"}.** SHA-256 per page; unchanged pages return `304 Not Modified`.

Not included: domain, firewall and TLS certificates.

## Reverse proxy setup

Bark runs behind a web server or load balancer that terminates TLS. The Docker image listens on port 8080:

```nginx
server {
    listen 443 ssl;
    server_name docs.example.com;

    location / {
        proxy_pass         http://127.0.0.1:8080;
        proxy_set_header    Host $host;
        proxy_set_header    X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header    X-Forwarded-Proto $scheme;
    }
}
```

Source builds (`dotnet Bark.dll`) listen on the port set by `ASPNETCORE_URLS` or the launch profile; adjust `proxy_pass` accordingly.

Configure [Forwarded Headers Middleware](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer){target="_blank" rel="noopener"} so the original scheme and host are used. Without `PublicBaseUrl`, absolute URLs in `robots.txt` and `sitemap.xml` derive from the request.

## Running as a service (source builds)

Docker and IIS manage the process lifecycle. For a source build:

```ini
[Unit]
Description=Bark documentation server
After=network.target

[Service]
WorkingDirectory=/srv/bark/publish
ExecStart=/usr/bin/dotnet /srv/bark/publish/Bark.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

Hot reload remains active. Code changes require a service restart; content changes do not.

## Going to production

Set `PublicBaseUrl` to the public origin and `AllowedHosts` to the matching hostname. Disable hot reload in containers where content is baked into the image or mounted read-only.

```yaml
    environment:
      PublicBaseUrl: https://docs.example.com
      Docs__EnableHotReload: "false"
      AllowedHosts: docs.example.com
```

`PublicBaseUrl` is a security setting. Without it, absolute URLs in `robots.txt`, `llms.txt`, the RSS feed and the `canonical`/`og:url` tags derive from the client-controlled `Host` header. A forged `Host` yields a `Sitemap:` line pointing elsewhere, which a CDN can cache and serve. `AllowedHosts` rejects requests for hostnames the site does not serve.

Full list: [Environment variables](/guide/environment-variables/).

## Sizing expectations

All rendered pages and the search index are held in memory. Sites of hundreds of pages are well within limits; tens of thousands of pages exceed the design target.
