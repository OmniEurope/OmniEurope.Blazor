namespace OmniEurope.Blazor.Components;

internal sealed class OmniStepsContext
{
    public required int Value { get; init; }
    public required Func<int, Task> SelectAsync { get; init; }

    /// <summary>Records a step in the order it is first rendered, for the steps that give no index.</summary>
    public required Action<IOmniStepsEntry> Register { get; init; }

    public required Action<IOmniStepsEntry> Unregister { get; init; }

    /// <summary>The position of a registered step among the steps, from zero; -1 for a step not registered.</summary>
    public required Func<IOmniStepsEntry, int> PositionOf { get; init; }
}
