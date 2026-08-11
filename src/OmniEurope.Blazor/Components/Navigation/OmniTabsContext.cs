namespace OmniEurope.Blazor.Components;

internal sealed class OmniTabsContext
{
    public required string? Value { get; init; }
    public required Func<string, Task> SelectAsync { get; init; }
}
