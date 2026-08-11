namespace OmniEurope.Blazor.Components;

public partial class OmniGrid
{
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public int Columns { get; set; } = 1;

    [Parameter]
    public OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Columns is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(Columns), "Grid columns must be between 1 and 12.");
        }
    }
}
