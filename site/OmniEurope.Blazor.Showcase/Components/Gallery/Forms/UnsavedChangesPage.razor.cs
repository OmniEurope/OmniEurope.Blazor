using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Unsaved changes page: a guard watching a flag, then a form that sets its own guard.</summary>
public partial class UnsavedChangesPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    // Off on arrival: a visitor who only passes through is not stopped on the way out; the switch turns the
    // pending changes on to try the guard.
    private bool HasChanges { get; set; }

    private bool CustomTexts { get; set; }
}
