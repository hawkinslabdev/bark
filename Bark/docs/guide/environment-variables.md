---
title: Environment Variables
description: Configuring Bark via environment variables for Docker and container deployments
---

# Environment Variables

Every `appsettings.json` setting can be supplied as an environment variable through the ASP.NET Core configuration system. Key reference: [Site Config](/reference/site-config).

## How the mapping works

Double underscores (`__`) in variable names map to the `:` section separator.

For example, `Docs:RootPath` becomes `Docs__RootPath`, and `Docs:Themes:PrimaryColor` becomes `Docs__Themes__PrimaryColor`.

## Public base URL

`PublicBaseUrl` sets the public origin of the site:

```bash
PublicBaseUrl=https://docs.example.com
```

It is used for absolute URLs in `robots.txt`, `llms.txt`, the RSS feed, and the `canonical` and `og:url` tags. When unset, these URLs derive from the request's `Host` header, which the client controls. On a public host, a forged `Host` produces a `Sitemap:` line pointing elsewhere, and a CDN can cache and serve that response.

`AllowedHosts` rejects requests for other hostnames:

```bash
AllowedHosts=docs.example.com
```

`Docs__PublicBaseUrl` is an equivalent key. Static export sets the same value through `--base-url`; the variable applies to server mode only.

## Server URL and port

`ASPNETCORE_URLS` sets the Kestrel listening address:

```bash
ASPNETCORE_URLS=http://+:8080
```

Multiple addresses are separated by semicolons:

```bash
ASPNETCORE_URLS=http://+:8080;https://+:8443
```

## Examples

The same variables apply to any process environment.

**`docker run`:**

```bash
docker run \
  -e Docs__RootPath=/docs \
  -e Docs__BasePath=/my-repo \
  -e PublicBaseUrl=https://docs.example.com \
  -e AllowedHosts=docs.example.com \
  -e ASPNETCORE_URLS=http://+:8080 \
  -v /path/to/your/docs:/docs \
  -p 8080:8080 \
  your-bark-image
```

**`docker-compose.yml`:**

```yaml
services:
  docs:
    build: .
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_URLS: http://+:8080
      Docs__RootPath: /docs
      Docs__BasePath: /my-repo
      PublicBaseUrl: https://docs.example.com
      Docs__EnableHotReload: "false"
      AllowedHosts: docs.example.com
    volumes:
      - ./docs:/docs
```
