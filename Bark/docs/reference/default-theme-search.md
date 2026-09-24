---
title: Search
description: Bark's built-in search, no third-party service required
---

# Search

The header search queries `/api/search`, backed by an in-memory inverted index. The index rebuilds automatically when the configuration changes.

## How it ranks results

| Match location | Weight |
| --- | --- |
| Title | 10 |
| Description | 5 |
| Heading | 3 |
| Body text | 1 |

Ranking is the sum of weighted term matches. A title match outranks a body-text match.

## Calling it directly

```bash
curl "http://localhost:5000/api/search?q=hot+reload"
```

Queries shorter than 2 characters return an empty array. Response shape: [API Reference](../api-reference).

## What's not here

Not included: AI answers, analytics, fuzzy or typo-tolerant matching. The in-memory index targets sites of up to a few hundred pages. The search box is plain HTML and can be replaced from source or with custom JavaScript.
