namespace OmniEurope.Blazor.Components;

internal sealed class OmniStepsContext
{
    public required int Value { get; init; }
    public required Func<int, Task> SelectAsync { get; init; }
}
