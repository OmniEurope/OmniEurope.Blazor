namespace OmniEurope.Blazor.Components;

public partial class OmniBadge
{
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public OmniIconName? Icon { get; set; }

    [Parameter]
    public OmniBadgeVariant Variant { get; set; }

    /// <summary>
    /// Painted or drawn. Solid, the default, takes the fill and ink of the button of the same intention;
    /// Filled is the lighter tonal chip; Outline is for a badge that has to read as a different kind of
    /// thing from the painted ones beside it, not merely as another colour.
    /// </summary>
    [Parameter]
    public OmniBadgeFill Fill { get; set; } = OmniBadgeFill.Solid;

    /// <summary>An icon and nothing else: the badge is then a square as tall as a text badge.</summary>
    private bool IconOnly => Icon is not null && string.IsNullOrEmpty(Text) && ChildContent is null;
}
