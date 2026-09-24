---
title: Markdown
description: Every Markdown extension Bark supports, with live examples
---

# Using Markdown

Pages are written in [Markdown](https://www.markdownguide.org/guide/). Every example on this page renders live.

## Syntax-highlighted code blocks

Highlighting uses grammar-based tokenization[^1] for about 65 bundled languages:

```csharp
public sealed record Order(string Id, decimal Total)
{
    public bool IsLarge => Total > 1000m;
}
```

```python
def fibonacci(n: int) -> int:
    if n < 2:
        return n
    return fibonacci(n - 1) + fibonacci(n - 2)
```

```java
public class Greeter {
    public String greet(String name) {
        return "Hello, " + name + "!";
    }
}
```

```rust
fn main() {
    let nums = vec![1, 2, 3];
    println!("{:?}", nums.iter().sum::<i32>());
}
```

```sql
SELECT id, title FROM pages WHERE published = true ORDER BY title;
```

Aliases resolve to full grammar IDs: `c#`, `cs` and `dotnet` for C#, `f#` for F#, `c++` for C++. Unknown languages render without highlighting.

## Title bars

`[filename]` in the fence info string renders a title bar instead of the language badge:

````md
```json [src/appsettings.json]
{
    "Hello": "world"
}
```
````

Renders as:

```json [src/appsettings.json]
{
    "Hello": "world"
}
```

## Line highlighting

`{n,m-o}` in the fence info string highlights lines:

```ts{2}
function add(a: number, b: number) {
  return a + b; // this line is highlighted
}
```

A trailing `// [!code highlight]` comment also highlights a line; the comment is removed from output:

```ts
const cache = new Map();
const ttlMs = 60_000; // [!code highlight]
```

## Diff notation

```ts
const port = 3000; // [!code --]
const port = process.env.PORT ?? 3000; // [!code ++]
```

## Focus

```ts
function setup() {
  loadConfig(); // [!code focus]
  startServer();
}
```

## Line numbers

```ts:line-numbers
const a = 1;
const b = 2;
const c = a + b;
```

## Code groups

This example:

````md
::: code-group
```sh [npm]
npm install
```
```sh [pnpm]
pnpm install
```
```sh [c# icon:sharp]
dotnet restore
```
:::
````

Renders as:

::: code-group
```sh [npm]
npm install
```
```sh [pnpm]
pnpm install
```
```sh [c# icon:sharp]
dotnet restore
```
:::

`[label]` sets the tab title. Labels matching a [Simple Icons](https://simpleicons.org/) name display that icon. `[label icon:slug]` sets the icon explicitly, for example `[csharp icon:dotnet]`.

## Alerts

GitHub-style alerts use `> [!TYPE]`:

> [!NOTE]
> Important context.

> [!TIP]
> Helpful advice.

> [!IMPORTANT]
> Required for success.

> [!WARNING]
> Immediate attention needed.

> [!CAUTION]
> Potential negative risks.

## Custom containers

Blocks delimited by `:::` with a type: `note`, `tip`, `info`, `warning`, `danger` or `details`. `details` takes an optional summary text and renders as a disclosure.

::: note
This is a note container.
:::

::: tip
This is a tip container.
:::

::: info
This is an info container.
:::

::: warning
This is a warning container.
:::

::: danger
This is a danger container.
:::

::: details Click to expand
Hidden content goes here.
:::

Containers render as `<div class="TYPE custom-block">`. Raw HTML passes through, so the `div` can be written directly, for example to add an inline style:

```md
<div class="tip custom-block">

Quick setup: [Getting Started](/guide/getting-started).

</div>
```

Renders as:

<div class="tip custom-block">

Quick setup: [Getting Started](/guide/getting-started).

</div>

A blank line is required after the opening `<div>` and before the closing `</div>`; otherwise the content is treated as raw HTML and Markdown inside is not rendered.

## Badges

Inline labels written as HTML; `<badge>` is styled by the Bark stylesheet. See [Badge](/reference/default-theme-badge).

```md
Hello world <Badge type="tip">3.0+</Badge>
```

Renders as:

Hello world <Badge type="tip">3.0+</Badge>

Types match the alert colors: `info` (blue), `tip` (green, default), `warning` (amber), `danger` (red).

<Badge type="info">info</Badge>
<Badge type="tip">tip</Badge>
<Badge type="warning">warning</Badge>
<Badge type="danger">danger</Badge>

::: danger
The closing tag is required: `<Badge type="tip">text</Badge>`. HTML ignores `/>` on unknown elements, so `<Badge text="x" />` stays open and absorbs the rest of the paragraph.
:::

## Definition lists 

Terms followed by lines starting with `:` and a space produce a definition list.

Term 1
:   Definition of term 1.

Term 2
:   Definition of term 2.
:   Another definition of term 2.

## Math

Inline: $E = mc^2$

Block:

$$
\sum_{i=1}^{n} i = \frac{n(n+1)}{2}
$$

## Abbreviations

Abbreviation definitions at the end of a document wrap matching terms in `<abbr>`:

````md
The spec is written by the WHATWG and served here over HTTP.

*[WHATWG]: Web Hypertext Application Technology Working Group
*[HTTP]: HyperText Transfer Protocol
````

Renders as:

The spec is written by the WHATWG and served here over HTTP.

*[HTML]: HyperText Markup Language
*[WHATWG]: Web Hypertext Application Technology Working Group
*[HTTP]: HyperText Transfer Protocol

A definition applies to every occurrence on the page. Terms render with a dotted underline; the expansion appears on hover, and on tap on touch devices.

## Citations

Text wrapped in doubled quotes (`""`) renders as a `<cite>` element:

""The Art of Computer Programming""

## Standard Markdown

Tables, task lists and footnotes are supported:

| Feature | Supported |
|---|---|
| Tables | ✅ |
| Task lists | ✅ |
| Footnotes | ✅ |

- [x] Done
- [ ] Not done yet

A sentence with a footnote.[^2]

[^1]: [TextMateSharp](https://github.com/danipen/TextMateSharp){target="_blank" rel="noopener"}.
[^2]: The footnote text.

## Link attributes

`{target="_blank" rel="noopener"}` after a link sets its attributes:

```md
[Bark on GitHub](https://github.com/org/bark){target="_blank" rel="noopener"}
```

## Video and media

Image syntax pointing at a video, audio or embed URL renders a native player. Media files are stored in `docs/assets/`:

```md
![Demo clip](/assets/video.mp4)
```

![Demo clip](/assets/video.mp4)
