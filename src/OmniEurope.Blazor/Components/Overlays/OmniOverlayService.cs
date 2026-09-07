using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public sealed class OmniOverlayService : IDisposable
{
    private readonly OmniNotificationStore _notifications;
    private readonly OmniDialogStore _dialogs = new();
    // Keyed by identity, not by value: OmniDialogRequest is a record, so two dialogs asking the same
    // question would otherwise share one entry and one of the two callers would never be answered.
    private readonly Dictionary<OmniDialogRequest, TaskCompletionSource<object?>> _pending =
        new(RequestIdentityComparer.Instance);

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
        // A dialog opened through OpenDialogAsync has a caller waiting on it: closing it any other
        // way, including the panel's own close button, has to answer that caller rather than leave
        // it hanging for ever.
        var pending = _dialogs.Current;
        if (_dialogs.Pop())
        {
            Complete(pending, null);
            RaiseChanged();
        }
    }

    /// <summary>
    /// Opens a dialog and waits for its outcome. The content decides what the outcome is and reports
    /// it with <see cref="CloseDialog(object?)"/>; dismissing the dialog any other way answers null.
    /// </summary>
    public Task<object?> OpenDialogAsync(OmniDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Reopening the same instance while a caller still waits on it would drop that caller's
        // completion and leave it hanging for ever: answer it the way any other dismissal does.
        if (_pending.Remove(request, out var displaced))
        {
            displaced.TrySetResult(null);
        }

        _pending[request] = completion;
        OpenDialog(request);
        return completion.Task;
    }

    /// <summary>Closes the current dialog and hands <paramref name="result"/> to whoever awaits it.</summary>
    public void CloseDialog(object? result)
    {
        var pending = _dialogs.Current;
        if (_dialogs.Pop())
        {
            Complete(pending, result);
            RaiseChanged();
        }
    }

    private void Complete(OmniDialogRequest? request, object? result)
    {
        if (request is not null && _pending.Remove(request, out var completion))
        {
            completion.TrySetResult(result);
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
        // Anything still awaiting a dialog is answered rather than left pending for ever.
        foreach (var completion in _pending.Values)
        {
            completion.TrySetResult(null);
        }

        _pending.Clear();
        _notifications.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class RequestIdentityComparer : IEqualityComparer<OmniDialogRequest>
    {
        internal static RequestIdentityComparer Instance { get; } = new();

        public bool Equals(OmniDialogRequest? left, OmniDialogRequest? right) => ReferenceEquals(left, right);

        public int GetHashCode(OmniDialogRequest request) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(request);
    }
}
