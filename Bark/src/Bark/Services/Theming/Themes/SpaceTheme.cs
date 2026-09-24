namespace Bark.Services.Theming.Themes;

/// <summary>Near-black navy, periwinkle accent, unruled features with room around them. Darkest built-in.</summary>
public sealed class SpaceTheme : IBarkTheme
{
    public string Name => "space";

    public string Label => "Space";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(278, 0.018),
        DarkNeutral = new(275, 0.035),
        Accent = new(275, 0.15),
        DarkGround = 0.13,
        LightAccentTone = 0.45,
        DarkAccentTone = 0.74
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-features {
                    gap: 3rem 4rem;
                    margin-top: 1rem;
                }
                .bark-feature {
                    border-top: 0;
                    padding-top: 0;
                }
                .bark-feature-icon {
                    color: var(--accent);
                }
                .bark-feature-title {
                    font-size: 1.1rem;
                }
        """;
}
