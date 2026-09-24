---
title: Last Updated Timestamp
description: Showing when a page was last changed
---

# Last Updated Timestamp

A "Last updated" stamp is rendered at the bottom of each page. The date comes from the file's last-write time, or from frontmatter when set.

## Enabling

```json
{
  "lastUpdated": true
}
```

Set in `docs/config.json`. Default: `false`.

## Per-page override

`lastUpdated: false` in frontmatter hides the stamp on that page, overriding the site-wide setting. Typical use: evergreen pages such as a glossary or license.

```yaml
---
lastUpdated: false
---
```

## Pinning a date

Copying, cloning, moving or renaming files resets their last-write time. The `date` and `updated` frontmatter fields record the content date independently of the filesystem.

```yaml
---
date: 2025-03-01
updated: 2025-06-28
---
```

Precedence: `updated`, then `date`, then the file's last-write time. Convention: set `date` on first publish and `updated` on significant changes.

Field reference: [Frontmatter Config](/reference/frontmatter-config#dates).
