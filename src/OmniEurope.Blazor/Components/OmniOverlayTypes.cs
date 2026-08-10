using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

public enum OmniNotificationSeverity
{
    Information,
    Success,
    Warning,
    Error
}

public sealed record OmniDialogRequest(string Title, RenderFragment Content, string CloseLabel = "Fermer");

public sealed record OmniNotificationMessage(
    Guid Id,
    string Message,
    OmniNotificationSeverity Severity = OmniNotificationSeverity.Information,
    string? Title = null);

public sealed class OmniOverlayService
{
    private readonly List<OmniNotificationMessage> _notifications = [];

    public OmniDialogRequest? Dialog { get; private set; }
    public IReadOnlyList<OmniNotificationMessage> Notifications => _notifications;
    internal event Action? Changed;

    public void OpenDialog(OmniDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dialog = request;
        Changed?.Invoke();
    }

    public void CloseDialog()
    {
        Dialog = null;
        Changed?.Invoke();
    }

    public Guid Notify(string message, OmniNotificationSeverity severity = OmniNotificationSeverity.Information, string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var notification = new OmniNotificationMessage(Guid.NewGuid(), message, severity, title);
        _notifications.Add(notification);
        Changed?.Invoke();
        return notification.Id;
    }

    public bool Dismiss(Guid id)
    {
        var removed = _notifications.RemoveAll(notification => notification.Id == id) > 0;
        if (removed)
        {
            Changed?.Invoke();
        }

        return removed;
    }
}
