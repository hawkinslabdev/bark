namespace Bark.Services.Theming.Themes;

/// <summary>Pale off-white ground, electric lime accent with a cyan counterpart, squared corners.</summary>
public sealed class LimelightTheme : IBarkTheme
{
    public string Name => "limelight";

    public string Label => "Limelight";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(120, 0.008),
        Accent = new(128, 0.2),
        AccentAlt = new(215, 0.13),
        LightAccentTone = 0.55,
        DarkGround = 0.17,
        DarkAccentTone = 0.86
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-features {
                    gap: 0;
                }
                .bark-feature {
                    padding: 0.25rem 1.5rem 1.75rem;
                    border-top: 0;
                    border-left: 1px solid var(--border);
                }
                .bark-feature:first-child {
                    border-left: 0;
                    padding-left: 0;
                }
                a.bark-feature:hover {
                    background-color: var(--accent-light);
                }
                .bark-feature-icon {
                    color: var(--accent-alt);
                }
                #scroll-indicator {
                    background-color: var(--accent-alt);
                }
                .bark-hero-action.brand,
                .bark-hero-action.alt {
                    border-radius: 0;
                }
                @media (max-width: 768px) {
                    .bark-feature {
                        padding: 1.5rem 0;
                        border-left: 0;
                        border-top: 1px solid var(--border);
                    }
                }
        """;
}
