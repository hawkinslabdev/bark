using Bark.Services.Theming.Themes;

namespace Bark.Services.Theming;

/// <summary>Built-in themes. To add your own, implement <see cref="IBarkTheme"/> and add a line to <see cref="All"/>.</summary>
public static class ThemeRegistry
{
    public static IReadOnlyList<IBarkTheme> All { get; } =
    [
        new DefaultTheme(),
        new ForestTheme(),
        new SignalTheme(),
        new BlueprintTheme(),
        new OceanTheme(),
        new SpaceTheme(),
        new SolarizedTheme(),
        new LaserwaveTheme(),
        new LimelightTheme()
    ];

    public static IBarkTheme Default { get; } = All[0];

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["signal-dark"] = "signal",
        ["blueprint-grid"] = "blueprint",
        ["forest-ledger"] = "forest",
        ["deep-space"] = "space"
    };

    /// <summary>Unknown names warn and fall back to the default; a typo must never take a site down.</summary>
    public static IBarkTheme Resolve(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Default;

        var trimmed = name.Trim();
        if (Aliases.TryGetValue(trimmed, out var renamed))
            trimmed = renamed;

        foreach (var theme in All)
        {
            if (string.Equals(theme.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                return theme;
        }

        Serilog.Log.Warning(
            "Unknown theme {Theme}; falling back to {Default}. Available: {Available}",
            trimmed, Default.Name, string.Join(", ", All.Select(t => t.Name)));
        return Default;
    }
}
