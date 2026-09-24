namespace Bark.Services.Theming.Themes;

/// <summary>Cyanotype: cobalt on white by day, pale lines on blueprint navy at night. Features ruled into one square-cornered drawing grid.</summary>
public sealed class BlueprintTheme : IBarkTheme
{
    public string Name => "blueprint";

    public string Label => "Blueprint";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(250, 0.01),
        DarkNeutral = new(258, 0.075),
        Accent = new(245, 0.17),
        LightGround = 0.99,
        DarkGround = 0.27,
        LightAccentTone = 0.48,
        DarkAccentTone = 0.86
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-features {
                    gap: 1px;
                    background-color: var(--border);
                    border: 1px solid var(--border);
                }
                .bark-feature {
                    padding: 1.75rem;
                    border-top: 0;
                    background-color: var(--bg-color);
                    transition: background-color 0.15s ease;
                }
                a.bark-feature:hover {
                    background-color: var(--accent-light);
                }
                .bark-feature-icon {
                    color: var(--accent);
                }
                .bark-hero-action.brand,
                .bark-hero-action.alt {
                    border-radius: 0;
                }
        """;
}
