using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>An alert whose severity, fill, title, icon, announcement, length and cross are parameters.</summary>
public partial class AlertExample
{
    /// <summary>The severity chooses the colour and the sample text.</summary>
    [Parameter]
    public OmniSeverity Severity { get; set; } = OmniSeverity.Info;

    /// <summary>How the colour is laid: a tint, an outline or the full colour.</summary>
    [Parameter]
    public OmniFill Fill { get; set; } = OmniFill.Outline;

    /// <summary>Whether a bold line sits above the message.</summary>
    [Parameter]
    public bool ShowTitle { get; set; } = true;

    /// <summary>Whether the icon slot is filled with the icon of the severity.</summary>
    [Parameter]
    public bool ShowIcon { get; set; } = true;

    /// <summary>Whether the alert interrupts the screen reader instead of waiting for a pause.</summary>
    [Parameter]
    public bool Live { get; set; }

    /// <summary>Whether the message is long enough to check how it wraps next to the icon.</summary>
    [Parameter]
    public bool LongText { get; set; }

    /// <summary>Whether the alert carries a cross that closes it.</summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    private bool Visible { get; set; } = true;

    private AlertSample Current => AlertSample.Of(Severity);

    private string? PreviewTitle => ShowTitle ? Current.Title : null;

    private string PreviewMessage => LongText ? AlertSample.LongMessage : Current.Message;

    private void Hide() => Visible = false;

    private void Show() => Visible = true;
}
