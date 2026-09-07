namespace OmniEurope.Blazor.Components;

public partial class OmniNotification
{
    [Parameter, EditorRequired]
    public string Message { get; set; } = string.Empty;

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public OmniNotificationSeverity Severity { get; set; }

    [Parameter]
    public bool Dismissible { get; set; } = true;

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    private string SeverityClass => $"omni-notification--{Severity.ToString().ToLowerInvariant()}";
    private string Role => Severity == OmniNotificationSeverity.Error ? "alert" : "status";
    private string LiveMode => Severity == OmniNotificationSeverity.Error ? "assertive" : "polite";
}
