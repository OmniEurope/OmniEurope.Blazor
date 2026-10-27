using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A card holding a short label, optionally taller than its neighbours.</summary>
public partial class LayoutBlock
{
    /// <summary>The text of the block, usually its number or its span.</summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>Whether the block carries two more lines of text.</summary>
    [Parameter]
    public bool Tall { get; set; }
}
