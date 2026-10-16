using Bark.Models;

namespace Bark.Services.Rendering;

public sealed record LocaleRoute(string Code, string Prefix, string ContentPath, string? FallbackPath);

public static class LocaleRouting
{
    public const string RootKey = "root";

    public static string RootCode(Config? config) => Config.ResolveLocale(config)?.Code ?? "en";

    // A translated version is a tree of its own: docs/nl/v1.0/ is served at /nl/v1.0/.
    public static IReadOnlyList<string> TreeCodes(Config? config)
    {
        var locales = LocaleCodes(config);
        var versions = VersionCodes(config);
        return [.. locales, .. versions, .. locales.SelectMany(locale => versions.Select(version => TreeOf(locale, version)))];
    }

    public static string TreeOf(string locale, string version) =>
        locale.Length == 0 ? version : version.Length == 0 ? locale : $"{locale}/{version}";

    public static string LocalePartOf(Config? config, string code)
    {
        var head = code.Split('/')[0];
        return LocaleCodes(config).Contains(head, StringComparer.OrdinalIgnoreCase) ? head.ToLowerInvariant() : string.Empty;
    }

    public static string VersionPartOf(Config? config, string code)
    {
        var tail = code.Split('/')[^1];
        return VersionCodes(config).Contains(tail, StringComparer.OrdinalIgnoreCase) ? tail.ToLowerInvariant() : string.Empty;
    }

    // Pages missing from a translated tree are served from the same version in the root language. The root language of a version has no fallback.
    public static string? FallbackTreeOf(Config? config, string code)
    {
        var locale = LocalePartOf(config, code);
        return locale.Length == 0 ? null : VersionPartOf(config, code);
    }

    public static IReadOnlyList<string> LocaleCodes(Config? config) => NonRootKeys(config?.Locales?.Keys).Order().ToArray();

    // Config order, so the switcher lists versions as the author wrote them.
    public static IReadOnlyList<string> VersionCodes(Config? config) => NonRootKeys(config?.Versions?.Keys).Distinct().ToArray();

    public static IReadOnlyList<string> VersionLocaleCollisions(Config? config) =>
        VersionCodes(config).Where(code => code.Equals(RootCode(config), StringComparison.OrdinalIgnoreCase) || LocaleCodes(config).Contains(code)).ToArray();

    public static bool IsVersion(Config? config, string code) => VersionPartOf(config, code).Length > 0;

    public static string VersionLabelOf(Config? config, string code)
    {
        var version = VersionPartOf(config, code);
        var key = version.Length == 0 ? RootKey : version;
        var entry = config?.Versions?.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        return entry?.Label is { Length: > 0 } label ? label : key;
    }

    public static Dictionary<string, List<NavEntry>>? SidebarFor(Config? config, string prefix) =>
        VersionPartOf(config, prefix) is { Length: > 0 } version
            && config?.Versions?.FirstOrDefault(kv => kv.Key.Equals(version, StringComparison.OrdinalIgnoreCase)).Value?.Sidebar is { } sidebar
            ? sidebar
            : config?.Sidebar;

    private static IEnumerable<string> NonRootKeys(IEnumerable<string>? keys) =>
        (keys ?? [])
            .Where(key => !key.Equals(RootKey, StringComparison.OrdinalIgnoreCase))
            .Select(key => key.ToLowerInvariant());

    public static string LocaleOf(string path, IReadOnlyList<string> localeCodes, string rootLocale)
    {
        var segments = path.Trim('/').Split('/', 3);
        if (segments.Length > 1 && localeCodes.Contains($"{segments[0]}/{segments[1]}", StringComparer.OrdinalIgnoreCase))
            return $"{segments[0]}/{segments[1]}".ToLowerInvariant();

        return localeCodes.Contains(segments[0], StringComparer.OrdinalIgnoreCase) ? segments[0].ToLowerInvariant() : rootLocale;
    }

    public static LocaleEntry? EntryOf(Config? config, string code)
    {
        if (config?.Locales is null)
            return null;

        var key = code == RootCode(config) ? RootKey : code;
        return config.Locales.TryGetValue(key, out var entry) ? entry : null;
    }

    public static string LangOf(Config? config, string code)
    {
        if (IsVersion(config, code))
            code = LocalePartOf(config, code) is { Length: > 0 } locale ? locale : RootCode(config);

        return EntryOf(config, code)?.Lang is { Length: > 0 } lang ? lang : code;
    }

    public static string LabelOf(Config? config, string code) =>
        EntryOf(config, code)?.Label is { Length: > 0 } label ? label : code;

    public static LocaleRoute Resolve(Config? config, string path)
    {
        var trimmed = path.Trim('/');
        var rootCode = RootCode(config);
        var trees = TreeCodes(config);
        if (trees.Count == 0)
            return new LocaleRoute(rootCode, string.Empty, trimmed, null);

        var code = LocaleOf(trimmed, trees, rootCode);
        if (code == rootCode)
            return new LocaleRoute(rootCode, string.Empty, trimmed, null);

        var rest = trimmed.Length > code.Length ? trimmed[(code.Length + 1)..] : string.Empty;
        var fallback = FallbackTreeOf(config, code) is { } fallbackTree
            ? Localize(fallbackTree, rest.Length == 0 ? "index" : rest)
            : null;

        return new LocaleRoute(code, code, trimmed, fallback);
    }

    public static string Localize(string prefix, string path)
    {
        var trimmed = path.Trim('/');
        if (prefix.Length == 0)
            return trimmed;

        if (trimmed.Length == 0 || trimmed.Equals("index", StringComparison.OrdinalIgnoreCase))
            return prefix;

        var alreadyLocalized = trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase);

        return alreadyLocalized ? trimmed : $"{prefix}/{trimmed}";
    }

    // Root-relative links in a version page target its own tree; a leading locale or version segment is not repeated.
    public static string LocalizeInTree(string tree, string path)
    {
        var trimmed = path.Trim('/');
        if (trimmed.Equals(tree, StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith($"{tree}/", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        foreach (var segment in tree.Split('/'))
            if (trimmed.Equals(segment, StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith($"{segment}/", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed[segment.Length..].TrimStart('/');

        return Localize(tree, trimmed);
    }

    public static string Delocalize(string prefix, string path)
    {
        var trimmed = path.Trim('/');
        if (prefix.Length == 0 || !trimmed.StartsWith(prefix, StringComparison.Ordinal))
            return trimmed;

        var rest = trimmed[prefix.Length..].TrimStart('/');
        return rest.Length == 0 ? "index" : rest;
    }
}
