namespace OmniEurope.Blazor.Components;

public partial class OmniImage
{
    [Parameter, EditorRequired]
    public string Source { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string Alt { get; set; } = string.Empty;

    [Parameter]
    public int? Width { get; set; }

    [Parameter]
    public int? Height { get; set; }

    [Parameter]
    public OmniImageLoading Loading { get; set; } = OmniImageLoading.Lazy;

    [Parameter]
    public OmniImageFit Fit { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "Image dimensions must be positive when provided.");
        }
    }
}
