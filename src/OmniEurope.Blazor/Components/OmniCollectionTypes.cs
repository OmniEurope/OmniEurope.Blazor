namespace OmniEurope.Blazor.Components;

public sealed class OmniTreeContext<TValue>
{
    public IReadOnlyList<TValue> SelectedValues { get; set; } = Array.Empty<TValue>();
    public required Func<TValue, Task> ToggleSelectionAsync { get; init; }
}
