namespace OmniEurope.Blazor.Components;

/// <summary>
/// A message in the page: information, a success, a warning or an error, with an optional title, a
/// close button and an announcement to assistive technologies.
/// </summary>
public partial class OmniAlert
{
    private bool _dismissed;

    /// <summary>A title in bold before the message; none when null or blank.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The message.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>What the message is: its colour and its glyph. <see cref="OmniSeverity.Info"/> by default.</summary>
    [Parameter]
    public OmniSeverity Severity { get; set; } = OmniSeverity.Info;

    /// <summary>
    /// How much of the severity colour the alert carries. <see cref="OmniFill.Outline"/>, the default,
    /// keeps the message light on the page (the card surface, a rule in the colour);
    /// <see cref="OmniFill.Tonal"/> lays a light tint of it behind the page text;
    /// <see cref="OmniFill.Solid"/> paints the full colour behind it, which is what a blocking message
    /// needs to be read as one.
    /// </summary>
    [Parameter]
    public OmniFill Fill { get; set; } = OmniFill.Outline;

    /// <summary>
    /// Drawn in the icon disc before the title, in place of the severity's own glyph. A slot rather
    /// than a name, so the consumer keeps its own icon set. Empty by default: the disc then shows the
    /// glyph of <see cref="Severity"/> (i, check mark, exclamation mark or cross).
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Announces the message as soon as it appears (<c>role="alert"</c>, assertive): for a blocking error
    /// only, as the accessibility contract reserves assertive announcements. False, the default, gives
    /// the alert no live role: it is read where it stands.
    /// </summary>
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

    private string GlyphPath => OmniSeverityGlyph.For(Severity);

    private string EffectiveCloseLabel => LocalizeOr(CloseLabel, "Close");

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
