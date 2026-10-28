using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Colors page: the value display and state of a colour picker, and presets that set its bound value from the page.</summary>
public partial class ColorsPage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private string Color { get; set; } = "#165DFF";

    private bool ShowValue { get; set; } = true;

    private bool PickerDisabled { get; set; }
}
