namespace Bark.Models;

public sealed record HeroActionFrontMatter
{
    /// <summary><c>"brand"</c> (filled), <c>"alt"</c> (outline) or <c>"minimal"</c> (text link). Defaults to <c>"brand"</c>.</summary>
    public string? Theme { get; init; }
    public string? Text { get; init; }
    public string? Link { get; init; }
}
