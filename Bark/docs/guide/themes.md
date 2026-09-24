---
title: Themes
description: Override colors, add your own CSS and JS, or hand the whole theme folder to a designer
---

# Themes

Bark includes nine built-in themes and four levels of customization:

1. **Built-in theme**: one value in `config.json`.
2. **CSS variables**: palette and font overrides.
3. **`custom.css` / `custom.js`**: arbitrary styling and scripting.
4. **Theme class**: a theme shipped as code.

Brand colors and dark-mode settings: [CSS variables](#css-variables).

## Picking a theme

`theme` in `docs/config.json` selects the theme:

```json
{
  "theme": "forest-ledger"
}
```

Default: `default`. Changes apply through hot reload without a restart.

A ` dark` or ` light` suffix pins the color scheme, for example `"theme": "forest-ledger dark"`. A pinned scheme removes the theme toggle and ignores the OS preference. Without a suffix, the toggle is shown and the OS preference applies.

| Name | Look |
|---|---|
| `default` | Off-white ground, centred hero under a green kicker, deep green accent, flat monochrome outline icons under a hairline rule. |
| `forest-ledger` | Warm paper tones and forest green, with features set as numbered rule-lines. |
| `signal-dark` | Deep charcoal and an amber accent, with a divided feature row. |
| `blueprint-grid` | Mint tints, features drawn as one bordered grid with tinted icon chips. |
| `ocean` | Cool blue-grey paper and a deep harbour accent, with the feature row on a tinted band. |
| `deep-space` | Near-black navy and a periwinkle accent. The darkest built-in, with a soft-cornered feature grid. |
| `solarized` | Solarized base tones: warm paper by day, deep teal by night, square corners throughout. |
| `laserwave` | Synthwave violet with a hot magenta accent and a 2px accent rule above each feature. |
| `limelight` | Pale off-white with a sage-lime accent and a cyan counterpart. Square corners and a gradient rule under the hero. |

Every theme defines light **and** dark palettes, including `signal-dark`, `laserwave` and `deep-space`.

An unrecognized name logs a warning and falls back to `default`.

Alternative sources:

| Source | Wins over | Use it for |
|---|---|---|
| `--theme <name>` CLI flag | everything | previewing a theme, or exporting a themed static site |
| `Docs:Themes:Name` in `appsettings.json` | `config.json` | pinning a theme per deployment |
| `theme` in `docs/config.json` | nothing | the normal case |

```bash
dotnet run --project src/Bark -- --theme signal-dark
dotnet run --project src/Bark -- --export ./out --theme blueprint-grid
```

### What a theme controls

Colors, fonts, icon treatment, card treatment and hero styling.

Themes do not affect navigation, content, URLs or front matter.

## The theme folder

File locations:

| File | Effect |
|---|---|
| `wwwroot/theme/custom.css` | Loaded last, after every built-in style. Plain selectors win without `!important`. |
| `wwwroot/theme/custom.js` | Loaded with `defer` on every page. |
| `wwwroot/theme/theme.json` | CSS variable overrides and toggles, as plain JSON. See [CSS variables](#css-variables) for the field list. |

> [!IMPORTANT]
> New files in `wwwroot/theme/` need an application restart to take effect.

## CSS variables

Colors and fonts are CSS variables. The active theme defines all of them; overrides replace individual values.

Defaults (`default` theme, light mode):

| Variable | Default (light) | Controls |
|---|---|---|
| `--primary-color` | `#1f6b4a` | Links, highlights, the active nav indicator. Also sets `--accent`. |
| `--bg-color` | `#fafafa` | Page background. |
| `--sidebar-bg` | `#f2f4f2` | Sidebar background. |
| `--text-color` | `#1a1d1f` | Primary text. |
| `--text-muted` | `#565f59` | Descriptions, timestamps, muted labels. |
| `--border` | `#e2e7e2` | Hairline borders throughout the layout. |
| `--code-bg` | `#f4f6f4` | Inline code and fenced code blocks. |
| `--search-bg` | `#f4f6f4` | The boxed search field in the header. Falls back to `--sidebar-bg` when a theme leaves it unset. |
| `--accent-light` | `#eef1ee` | Accent-tinted surface: active nav rows, hover fills. Has no text-contrast duty, so a theme can put a saturated color here. |
| `--promo-bg` / `--promo-text` | `--accent-light` / `--accent` | The announcement bar above the header. Aliases by default, so they follow your accent unless you set them. |
| `--font-sans` | system stack | Body font. |
| `--font-mono` | system stack | Code font. |

> [!IMPORTANT]
> Overrides apply to **both** light and dark mode. For a single mode, use `custom.css` with a `:root[data-theme="dark"]` selector, or a custom theme.

Set overrides in `theme.json` or in `Docs:Themes` in `appsettings.json`. Field names are the variable names in PascalCase:

::: code-group

```json [wwwroot/theme/theme.json]
{
  "primaryColor": "#7c3aed",
  "fontSans": "'Inter', system-ui, sans-serif",
  "darkMode": true
}
```

```json [appsettings.json]
{
  "Docs": {
    "Themes": {
      "PrimaryColor": "#7c3aed",
      "FontSans": "'Inter', system-ui, sans-serif",
      "DarkMode": true
    }
  }
}
```

:::

> [!IMPORTANT]
> When `Docs:Themes` exists in `appsettings.json`, `theme.json` is ignored. The sources are not merged.

Additional fields:

| Field | Type | Default | Effect |
|---|---|---|---|
| `DarkMode` | `bool` | `true` | Toggles the `prefers-color-scheme: dark` variant and the in-page dark mode switch. |
| `ShowScrollIndicator` | `bool` | `true` | The thin progress bar pinned to the top of the viewport while you scroll. |

## Escape hatches

`custom.css` and `custom.js` cover changes beyond palette and fonts.

`wwwroot/theme/custom.css`:

```css
/* Bark's CSS variables don't expose border-radius, so override the rule directly. */
.search-trigger {
  border-radius: 999px;
}
```

`wwwroot/theme/custom.js`:

```js
document.addEventListener('DOMContentLoaded', () => {
  console.log('Custom theme JS loaded.');
});
```

`custom.css` loads after the built-in stylesheet; equal-specificity selectors override it. `custom.js` loads with `defer`, before the built-in script initializes search, the sidebar and dark mode. Code that depends on them should run on `DOMContentLoaded`.

> [!TIP]
> `CustomCssUrl` in `Docs:Themes` or `theme.json` loads a stylesheet from another URL. It takes priority over `custom.css`.

## Writing your own theme

A theme is a class implementing `IBarkTheme` in `src/Bark/Services/Theming/Themes/`:

```csharp
namespace Bark.Services.Theming.Themes;

public sealed class MidnightTheme : IBarkTheme
{
    public string Name => "midnight";

    public string Label => "Midnight";

    public IReadOnlyDictionary<string, string> LightTokens { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["--bg-color"] = "#ffffff",
        ["--sidebar-bg"] = "#f7f8fb",
        ["--text-color"] = "#12151f",
        ["--text-muted"] = "#5a6072",
        ["--accent"] = "#3a4bb8",
        ["--accent-light"] = "#eceefa",
        ["--border"] = "#dfe2ec",
        ["--code-bg"] = "#f3f5fa"
    };

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["--bg-color"] = "#0c0e16",
        ["--sidebar-bg"] = "#12141f",
        ["--text-color"] = "#e6e8f2",
        ["--text-muted"] = "#969cb4",
        ["--accent"] = "#8b98f0",
        ["--accent-light"] = "#181c2e",
        ["--border"] = "#232739",
        ["--code-bg"] = "#12151f"
    };

    public string ComponentCss => """
                .bark-feature {
                    border-radius: 0;
                }
        """;
}
```

Register it in `ThemeRegistry.All`:

```csharp
public static IReadOnlyList<IBarkTheme> All { get; } =
[
    new DefaultTheme(),
    new ForestLedgerTheme(),
    new SignalDarkTheme(),
    new BlueprintGridTheme(),
    new OceanTheme(),
    new DeepSpaceTheme(),
    new SolarizedTheme(),
    new LaserwaveTheme(),
    new MidnightTheme()
];
```

`"theme": "midnight"` then selects it.

Rules enforced by the test suite:

- **Both modes.** Every literal color in `LightTokens` requires a counterpart in `DarkTokens`.
- **The eight palette keys are required.** `--bg-color`, `--sidebar-bg`, `--text-color`, `--text-muted`, `--accent`, `--accent-light`, `--border`, `--code-bg`. Other values (alert hues, fonts, shadows, aliases) default from `ThemeDefaults`.
- **Contrast.** Text, muted text and accent require at least 4.5:1 against the background in both modes (WCAG 1.4.3). `dotnet test --filter ThemeContrastTests` reports measured ratios.

`ComponentCss` is appended after the built-in stylesheet in the same `<style>` element; `!important` is not required.

## Limitations

Themes change presentation only: header structure, sidebar behavior and per-page layouts are fixed.

Structural changes require modifying Bark source. There is no plugin system by design.