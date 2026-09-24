namespace Bark.Services.Theming.Themes;

/// <summary>Sea-mist blue ground, deep petrol accent, feature row set on one tinted band.</summary>
public sealed class OceanTheme : IBarkTheme
{
    public string Name => "ocean";

    public string Label => "Ocean";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(222, 0.024),
        DarkNeutral = new(232, 0.05),
        Accent = new(212, 0.12),
        LightGround = 0.95,
        LightAccentTone = 0.42,
        DarkGround = 0.2,
        DarkAccentTone = 0.78
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-features {
                    padding: 2.5rem;
                    background-color: var(--accent-light);
                    border-radius: 10px;
                }
                .bark-feature {
                    padding-top: 0;
                    border-top: 0;
                }
                .bark-feature-icon {
                    color: var(--accent);
                }
                @media (max-width: 768px) {
                    .bark-features {
                        padding: 1.75rem 1.5rem;
                    }
                }
        """;
}
