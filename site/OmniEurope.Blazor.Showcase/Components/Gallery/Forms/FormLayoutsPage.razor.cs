using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Form layouts page: a vertical form with its errors, its summary and its disabled and read-only fields.</summary>
public partial class FormLayoutsPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    /// <summary>Where the two forms of the preview show their summary of the errors: just above the actions, by default.</summary>
    private OmniValidationSummaryPosition SummaryPosition { get; set; } = OmniValidationSummaryPosition.Bottom;

    private IReadOnlyList<OmniOption<OmniValidationSummaryPosition>> SummaryPositionOptions =>
    [
        new(OmniValidationSummaryPosition.Top, Text["FormLayoutsSummaryTop"]),
        new(OmniValidationSummaryPosition.Bottom, Text["FormLayoutsSummaryBottom"]),
        new(OmniValidationSummaryPosition.Hidden, Text["FormLayoutsSummaryHidden"])
    ];
}
