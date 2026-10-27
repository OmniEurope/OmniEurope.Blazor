using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Feedback;

/// <summary>The Alerts page: the settings of one alert, then every severity in every fill.</summary>
public partial class AlertsPage
{
    [Inject]
    private IStringLocalizer<FeedbackStrings> Text { get; set; } = default!;

    private OmniSeverity Severity { get; set; } = OmniSeverity.Info;

    private OmniFill Fill { get; set; } = OmniFill.Outline;

    private bool ShowTitle { get; set; } = true;

    private bool ShowIcon { get; set; } = true;

    private bool Dismissible { get; set; } = true;

    private bool Live { get; set; }

    private bool LongText { get; set; }

    private IReadOnlyList<OmniOption<OmniSeverity>> SeverityOptions =>
    [
        new(OmniSeverity.Info, Text["AlertsSeverityInfo"]),
        new(OmniSeverity.Success, Text["AlertsSeveritySuccess"]),
        new(OmniSeverity.Warning, Text["AlertsSeverityWarning"]),
        new(OmniSeverity.Danger, Text["AlertsSeverityDanger"])
    ];

    private IReadOnlyList<OmniOption<OmniFill>> FillOptions =>
    [
        new(OmniFill.Outline, Text["AlertsVariantOutline"]),
        new(OmniFill.Tonal, Text["AlertsVariantTonal"]),
        new(OmniFill.Solid, Text["AlertsVariantFilled"])
    ];
}
