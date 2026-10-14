namespace Bark.Models;

public sealed class VersionsConfig
{
    public bool Enabled { get; set; } = true;

    /// <summary>Newest first. Each link is that version's root relative to the site root, e.g. <c>"/"</c> then <c>"/v1/"</c>.</summary>
    public List<TopNavItem>? Items { get; set; }
}
