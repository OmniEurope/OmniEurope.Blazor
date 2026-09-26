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

    /// <summary>
    /// Right of the plot, below the chart, or <see cref="OmniLegendPosition.Auto"/> (the default):
    /// right while the longest entry fits a narrow column, below otherwise. Outside a chart the
    /// legend is always drawn on the right.
    /// </summary>
    [Parameter] public OmniLegendPosition Position { get; set; } = OmniLegendPosition.Auto;

    private OmniChartContext.LegendRegistration Registration =>
        new(EffectiveLabel, Items, ColorIndexes, Position);

    protected override void OnParametersSet() => ChartContext?.RegisterLegend(this, Registration);
    private bool Below => ChartContext?.IsLegendBelow(this) == true;
    // The legend column right of the plot; it moves with the plot when the chart is wide.
    private double LegendLeft => ChartContext?.LegendLeft ?? 79;
    private string Localize(string name) => StringLocalizer[name].Value;
    private static string N(double value) => OmniChartGeometry.Number(value);
    public void Dispose() { ChartContext?.UnregisterLegend(this); GC.SuppressFinalize(this); }
}
