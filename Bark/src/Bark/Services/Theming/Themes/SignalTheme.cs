namespace Bark.Services.Theming.Themes;

/// <summary>Deep charcoal, amber accent, divided feature row.</summary>
/// <remarks>Amber is a highlighter on paper; the contrast solver walks it down to bronze in light mode.</remarks>
public sealed class SignalTheme : IBarkTheme
{
    public string Name => "signal";

    public string Label => "Signal";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(95, 0.008),
        Accent = new(80, 0.16),
        DarkGround = 0.16,
        DarkAccentTone = 0.84
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-features {
                    gap: 0;
                }
                .bark-feature {
                    padding: 0 1.75rem;
                    border-top: 0;
                    border-left: 1px solid var(--border);
                }
                .bark-feature:first-child {
                    border-left: 0;
                    padding-left: 0;
                }
                .bark-feature-icon {
                    color: var(--accent);
                }
                @media (max-width: 768px) {
                    .bark-features {
                        gap: 2rem;
                    }
                    .bark-feature {
                        padding: 0;
                        border-left: 0;
                    }
                }
        """;
}
