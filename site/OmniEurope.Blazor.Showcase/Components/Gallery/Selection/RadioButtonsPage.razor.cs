using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Radio buttons page: the settings of a shipping-method list, then the list for other value types and a star rating.</summary>
public partial class RadioButtonsPage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private bool ShowLegend { get; set; } = true;

    private bool DisableOption { get; set; } = true;

    private bool ListDisabled { get; set; }
}
