using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Unsaved changes page: a guard watching a flag, then a form that sets its own guard.</summary>
public partial class UnsavedChangesPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    private bool HasChanges { get; set; } = true;

    private bool CustomTexts { get; set; }
}
