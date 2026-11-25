namespace OmniEurope.Blazor.Components;

/// <summary>Whether the grid's header bar carries the column chooser, from <see cref="GridColumnChooser{TItem}"/>.</summary>
internal interface IGridColumnChooser
{
    bool Shown { get; }
}
