using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Multi-select page: the settings of a country selection, then the control in its other forms.</summary>
public partial class MultiSelectPage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private bool Filterable { get; set; } = true;

    private string PlaceholderText { get; set; } = string.Empty;

    private string FilterPlaceholderText { get; set; } = string.Empty;

    private bool DisableOption { get; set; }

    private bool FieldDisabled { get; set; }

    /// <summary>On by default, as in the package; a click in the settings does not count as outside (see the page frame).</summary>
    private bool CloseOnOutsideClick { get; set; } = true;
}
