namespace Bark.Services.Theming;

public readonly record struct Tint(double Hue, double Chroma);

public sealed record ThemePalette
{
    public required Tint Neutral { get; init; }

    public Tint? DarkNeutral { get; init; }

    public required Tint Accent { get; init; }

    public Tint? AccentAlt { get; init; }

    public double LightGround { get; init; } = 0.985;

    public double DarkGround { get; init; } = 0.18;

    public double LightAccentTone { get; init; } = 0.5;

    public double DarkAccentTone { get; init; } = 0.76;

    private const double TextFloor = 4.6;
    private const double AlertFloor = 5.0;

    private static readonly (string Key, double Hue)[] Alerts =
    [
        ("--alert-note", 250),
        ("--alert-tip", 150),
        ("--alert-important", 300),
        ("--alert-warning", 70),
        ("--alert-caution", 25)
    ];

    public IReadOnlyDictionary<string, string> Light() => Build(dark: false);

    public IReadOnlyDictionary<string, string> Dark() => Build(dark: true);

    private Dictionary<string, string> Build(bool dark)
    {
        var n = dark ? DarkNeutral ?? Neutral : Neutral;
        var ground = dark ? DarkGround : LightGround;
        var step = dark ? 1.0 : -1.0;

        Oklch Surface(double offset, double chroma, double hue) =>
            Oklch.Of(ground + (step * offset), chroma, hue).ClampToSrgb();

        var bg = Surface(0, n.Chroma, n.Hue);
        var sidebar = Surface(0.022, n.Chroma * 1.15, n.Hue);
        var code = Surface(0.016, n.Chroma * 1.1, n.Hue);
        var accentLight = Surface(dark ? 0.065 : 0.045, Math.Min(Accent.Chroma * 0.3, 0.045), Accent.Hue);
        var border = Solve(ground + (step * (dark ? 0.1 : 0.085)), n.Chroma * 1.4, n.Hue, 1.25, [bg], dark);
        var selection = Surface(dark ? 0.2 : 0.11, Accent.Chroma * 0.45, Accent.Hue);

        Oklch[] surfaces = [bg, sidebar, code, accentLight];

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--bg-color"] = bg.ToString(),
            ["--sidebar-bg"] = sidebar.ToString(),
            ["--code-bg"] = code.ToString(),
            ["--search-bg"] = code.ToString(),
            ["--accent-light"] = accentLight.ToString(),
            ["--border"] = border.ToString(),
            ["--selection"] = selection.ToString(),
            ["--text-color"] = Solve(dark ? 0.94 : 0.2, Math.Min(n.Chroma, 0.02), n.Hue, 7, surfaces, dark).ToString(),
            ["--text-muted"] = Solve(dark ? 0.72 : 0.5, Math.Min(n.Chroma * 2, 0.045), n.Hue, TextFloor, surfaces, dark).ToString(),
            ["--accent"] = Solve(dark ? DarkAccentTone : LightAccentTone, Accent.Chroma, Accent.Hue, TextFloor, surfaces, dark).ToString()
        };

        if (AccentAlt is { } alt)
            tokens["--accent-alt"] = Solve(dark ? DarkAccentTone : LightAccentTone, alt.Chroma, alt.Hue, TextFloor, surfaces, dark).ToString();

        foreach (var (key, hue) in Alerts)
            tokens[key] = Solve(dark ? 0.76 : 0.52, dark ? 0.13 : 0.15, hue, AlertFloor, [bg, sidebar], dark).ToString();

        return tokens;
    }

    private static Oklch Solve(double tone, double chroma, double hue, double floor, Oklch[] surfaces, bool dark)
    {
        var step = dark ? 0.005 : -0.005;
        var colour = Oklch.Of(tone, chroma, hue).ClampToSrgb();
        for (var l = tone; l is > 0 and < 1; l += step)
        {
            colour = Oklch.Of(l, chroma, hue).ClampToSrgb();
            if (surfaces.All(s => colour.ContrastWith(s) >= floor))
                return colour;
        }
        return colour;
    }
}
