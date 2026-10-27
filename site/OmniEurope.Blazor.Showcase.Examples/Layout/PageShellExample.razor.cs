using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A miniature page shell whose width, scrolling, header and main-area accessibility are parameters.</summary>
public partial class PageShellExample
{
    /// <summary>The width of the content column of the main area.</summary>
    [Parameter]
    public OmniLayoutWidth ContentWidth { get; set; } = OmniLayoutWidth.Content;

    /// <summary>Whether the main area scrolls by itself, its scrollbar at the right edge of the frame.</summary>
    [Parameter]
    public bool Scrollable { get; set; } = true;

    /// <summary>Whether the header stays at the top of the container that scrolls.</summary>
    [Parameter]
    public bool Sticky { get; set; } = true;

    /// <summary>Whether the main area can receive the focus from a skip link or a navigation.</summary>
    [Parameter]
    public bool FocusTarget { get; set; } = true;

    /// <summary>Whether the main area is named by its heading.</summary>
    [Parameter]
    public bool LabelledBy { get; set; } = true;

    /// <summary>Whether the scrollbar of the main area shows only while the content moves.</summary>
    [Parameter]
    public bool AutoHideScrollbar { get; set; }

    /// <summary>The surface of the header: the page colour or the accent band.</summary>
    [Parameter]
    public OmniHeaderSurface Surface { get; set; } = OmniHeaderSurface.Surface;
}
