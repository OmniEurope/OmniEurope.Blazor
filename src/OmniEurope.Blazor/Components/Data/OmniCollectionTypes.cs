namespace OmniEurope.Blazor.Components;

internal sealed class OmniTreeContext<TValue>
{
    public IReadOnlyList<TValue> SelectedValues { get; set; } = Array.Empty<TValue>();
    public required Func<TValue, Task> ToggleSelectionAsync { get; init; }
}
