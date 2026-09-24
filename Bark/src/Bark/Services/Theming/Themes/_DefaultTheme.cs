namespace Bark.Services.Theming.Themes;

/// <summary>White ground, green accent, flat outline icons. Used when none is configured.</summary>
public sealed class DefaultTheme : IBarkTheme
{
    public string Name => "default";

    public string Label => "Default";

    private static readonly ThemePalette Palette = new()
    {
        Neutral = new(150, 0.005),
        Accent = new(160, 0.12),
        DarkGround = 0.17
    };

    public IReadOnlyDictionary<string, string> LightTokens { get; } = Palette.Light();

    public IReadOnlyDictionary<string, string> DarkTokens { get; } = Palette.Dark();

    /// <summary>Empty: the base stylesheet is this theme.</summary>
    public string ComponentCss => string.Empty;
}
