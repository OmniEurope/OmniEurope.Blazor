
namespace OmniEurope.Blazor.Components;

/// <summary>The look of the bar buttons as the grid's other collaborators read it, from <see cref="GridCssClasses{TItem}"/>.</summary>
internal interface IGridCssClasses
{
    OmniControlSize BarButtonSize { get; }

    string ChooserToggleClass { get; }
}
