namespace Bark.Models;

/// <summary>A leaf link (<see cref="Path"/> set) or a group header (<see cref="Items"/> set, nesting allowed).</summary>
public class NavEntry
{
    public string Title { get; set; } = string.Empty;

    /// <summary>Leaf link target. Null/omitted when this entry is a group header.</summary>
    public string? Path { get; set; }

    /// <summary>Group collapse behavior: null is never collapsible, false starts expanded, true starts collapsed; a group holding the current page always renders expanded.</summary>
    public bool? Collapsed { get; set; }

    /// <summary>Child entries (links and/or nested groups). Null/empty marks this a leaf link.</summary>
    public List<NavEntry>? Items { get; set; }
}
