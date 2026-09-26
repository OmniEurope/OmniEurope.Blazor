namespace OmniEurope.Blazor.Components;

public partial class OmniChart
{
    private readonly OmniChartContext _context = new();
    private bool _aspectRatioSet;
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? DataTable { get; set; }

    /// <summary>
    /// Width over height of the drawing. A wider value lets a line, area or column chart fill a wide,
    /// low card: the plot stretches, text keeps its size and a pie stays centred. Values under 1 are
    /// treated as 1. Left unset, the chart chooses: 2 for a time series (more than 12 categories on
    /// a vertical chart), 1 (square) otherwise; any value set, 1 included, is kept as given.
    /// </summary>
    [Parameter] public double AspectRatio { get; set; } = 1;

    private string SvgClass => _context.IsWide ? "omni-chart__svg omni-chart__svg--wide" : "omni-chart__svg";

    private string ViewBox => FormattableString.Invariant($"{_context.ViewLeft:0.###} 0 {_context.ViewWidth:0.###} 100");

    public override Task SetParametersAsync(ParameterView parameters)
    {
        _aspectRatioSet = parameters.TryGetValue<double>(nameof(AspectRatio), out _);
        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A preset may set the ratio without the markup naming it: a value other than the default counts as set.
        _context.SetAspectRatio(_aspectRatioSet || AspectRatio != 1 ? AspectRatio : null);
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
