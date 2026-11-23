using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The rows in edit mode as the grid's other collaborators use them, from <see cref="GridRowEditing{TItem}"/>.</summary>
/// <typeparam name="TItem">The type of the rows.</typeparam>
internal interface IGridRowEditing<TItem>
{
    bool FocusRequested { get; }

    string? FocusColumn { get; }

    bool IsRowEditing(TItem item);

    void FocusGiven();

    bool TriggersFrom(OmniDataGridColumnDefinition<TItem> column);

    Task EditAsync(TItem item, string? focusColumn = null);

    Task EditCellAsync(int rowIndex, string columnKey);
}
