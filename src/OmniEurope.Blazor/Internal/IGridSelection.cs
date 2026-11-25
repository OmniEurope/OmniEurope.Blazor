namespace OmniEurope.Blazor.Components;

/// <summary>The row selection as the grid's other collaborators read it, from <see cref="GridSelection{TItem}"/>.</summary>
internal interface IGridSelection
{
    bool RowsAreInteractive { get; }

    bool IsSelected(object key);
}
