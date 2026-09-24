namespace Bark.Services.Theming.Themes;

/// <summary>Warm paper, moss green, features set as ledger rows: title left, entry right.</summary>
public sealed class ForestTheme : IBarkTheme
{
    public string Name => "forest";

    public string Label => "Forest";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(80, 0.016),
        Accent = new(140, 0.075),
        LightGround = 0.975,
        DarkGround = 0.2,
        LightAccentTone = 0.44,
        DarkAccentTone = 0.74
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-features {
                    display: block;
                }
                .bark-feature {
                    display: grid;
                    grid-template-columns: minmax(10rem, 16rem) 1fr;
                    gap: 0.5rem 2.5rem;
                    padding: 1.5rem 0;
                    border-top: 1px solid var(--border);
                }
                .bark-feature:last-child {
                    border-bottom: 1px solid var(--border);
                }
                .bark-feature-icon {
                    display: none;
                }
                .bark-feature-title {
                    grid-column: 1;
                    font-size: 1.05rem;
                    color: var(--accent);
                }
                .bark-feature-details {
                    grid-column: 2;
                    grid-row: 1;
                    margin-top: 0;
                    font-size: 0.95rem;
                }
                @media (max-width: 640px) {
                    .bark-feature {
                        grid-template-columns: 1fr;
                    }
                    .bark-feature-details {
                        grid-column: 1;
                        grid-row: 2;
                    }
                }
        """;
}
