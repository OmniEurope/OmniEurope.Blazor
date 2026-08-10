using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

public sealed class OmniTabsContext
{
    public required string? Value { get; init; }
    public required Func<string, Task> SelectAsync { get; init; }
}

public sealed class OmniStepsContext
{
    public required int Value { get; init; }
    public required Func<int, Task> SelectAsync { get; init; }
}
