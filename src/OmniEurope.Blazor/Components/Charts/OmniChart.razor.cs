namespace OmniEurope.Blazor.Components;

public partial class OmniChart
{
    private readonly OmniChartContext _context = new();
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? DataTable { get; set; }
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
