namespace OmniEurope.Blazor.Components;

public partial class OmniAlert
{
    private bool _dismissed;

    [Parameter]
    public string? Title { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniAlertSeverity Severity { get; set; } = OmniAlertSeverity.Info;

    /// <summary>
    /// Outline keeps the message light on the page; Filled paints the severity colour behind it,
    /// which is what a blocking message needs to be read as one.
    /// </summary>
    [Parameter]
    public OmniAlertVariant Variant { get; set; } = OmniAlertVariant.Outline;

    /// <summary>
    /// Drawn in the icon disc before the title, in place of the severity's own glyph. A slot rather
    /// than a name, so the consumer keeps its own icon set. Empty by default: the disc then shows the
    /// glyph of <see cref="Severity"/> (i, check mark, exclamation mark or cross).
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    [Parameter]
    public bool Live { get; set; }

    /// <summary>
    /// Adds a close button that hides the alert. Off by default: an alert that is not asked to be
    /// dismissible renders exactly as before.
    /// </summary>
    [Parameter]
    public bool Dismissible { get; set; }

    /// <summary>Accessible name and tooltip of the close button; the localized "Close" when empty.</summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    /// <summary>Raised once the user has closed the alert.</summary>
    [Parameter]
    public EventCallback OnDismiss { get; set; }

    // The glyph alone, without a circle or a triangle around it (the disc is the frame), drawn
    // centred on (12, 12) of a 24-unit box so it sits in the middle of the disc.
    private string GlyphPath => Severity switch
    {
        OmniAlertSeverity.Success => "m6.5 12.5 3.5 3.5 7.5-8",
        OmniAlertSeverity.Warning => "M12 7v6M12 17h.01",
        OmniAlertSeverity.Danger => "M8 8l8 8M16 8l-8 8",
        _ => "M12 11v6M12 7h.01"
    };

    private string EffectiveCloseLabel => string.IsNullOrWhiteSpace(CloseLabel) ? Localize("Close") : CloseLabel;

    private async Task DismissAsync()
    {
        _dismissed = true;
        await OnDismiss.InvokeAsync();
    }
}
