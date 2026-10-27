using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Drop-downs page: the settings of a country drop-down, then the same control for other value types.</summary>
public partial class DropDownsPage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private bool AllowEmpty { get; set; }

    private string EmptyText { get; set; } = string.Empty;

    private bool Grouped { get; set; }

    private bool DisableOption { get; set; } = true;

    private bool Filterable { get; set; }

    private bool ListDisabled { get; set; }
}
