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
  "theme": "forest"
}
```

Default: `default`. Changes apply through hot reload without a restart.

A ` dark` or ` light` suffix pins the color scheme, for example `"theme": "forest dark"`. A pinned scheme removes the theme toggle and ignores the OS preference. Without a suffix, the toggle is shown and the OS preference applies.

| Name | Look |
|---|---|
| `default` | Off-white ground and deep green accent. Two-line hero headline, outline icons beside each feature title under a hairline rule. |
| `forest` | Warm paper and moss green. Features set as ledger rows: title on the left, entry on the right. |
| `signal` | Deep charcoal and an amber accent, with a divided feature row. |
| `blueprint` | Cyanotype: cobalt on white, pale lines on blueprint navy in dark mode. Features ruled into one square-cornered grid. |
| `ocean` | Sea-mist blue ground and a deep petrol accent; deep sea-blue in dark mode. Feature row on a tinted band. |
| `space` | Near-black navy and a periwinkle accent. The darkest built-in; features unruled with wide spacing. |
| `solarized` | Solarized hues at reduced saturation: cream by day, teal by night, square buttons. |
| `laserwave` | Synthwave violet with a hot magenta accent; feature icons in the cyan counterpart. |
| `limelight` | Pale off-white with an electric lime accent and a cyan counterpart. Square corners; features split by vertical hairlines. |

Every theme defines light **and** dark palettes, including `signal`, `laserwave` and `space`.

Built-in palettes are generated in OKLCH from a few seeds per theme (see [Writing your own theme](#writing-your-own-theme)). Surfaces step by the same perceptual amount in every theme; text, accent and alert colors are solved against the surfaces they sit on to clear WCAG AA in both modes.

An unrecognized name logs a warning and falls back to `default`.

Former names still resolve: `forest-ledger` (`forest`), `signal-dark` (`signal`), `blueprint-grid` (`blueprint`), `deep-space` (`space`).

Alternative sources:

| Source | Wins over | Use it for |
|---|---|---|
| `--theme <name>` CLI flag | everything | previewing a theme, or exporting a themed static site |
| `Docs:Themes:Name` in `appsettings.json` | `config.json` | pinning a theme per deployment |
| `theme` in `docs/config.json` | nothing | the normal case |

```bash
dotnet run --project src/Bark -- --theme signal
dotnet run --project src/Bark -- --export ./out --theme blueprint
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
| `--primary-color` | `oklch(0.5 0.113 160)` | Links, highlights, the active nav indicator. Also sets `--accent`. |
| `--bg-color` | `oklch(0.985 0.005 150)` | Page background. |
| `--sidebar-bg` | `oklch(0.963 0.006 150)` | Sidebar background. |
| `--text-color` | `oklch(0.2 0.005 150)` | Primary text. |
| `--text-muted` | `oklch(0.5 0.01 150)` | Descriptions, timestamps, muted labels. |
| `--border` | `oklch(0.9 0.007 150)` | Hairline borders throughout the layout. |
| `--code-bg` | `oklch(0.969 0.006 150)` | Inline code and fenced code blocks. |
| `--search-bg` | `oklch(0.969 0.006 150)` | The boxed search field in the header. Falls back to `--sidebar-bg` when a theme leaves it unset. |
| `--accent-light` | `oklch(0.94 0.036 160)` | Accent-tinted surface: active nav rows, hover fills. Carries accent-colored text on active nav rows. |
| `--selection` | `oklch(0.875 0.054 160)` | Text selection background. Falls back to `--accent-light` when a theme leaves it unset. |
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

A theme is a class implementing `IBarkTheme` in `src/Bark/Services/Theming/Themes/`. A `ThemePalette` derives both modes from seed hues:

```csharp
namespace Bark.Services.Theming.Themes;

public sealed class MidnightTheme : IBarkTheme
{
    public string Name => "midnight";

    public string Label => "Midnight";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(265, 0.012),
        Accent = new(270, 0.16),
        DarkGround = 0.15
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-feature {
                    border-radius: 0;
                }
        """;
}
```

`ThemePalette` seeds:

| Seed | Default | Effect |
|---|---|---|
| `Neutral` | required | Hue and chroma of grounds, borders and text. `0.005` is near-grey, `0.03` a visible tint. |
| `DarkNeutral` | `Neutral` | Separate dark-mode neutral, e.g. `solarized`'s cream-to-teal swap. |
| `Accent` | required | Accent hue and chroma. Also tints `--accent-light` and `--selection`. |
| `AccentAlt` | none | Second hue, emitted as `--accent-alt`. |
| `LightGround` / `DarkGround` | `0.985` / `0.18` | Page lightness per mode. |
| `LightAccentTone` / `DarkAccentTone` | `0.5` / `0.76` | Preferred accent lightness. The solver moves it only as far as contrast requires. |

Chroma beyond the sRGB gamut is reduced at the same lightness and hue. Alert colors keep their hue families (note blue, tip green, important violet, warning amber, caution red) with lightness solved per theme.

`LightTokens` and `DarkTokens` also accept literal dictionaries of hex or `oklch()` values in place of a palette.

Register it in `ThemeRegistry.All`:

```csharp
public static IReadOnlyList<IBarkTheme> All { get; } =
[
    new DefaultTheme(),
    new ForestTheme(),
    new SignalTheme(),
    new BlueprintTheme(),
    new OceanTheme(),
    new SpaceTheme(),
    new SolarizedTheme(),
    new LaserwaveTheme(),
    new MidnightTheme()
];
```

`"theme": "midnight"` then selects it.

Rules enforced by the test suite:

- **Both modes.** Every literal color in `LightTokens` requires a counterpart in `DarkTokens`.
- **The eight palette keys are required.** `--bg-color`, `--sidebar-bg`, `--text-color`, `--text-muted`, `--accent`, `--accent-light`, `--border`, `--code-bg`. Other values (alert hues, fonts, shadows, aliases) default from `ThemeDefaults`.
- **Contrast.** Text, muted text, accent and alert colors require at least 4.5:1 against the background in both modes (WCAG 1.4.3); accent also against `--accent-light`. `dotnet test --filter ThemeContrastTests` reports measured ratios.
- **Gamut.** Generated `oklch()` values stay inside sRGB.

`ComponentCss` is appended after the built-in stylesheet in the same `<style>` element; `!important` is not required.

## Limitations

Themes change presentation only: header structure, sidebar behavior and per-page layouts are fixed.

Structural changes require modifying Bark source. There is no plugin system by design.