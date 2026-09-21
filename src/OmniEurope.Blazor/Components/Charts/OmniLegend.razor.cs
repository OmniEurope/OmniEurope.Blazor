using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public partial class OmniLegend
{
    [Inject] private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;
    [CascadingParameter] private OmniChartContext? ChartContext { get; set; }
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("LegendLabel")
        : Label;
    [Parameter] public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The <c>ColorIndex</c> of the series each item names, in the order of <see cref="Items"/>. Empty,
    /// or shorter than the items, the remaining items take the colours 0, 1, 2 and on by position.
    /// </summary>
    [Parameter] public IReadOnlyList<int> ColorIndexes { get; set; } = Array.Empty<int>();
    protected override void OnParametersSet() => ChartContext?.RegisterLegend(this);
    // The legend column right of the plot; it moves with the plot when the chart is wide.
    private double LegendLeft => ChartContext?.LegendLeft ?? 79;
    private string Localize(string name) => StringLocalizer[name].Value;
    private int ColorOf(int index) => Math.Abs(index < ColorIndexes.Count ? ColorIndexes[index] : index) % 8;
    private static string N(double value) => OmniChartGeometry.Number(value);
    public void Dispose() { ChartContext?.UnregisterLegend(this); GC.SuppressFinalize(this); }
}
