using OmniEurope.Blazor.Showcase.Examples.Forms;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Check boxes and switches page: two-state and three-state controls, then each one in each state.</summary>
public partial class TogglesPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    private bool Disabled { get; set; }

    private bool AllowIndeterminate { get; set; } = true;

    private bool ShowLabels { get; set; } = true;

    private bool TextFirst { get; set; }

    /// <summary>The preview, whose four values the buttons of the settings force.</summary>
    private TogglesExample? Toggles { get; set; }

    private void SetAllNull() => Toggles?.SetAllNull();

    private void SetAll(bool value) => Toggles?.SetAll(value);
}
