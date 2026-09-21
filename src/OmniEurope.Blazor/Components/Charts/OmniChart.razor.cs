namespace OmniEurope.Blazor.Components;

public partial class OmniChart
{
    private readonly OmniChartContext _context = new();
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? DataTable { get; set; }

    /// <summary>
    /// Width over height of the drawing, 1 (square) by default. A wider value lets a line, area or
    /// column chart fill a wide, low card: the plot stretches, text keeps its size and a pie stays
    /// centred. Values under 1 are treated as 1; set the chart's CSS height to match.
    /// </summary>
    [Parameter] public double AspectRatio { get; set; } = 1;

    private string ViewBox => FormattableString.Invariant($"{_context.ViewLeft:0.###} 0 {_context.ViewWidth:0.###} 100");

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _context.SetAspectRatio(AspectRatio);
    }
    private string TitleId => $"{Id ?? "omni-chart"}-title";
    private string DescriptionId => $"{Id ?? "omni-chart"}-description";

    protected override void OnInitialized() => _context.Changed += HandleProjectionChanged;
    private void HandleProjectionChanged() => _ = InvokeAsync(StateHasChanged);
    public void Dispose()
    {
        _context.Changed -= HandleProjectionChanged;
        GC.SuppressFinalize(this);
    }
}
