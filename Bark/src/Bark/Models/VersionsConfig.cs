using System.Text.Json.Serialization;

namespace Bark.Models;

public sealed class VersionsConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>Newest first. Each link is that version's root relative to the site root, e.g. <c>"/"</c> then <c>"/v1/"</c>.</summary>
    public List<VersionItem>? Items { get; set; }

    /// <summary>Version codes with a docs folder, e.g. <c>"v1"</c> for <c>docs/v1/</c>. Set when docs load.</summary>
    [JsonIgnore]
    public IReadOnlySet<string> Folders { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class VersionItem : TopNavItem
{
    /// <summary>Sidebar for a folder version, keyed like <see cref="Config.Sidebar"/> with paths relative to the version folder. Null uses the site sidebar.</summary>
    public Dictionary<string, List<NavEntry>>? Sidebar { get; set; }
}
