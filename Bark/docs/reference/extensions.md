---
title: Extensions
description: Enabling privacy-friendly analytics like Matomo, Plausible, Medama, and GoatCounter through docs/extensions.json
---

# Extensions

Extensions are built-in integrations configured in `extensions.json`. Bark injects the script, updates the Content Security Policy, and applies changes through hot reload. Available extensions are privacy-focused analytics providers.

## What's Supported

| Extension | Type | Required keys |
| --- | --- | --- |
| [Matomo](#matomo) | Analytics, self-hosted | `url`, `siteId` |
| [Plausible](#plausible) | Analytics, hosted or self-hosted | `domain` |
| [Medama](#medama) | Analytics, self-hosted | `url` |
| [GoatCounter](#goatcounter) | Analytics, hosted or self-hosted | `url` |
| [Liwan](#liwan) | Analytics, self-hosted | `url`, `entity` |

## How Extensions Work

Extensions are configured in the optional `docs/extensions.json`, next to `config.json`.

```json
{
  "extensions": {
    "plausible": {
      "enabled": true,
      "domain": "docs.example.com"
    }
  }
}
```

Extensions are disabled unless `enabled` is `true`. An empty or absent file disables all extensions. Changes apply through hot reload.

Each enabled extension is validated. An invalid configuration leaves the extension inactive and logs a warning at startup.

::: info
The Content Security Policy is extended with the configured origin, and each injected script carries the page nonce. A strict CSP remains compatible.
:::

## Available Extensions

Multiple extensions can run simultaneously.

### Matomo

[Matomo](https://matomo.org): self-hosted analytics. Cookieless mode is enabled by default.

```json
{
  "extensions": {
    "matomo": {
      "enabled": true,
      "url": "https://analytics.example.com",
      "siteId": "1",
      "disableCookies": true
    }
  }
}
```

| Field | Description |
|---|---|
| `enabled` | `true` activates the extension. |
| `url` | Matomo base URL. |
| `siteId` | Numeric Matomo site ID. `site_id` is also accepted. |
| `disableCookies` | Cookieless tracking. Default: `true`. |

### Plausible

[Plausible](https://plausible.io): cookie-free analytics, hosted or self-hosted.

```json
{
  "extensions": {
    "plausible": {
      "enabled": true,
      "domain": "docs.example.com",
      "url": "https://plausible.io",
      "script": "script.js"
    }
  }
}
```

| Field | Description |
|---|---|
| `enabled` | `true` activates the extension. |
| `domain` | Site domain registered in Plausible. Comma-separated for a shared script. |
| `url` | Base URL. Default: `https://plausible.io`; required only when self-hosting. |
| `script` | Script variant under `/js/`, such as `script.outbound-links.js`. Default: `script.js`. |

### Medama

[Medama](https://github.com/medama-io/medama): self-hosted analytics.

```json
{
  "extensions": {
    "medama": {
      "enabled": true,
      "url": "https://medama.example.com"
    }
  }
}
```

| Field | Description |
|---|---|
| `enabled` | `true` activates the extension. |
| `url` | Medama base URL. |

### GoatCounter

[GoatCounter](https://www.goatcounter.com): analytics, free hosted or self-hosted.

```json
{
  "extensions": {
    "goatcounter": {
      "enabled": true,
      "url": "https://you.goatcounter.com"
    }
  }
}
```

| Field | Description |
|---|---|
| `enabled` | `true` activates the extension. |
| `url` | GoatCounter site base URL. |

### Liwan

[Liwan](https://liwan.dev): self-hosted analytics with single-file storage.

```json
{
  "extensions": {
    "liwan": {
      "enabled": true,
      "url": "https://liwan.example.com",
      "entity": "my-website"
    }
  }
}
```

| Field | Description |
|---|---|
| `enabled` | `true` activates the extension. |
| `url` | Liwan base URL. |
| `entity` | Liwan entity ID for this site. |

## A Complete Example

Template with all five providers disabled:

```json
{
  "extensions": {
    "matomo": {
      "enabled": false,
      "url": "https://analytics.example.com",
      "siteId": "1",
      "disableCookies": true
    },
    "plausible": {
      "enabled": false,
      "domain": "example.com",
      "url": "https://plausible.io",
      "script": "script.js"
    },
    "medama": {
      "enabled": false,
      "url": "https://medama.example.com"
    },
    "goatcounter": {
      "enabled": false,
      "url": "https://you.goatcounter.com"
    },
    "liwan": {
      "enabled": false,
      "url": "https://liwan.example.com",
      "entity": "my-website"
    }
  }
}
```

An enabled extension injects its script into `<head>` with the page nonce. Related: [Site Config](/reference/site-config).
