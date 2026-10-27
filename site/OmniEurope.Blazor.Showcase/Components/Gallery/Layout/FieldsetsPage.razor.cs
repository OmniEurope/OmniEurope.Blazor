using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Field groups page: one group driven by its settings, then the variants and a group whose state is bound.</summary>
public partial class FieldsetsPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private string LegendText { get; set; } = string.Empty;

    private bool Disabled { get; set; }

    private bool Collapsible { get; set; }

    private bool Collapsed { get; set; }

    protected override void OnInitialized() => LegendText = Text["FieldsetsLegendDefault"];
}
