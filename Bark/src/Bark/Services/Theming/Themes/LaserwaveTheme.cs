namespace Bark.Services.Theming.Themes;

/// <summary>Synthwave violet, hot magenta accent, cyan counterpart on icons.</summary>
/// <remarks>Full magenta only holds on dark; the contrast solver walks it down to deep berry in light mode.</remarks>
public sealed class LaserwaveTheme : IBarkTheme
{
    public string Name => "laserwave";

    public string Label => "Laserwave";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(310, 0.018),
        DarkNeutral = new(305, 0.035),
        Accent = new(350, 0.2),
        AccentAlt = new(205, 0.13),
        DarkGround = 0.24,
        DarkAccentTone = 0.72
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-feature-icon {
                    color: var(--accent-alt);
                }
                a.bark-feature:hover .bark-feature-icon {
                    color: var(--accent);
                }
                .bark-hero-text {
                    color: var(--accent);
                }
                .bark-hero-name {
                    color: var(--text-color);
                }
        """;
}
