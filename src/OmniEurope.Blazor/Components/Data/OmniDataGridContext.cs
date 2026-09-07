namespace OmniEurope.Blazor.Components;

internal sealed class OmniDataGridContext<TItem>
{
    public required Action<OmniDataGridColumnDefinition<TItem>> Register { get; init; }
    public required Action<string> Unregister { get; init; }
}
