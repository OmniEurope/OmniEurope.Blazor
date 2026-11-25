using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the markup of the column chooser (<see cref="GridColumnChooserView{TItem}"/>) reads and calls on the
/// chooser (<see cref="GridColumnChooser{TItem}"/>) that owns it.
/// </summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridColumnChooserModel<TItem>
{
    ElementReference List { get; set; }

    IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Entries { get; }

    bool IsVisible(OmniDataGridColumnDefinition<TItem> column);

    bool CanToggle(OmniDataGridColumnDefinition<TItem> column);

    bool CanMove(OmniDataGridColumnDefinition<TItem> column, int step);

    Task ToggleAsync(OmniDataGridColumnDefinition<TItem> column, bool visible);

    Task MoveAsync(OmniDataGridColumnDefinition<TItem> column, int step);

    bool IsFrozen(OmniDataGridColumnDefinition<TItem> column);

    Task FreezeAsync(OmniDataGridColumnDefinition<TItem> column, bool frozen);

    Task ResetAsync();
}
