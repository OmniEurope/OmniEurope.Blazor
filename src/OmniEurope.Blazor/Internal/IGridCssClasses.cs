using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>The look of the bar buttons as the grid's other collaborators read it, from <see cref="GridCssClasses{TItem}"/>.</summary>
internal interface IGridCssClasses
{
    OmniControlSize BarButtonSize { get; }

    string ChooserToggleClass { get; }
}
