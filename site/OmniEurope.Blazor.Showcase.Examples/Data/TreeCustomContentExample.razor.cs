using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A tree whose items carry their own content (an icon and a remark), with selection mode and a disabled item as parameters.</summary>
public partial class TreeCustomContentExample
{
    /// <summary>Whether several items can be selected; off, selecting an item releases the previous one.</summary>
    [Parameter]
    public bool Multiple { get; set; } = true;

    /// <summary>Whether Luxembourg stays visible but can no longer be selected.</summary>
    [Parameter]
    public bool DisableItems { get; set; } = true;

    private IReadOnlyList<string> Selection { get; set; } = ["bru"];
}
