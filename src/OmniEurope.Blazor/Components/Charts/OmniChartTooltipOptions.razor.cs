using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public partial class OmniChartTooltipOptions
{
    [Inject] private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;
    [Parameter] public bool Enabled { get; set; } = true;
    [Parameter] public string Description { get; set; } = string.Empty;
    private string EffectiveDescription => string.IsNullOrWhiteSpace(Description)
        ? Localize("ChartTooltipDescription")
        : Description;
    private string Localize(string name) => StringLocalizer[name].Value;
}
