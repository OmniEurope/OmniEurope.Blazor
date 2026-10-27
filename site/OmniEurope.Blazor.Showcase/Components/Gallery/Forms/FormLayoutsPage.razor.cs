using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Form layouts page: a vertical form with its errors, its summary and its disabled and read-only fields.</summary>
public partial class FormLayoutsPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;
}
