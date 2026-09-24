---
title: Diagrams
description: Mermaid diagrams, rendered straight from a fenced code block
---

# Using Diagrams

Fenced code blocks with the `mermaid` language identifier render as diagrams.

No plugins or build steps are required. `mermaid.js` loads once per page and replaces each block with an SVG after page load.

## Flowchart

````md
```mermaid
flowchart LR
    A[Request hits Bark] --> B{Page in cache?}
    B -->|Yes| C[Serve cached HTML]
    B -->|No| D[404]
    C --> E[Compute ETag]
    E --> F[Return response]
```
````

```mermaid
flowchart LR
    A[Request hits Bark] --> B{Page in cache?}
    B -->|Yes| C[Serve cached HTML]
    B -->|No| D[404]
    C --> E[Compute ETag]
    E --> F[Return response]
```

## Sequence diagram

```mermaid
sequenceDiagram
    participant Browser
    participant Bark
    participant Watcher as FileSystemWatcher
    Browser->>Bark: GET /guide/diagrams
    Bark->>Browser: 200 OK (cached HTML)
    Note over Watcher: docs/diagrams.md saved
    Watcher->>Bark: change event (debounced 300ms)
    Bark->>Bark: rebuild page cache
    Browser->>Bark: poll /api/build-version
    Bark->>Browser: new version
    Browser->>Browser: location.reload()
```

## Class diagram

Data model example:

```mermaid
classDiagram
    class DocumentationPage {
        +string Path
        +string Title
        +string HtmlContent
        +DateTime LastModified
        +string Layout
    }
    class NavEntry {
        +string Title
        +string Path
        +bool Collapsed
        +NavEntry[] Items
    }
    DocumentationPage "1" --> "0..*" NavEntry : appears in
```

## State diagram

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> Rebuilding: file changed
    Rebuilding --> Idle: content unchanged (hash match)
    Rebuilding --> Reloading: content changed
    Reloading --> Idle: browser reloaded
```

## Pie chart

```mermaid
pie title Search ranking weight
    "Title" : 10
    "Description" : 5
    "Heading" : 3
    "Body text" : 1
```

Diagrams render client-side. They are absent from the HTML source, from text-only crawlers, and from `llms.txt`. Content essential to understanding must also appear in the surrounding text, which also provides a text alternative for assistive technology.

::: note
Diagram colors come from the active theme palette. Mermaid bakes colors into the SVG instead of using CSS variables, so toggling dark mode on a page with diagrams triggers a full reload.
:::
