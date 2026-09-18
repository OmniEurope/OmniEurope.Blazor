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

    private string GlyphPath => Severity switch
    {
        OmniAlertSeverity.Success => OmniSeverityGlyph.Success,
        OmniAlertSeverity.Warning => OmniSeverityGlyph.Warning,
        OmniAlertSeverity.Danger => OmniSeverityGlyph.Danger,
        _ => OmniSeverityGlyph.Information
    };

    private string EffectiveCloseLabel => string.IsNullOrWhiteSpace(CloseLabel) ? Localize("Close") : CloseLabel;

    private async Task DismissAsync()
    {
        _dismissed = true;
        await OnDismiss.InvokeAsync();
    }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
