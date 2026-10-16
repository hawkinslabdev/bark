namespace Bark.Models;

public sealed class VersionEntry
{
    public string? Label { get; set; }

    /// <summary>Sidebar for this version, keyed like <see cref="Config.Sidebar"/> with paths relative to the version folder. Null uses the root sidebar.</summary>
    public Dictionary<string, List<NavEntry>>? Sidebar { get; set; }
}
