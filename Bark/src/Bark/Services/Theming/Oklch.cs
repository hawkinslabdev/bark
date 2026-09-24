using System.Globalization;

namespace Bark.Services.Theming;

public readonly record struct Oklch(double L, double C, double H)
{
    public static Oklch Of(double l, double c, double h) =>
        new(Math.Round(Math.Clamp(l, 0, 1), 3), Math.Round(Math.Max(c, 0), 3), Math.Round(((h % 360) + 360) % 360, 1));

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"oklch({L:0.###} {C:0.###} {H:0.#})");

    public static Oklch Parse(string css)
    {
        var parts = css.Trim()["oklch(".Length..^1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new(
            double.Parse(parts[0], CultureInfo.InvariantCulture),
            double.Parse(parts[1], CultureInfo.InvariantCulture),
            double.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    public (double R, double G, double B) ToLinearSrgb()
    {
        var rad = H * Math.PI / 180;
        var (a, b) = (C * Math.Cos(rad), C * Math.Sin(rad));

        var l = Math.Pow(L + (0.3963377774 * a) + (0.2158037573 * b), 3);
        var m = Math.Pow(L - (0.1055613458 * a) - (0.0638541728 * b), 3);
        var s = Math.Pow(L - (0.0894841775 * a) - (1.2914855480 * b), 3);

        return (
            (4.0767416621 * l) - (3.3077115913 * m) + (0.2309699292 * s),
            (-1.2684380046 * l) + (2.6097574011 * m) - (0.3413193965 * s),
            (-0.0041960863 * l) - (0.7034186147 * m) + (1.7076147010 * s));
    }

    public bool InSrgbGamut
    {
        get
        {
            const double eps = 0.0005;
            var (r, g, b) = ToLinearSrgb();
            return r is >= -eps and <= 1 + eps && g is >= -eps and <= 1 + eps && b is >= -eps and <= 1 + eps;
        }
    }

    public double RelativeLuminance
    {
        get
        {
            var (r, g, b) = ToLinearSrgb();
            return (0.2126 * Math.Clamp(r, 0, 1)) + (0.7152 * Math.Clamp(g, 0, 1)) + (0.0722 * Math.Clamp(b, 0, 1));
        }
    }

    public double ContrastWith(Oklch other)
    {
        var (x, y) = (RelativeLuminance, other.RelativeLuminance);
        return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05);
    }

    public Oklch ClampToSrgb()
    {
        if (InSrgbGamut)
            return this;

        double lo = 0, hi = C;
        for (var i = 0; i < 24; i++)
        {
            var mid = (lo + hi) / 2;
            if (new Oklch(L, mid, H).InSrgbGamut) lo = mid; else hi = mid;
        }
        return new(L, Math.Floor(lo * 1000) / 1000, H);
    }
}
