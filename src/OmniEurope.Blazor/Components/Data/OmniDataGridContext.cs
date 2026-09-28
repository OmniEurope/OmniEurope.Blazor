namespace OmniEurope.Blazor.Components;

internal sealed class OmniDataGridContext<TItem>
{
    public required Action<OmniDataGridColumnDefinition<TItem>> Register { get; init; }
    public required Action<string> Unregister { get; init; }

    /// <summary>
    /// Called by a column that kept its registration but took delegates with another target: the grid
    /// rendered its cells with the previous ones before its columns received the new ones.
    /// </summary>
    public required Action DelegatesAdopted { get; init; }
}
