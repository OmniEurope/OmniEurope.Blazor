namespace OmniEurope.Blazor.Components;

internal sealed class OmniTabsContext
{
    public required string? Value { get; init; }
    public required Func<string, Task> SelectAsync { get; init; }
    public required Func<string, string> RegisterKey { get; init; }
    public required OmniTabsPhase Phase { get; init; }

    /// <summary>
    /// Prefix of the ids of an item without its own: one per tabs, since each item is rendered twice
    /// (once per phase) and both halves must derive the same ids.
    /// </summary>
    public required string IdPrefix { get; init; }
    public bool RenderAllPanels { get; init; }
}
