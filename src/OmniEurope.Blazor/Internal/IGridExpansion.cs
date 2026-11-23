namespace OmniEurope.Blazor.Internal;

/// <summary>The open detail rows as the grid's other collaborators read them, from <see cref="GridExpansion{TItem}"/>.</summary>
internal interface IGridExpansion
{
    bool IsExpanded(object key);
}
