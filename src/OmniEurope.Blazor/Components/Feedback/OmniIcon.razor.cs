namespace OmniEurope.Blazor.Components;

public partial class OmniIcon
{
    [Parameter]
    public OmniIconName Name { get; set; }

    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    [Parameter]
    public string? AriaLabel { get; set; }
}
