namespace OmniEurope.Blazor.Components;

/// <summary>The saved grid state as the grid's other collaborators use it, from <see cref="GridStatePersistence{TItem}"/>.</summary>
internal interface IGridStatePersistence
{
    Task SaveAsync();
}
