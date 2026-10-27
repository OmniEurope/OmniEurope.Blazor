using System.Globalization;
using OmniEurope.Blazor.Showcase.Examples.Forms;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Validation page: a sign-up form with validators in the markup, a form validated by attributes and a dynamic form.</summary>
public partial class ValidationPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    private ValidationMessagePlacement Placement { get; set; } = ValidationMessagePlacement.Validator;

    private double DelayValue { get; set; } = 300;

    private TimeSpan Delay => TimeSpan.FromMilliseconds(DelayValue);

    private bool FocusFirstInvalid { get; set; } = true;

    private ValidationSuccessFeedback Feedback { get; set; } = ValidationSuccessFeedback.Notification;

    private IReadOnlyList<OmniOption<ValidationMessagePlacement>> PlacementOptions =>
    [
        new(ValidationMessagePlacement.Validator, Text["ValidationPlacementValidator"]),
        new(ValidationMessagePlacement.Field, Text["ValidationPlacementField"]),
        new(ValidationMessagePlacement.Summary, Text["ValidationPlacementSummary"])
    ];

    private IReadOnlyList<OmniOption<ValidationSuccessFeedback>> FeedbackOptions =>
    [
        new(ValidationSuccessFeedback.Notification, Text["ValidationFeedbackNotification"]),
        new(ValidationSuccessFeedback.InlineAlert, Text["ValidationFeedbackInline"])
    ];

    private string FormatDelay(double milliseconds) =>
        Text["ValidationDelayValue", milliseconds.ToString("0", CultureInfo.CurrentCulture)];
}
