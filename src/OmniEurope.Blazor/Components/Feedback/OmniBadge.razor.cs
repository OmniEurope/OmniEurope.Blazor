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
    /// Painted or drawn. Outline is for a badge that has to read as a different kind of thing from
    /// the filled ones beside it, not merely as another colour.
    /// </summary>
    [Parameter]
    public OmniBadgeFill Fill { get; set; } = OmniBadgeFill.Filled;
}
