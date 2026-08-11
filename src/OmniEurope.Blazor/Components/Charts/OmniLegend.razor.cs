using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public partial class OmniLegend
{
    [Inject] private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;
    [Parameter] public string Label { get; set; } = string.Empty;
    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("LegendLabel")
        : Label;
    [Parameter] public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();
    private string Localize(string name) => StringLocalizer[name].Value;
}
