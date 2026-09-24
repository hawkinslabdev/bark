namespace Bark.Services.Theming.Themes;

/// <summary>Solarized base tones: warm paper, deep teal night, square corners.</summary>
/// <remarks>Keeps Solarized's hues (cream day, teal night, blue accent) at reduced chroma; text is solved for contrast rather than taken from base00-02, which miss 4.5:1.</remarks>
public sealed class SolarizedTheme : IBarkTheme
{
    public string Name => "solarized";

    public string Label => "Solarized";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(90, 0.013),
        DarkNeutral = new(215, 0.028),
        Accent = new(245, 0.11),
        LightGround = 0.98,
        DarkGround = 0.22,
        DarkAccentTone = 0.72
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    public string ComponentCss => """
                .bark-feature-icon {
                    color: var(--accent);
                }
                .bark-hero-action.brand,
                .bark-hero-action.alt {
                    border-radius: 0;
                }
        """;
}
