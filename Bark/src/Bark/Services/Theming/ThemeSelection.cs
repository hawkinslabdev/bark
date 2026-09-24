using Bark.Models;

namespace Bark.Services.Theming;

/// <summary>Splits a theme value like <c>"forest dark"</c> into its palette name and a forced light/dark mode,
/// so <c>"dark"</c>/<c>"light"</c> pins the color scheme wherever a theme is named (appsettings, <c>--theme</c>, <c>config.json</c>).</summary>
public static class ThemeSelection
{
    public static (string? Name, ThemeMode Mode) Split(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (null, ThemeMode.Auto);

        string? name = null;
        var mode = ThemeMode.Auto;

        foreach (var token in value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token.ToLowerInvariant())
            {
                case "dark": mode = ThemeMode.Dark; break;
                case "light": mode = ThemeMode.Light; break;
                case "auto": mode = ThemeMode.Auto; break;
                default: name ??= token; break;
            }
        }

        return (name, mode);
    }
}
