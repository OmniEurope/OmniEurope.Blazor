namespace OmniEurope.Blazor.Components;

public partial class OmniMain
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool FocusTarget { get; set; } = true;

    [Parameter]
    public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// How wide the content runs inside the main area: the whole of it, or a centred column capped
    /// at 90rem (<see cref="OmniLayoutWidth.Wide"/>) or 72rem (<see cref="OmniLayoutWidth.Content"/>).
    /// Unlike <see cref="OmniLayout.Width"/>, which narrows the whole shell, header and sidebar
    /// included, this narrows only the content. The two caps are read from
    /// <c>--omni-layout-wide-width</c> and <c>--omni-layout-content-width</c>, so a host can set its own.
    /// </summary>
    [Parameter]
    public OmniLayoutWidth ContentWidth { get; set; } = OmniLayoutWidth.Full;

    /// <summary>
    /// Makes the main area the scroll container of the page, filling the height its
    /// <see cref="OmniBody"/> leaves free. It scrolls across the whole width of the area, so the
    /// scrollbar sits at the window's edge even when <see cref="ContentWidth"/> centres the content,
    /// and the content keeps a gutter of <c>--omni-main-gutter</c> (by default the medium spacing).
    /// The shell has to be bounded in height for anything to scroll here: an <see cref="OmniLayout"/>
    /// whose body holds a scrollable main becomes a column, and the host gives it its height, for
    /// example the viewport's.
    /// </summary>
    [Parameter]
    public bool Scrollable { get; set; }

    private string? WidthClass => ContentWidth == OmniLayoutWidth.Full
        ? null
        : $"omni-main--{ContentWidth.ToString().ToLowerInvariant()}";
}
