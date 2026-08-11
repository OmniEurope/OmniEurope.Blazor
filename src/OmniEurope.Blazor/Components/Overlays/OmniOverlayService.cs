using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public sealed class OmniOverlayService : IDisposable
{
    private readonly OmniNotificationStore _notifications;
    private readonly OmniDialogStore _dialogs = new();

    public OmniOverlayService(
        TimeProvider? timeProvider = null,
        int notificationCapacity = 5,
        TimeSpan? defaultNotificationDuration = null)
    {
        _notifications = new OmniNotificationStore(
            timeProvider ?? TimeProvider.System,
            RaiseChanged,
            notificationCapacity,
            defaultNotificationDuration ?? TimeSpan.FromSeconds(7));
    }

    public OmniDialogRequest? Dialog => _dialogs.Current;
    internal IReadOnlyList<OmniDialogRequest> Dialogs => _dialogs.Items;
    public IReadOnlyList<OmniNotificationMessage> Notifications => _notifications.Messages;
    internal event Action? Changed;

    public void OpenDialog(OmniDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _dialogs.Push(request);
        RaiseChanged();
    }

    public void CloseDialog()
    {
        if (_dialogs.Pop())
        {
            RaiseChanged();
        }
    }

    public Guid Notify(
        string message,
        OmniNotificationSeverity severity = OmniNotificationSeverity.Information,
        string? title = null,
        TimeSpan? duration = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return _notifications.Add(message, severity, title, duration);
    }

    public bool Dismiss(Guid id) => _notifications.Remove(id);

    private void RaiseChanged() => Changed?.Invoke();

    public void Dispose()
    {
        _notifications.Dispose();
        GC.SuppressFinalize(this);
    }
}
