using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Opens the package's dialogs and shows its notifications, drawn by an <see cref="OmniComponentsHost"/>.
/// Registered as a scoped service by the package's service registration; a host given none creates and
/// owns its own. Dialogs stack: the last one opened is the one shown and the first one closed.
/// </summary>
public sealed class OmniOverlayService : IDisposable
{
    private readonly OmniNotificationStore _notifications;
    private readonly OmniDialogStore _dialogs = new();
    // Keyed by identity, not by value: OmniDialogRequest is a record, so two dialogs asking the same
    // question would otherwise share one entry and one of the two callers would never be answered.
    private readonly Dictionary<OmniDialogRequest, TaskCompletionSource<object?>> _pending =
        new(RequestIdentityComparer.Instance);

    /// <summary>Creates the service.</summary>
    /// <param name="timeProvider">The clock that times the notifications. Null, the default, is <see cref="TimeProvider.System"/>.</param>
    /// <param name="notificationCapacity">
    /// How many notifications are shown at once, 5 by default. A new one beyond it removes the oldest.
    /// </param>
    /// <param name="defaultNotificationDuration">
    /// How long a notification stays when <c>Notify</c> is given no duration. Null, the default, is
    /// 7 seconds; zero keeps notifications until they are dismissed.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="notificationCapacity"/> is less than 1, or <paramref name="defaultNotificationDuration"/> is negative.
    /// </exception>
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

    /// <summary>The dialog shown, the last one opened and not yet closed. Null when no dialog is open.</summary>
    public OmniDialogRequest? Dialog => _dialogs.Current;
    internal IReadOnlyList<OmniDialogRequest> Dialogs => _dialogs.Items;

    /// <summary>The notifications shown, oldest first.</summary>
    public IReadOnlyList<OmniNotificationMessage> Notifications => _notifications.Messages;
    internal event Action? Changed;

    /// <summary>
    /// Opens a dialog over any dialog already open, without waiting for its outcome. Use
    /// <see cref="OpenDialogAsync(OmniDialogRequest)"/> to wait for it.
    /// </summary>
    /// <param name="request">The dialog to open.</param>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <see cref="OmniDialogRequest.Width"/> is set and is not a number followed by px, rem, em, ch, vw or %.
    /// </exception>
    public void OpenDialog(OmniDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Internal.DialogWidth.Validate(request.Width, nameof(request));
        _dialogs.Push(request);
        RaiseChanged();
    }

    /// <summary>
    /// Closes the current dialog; a caller awaiting it through <see cref="OpenDialogAsync(OmniDialogRequest)"/>
    /// is answered null. Does nothing when no dialog is open.
    /// </summary>
    public void CloseDialog()
    {
        // A dialog opened through OpenDialogAsync has a caller waiting on it: closing it any other
        // way, including the panel's own close button, has to answer that caller rather than leave
        // it hanging for ever.
        var pending = _dialogs.Current;
        if (_dialogs.Pop())
        {
            // A dialog was open, so the stack had a current one.
            Complete(pending!, null);
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
        // Checked before a completion is registered: a refused width leaves no caller waiting.
        Internal.DialogWidth.Validate(request.Width, nameof(request));
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

    /// <summary>
    /// Opens <typeparamref name="TComponent"/> as the content of a dialog and waits for its outcome, like
    /// <see cref="OpenDialogAsync(OmniDialogRequest)"/>. Each entry of <paramref name="parameters"/> is
    /// passed to the component as the parameter of that name. The component reports its outcome with
    /// <see cref="CloseDialog(object?)"/>; any other dismissal answers null.
    /// </summary>
    public Task<object?> OpenDialogAsync<TComponent>(
        string title,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string closeLabel = "")
        where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        OpenDialogAsync<TComponent>(title, parameters, OmniDialogSize.Medium, closeLabel);

    /// <summary>
    /// Same as <see cref="OpenDialogAsync{TComponent}(string, IReadOnlyDictionary{string, object?}?, string)"/>,
    /// plus how wide the dialog may grow. A separate overload rather than one more optional parameter,
    /// which would have changed the signature callers are already compiled against.
    /// </summary>
    public Task<object?> OpenDialogAsync<TComponent>(
        string title,
        IReadOnlyDictionary<string, object?>? parameters,
        OmniDialogSize size,
        string closeLabel = "")
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        ArgumentNullException.ThrowIfNull(title);
        return OpenDialogAsync(OmniDialogRequest.ForComponent<TComponent>(title, parameters, closeLabel) with
        {
            Size = size
        });
    }

    /// <summary>
    /// Asks a yes-or-no question in a dialog built to the package's button convention: the action
    /// first, cancelling after it in the neutral <see cref="OmniButtonVariant.Secondary"/>, both at the end of the
    /// footer and each with its icon. True only when the action is chosen; cancelling, the close
    /// button, Escape and the backdrop all answer false.
    /// </summary>
    public async Task<bool> ConfirmAsync(OmniConfirmRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dialog = new OmniDialogRequest(request.Title, content =>
        {
            content.OpenElement(0, "p");
            content.AddAttribute(1, "class", "omni-confirm__message");
            content.AddContent(2, request.Message);
            content.CloseElement();
        })
        {
            Intent = request.EffectiveIntent,
            Footer = footer =>
            {
                footer.OpenComponent<OmniConfirmFooter>(0);
                footer.AddComponentParameter(1, nameof(OmniConfirmFooter.Request), request);
                footer.AddComponentParameter(2, nameof(OmniConfirmFooter.Service), this);
                footer.CloseComponent();
            }
        };

        return await OpenDialogAsync(dialog) is true;
    }

    /// <summary>Closes the current dialog and hands <paramref name="result"/> to whoever awaits it.</summary>
    public void CloseDialog(object? result)
    {
        var pending = _dialogs.Current;
        if (_dialogs.Pop())
        {
            Complete(pending!, result);
            RaiseChanged();
        }
    }

    private void Complete(OmniDialogRequest request, object? result)
    {
        if (_pending.Remove(request, out var completion))
        {
            completion.TrySetResult(result);
        }
    }

    /// <summary>
    /// Shows a notification. When as many are shown as the service's capacity, the oldest is removed
    /// first. A message longer than 300 characters stays at least four seconds plus one per hundred
    /// characters (thirty at most), so it can be read.
    /// </summary>
    /// <param name="message">The text of the notification. Must not be null, empty or white space.</param>
    /// <param name="severity">How the notification reads. <see cref="OmniSeverity.Info"/> by default.</param>
    /// <param name="title">A title over the message. Null, the default, shows none.</param>
    /// <param name="duration">
    /// How long it stays. Null, the default, is the service's default duration; zero or less keeps it
    /// until it is dismissed.
    /// </param>
    /// <returns>The id of the notification, for <see cref="Dismiss(Guid)"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="message"/> is null, empty or white space.</exception>
    public Guid Notify(
        string message,
        OmniSeverity severity = OmniSeverity.Info,
        string? title = null,
        TimeSpan? duration = null) => Notify(message, severity, title, duration, detailsHref: null);

    /// <summary>
    /// Same as the overload without it, plus where the full account can be read. A separate overload
    /// rather than one more optional parameter, which would have changed the signature callers are
    /// already compiled against.
    /// </summary>
    /// <param name="message">The text of the notification. Must not be null, empty or white space.</param>
    /// <param name="severity">How the notification reads.</param>
    /// <param name="title">A title over the message, or null for none.</param>
    /// <param name="duration">
    /// How long it stays. Null is the service's default duration; zero or less keeps it until it is dismissed.
    /// </param>
    /// <param name="detailsHref">
    /// Offered from the notification when its message is too long to read in one. Checked against
    /// the same URI policy as every other link.
    /// </param>
    /// <returns>The id of the notification, for <see cref="Dismiss(Guid)"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="message"/> is null, empty or white space.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="detailsHref"/> uses a URI scheme that is not allowed.</exception>
    public Guid Notify(
        string message,
        OmniSeverity severity,
        string? title,
        TimeSpan? duration,
        string? detailsHref)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return _notifications.Add(message, severity, title, duration, OmniUriPolicy.EnsureSafe(detailsHref, nameof(detailsHref)));
    }

    /// <summary>
    /// Same as the overload without it, plus one action offered from the notification as a button
    /// (undoing what was just done, retrying). Choosing it runs <paramref name="action"/> once and
    /// closes the notification. A separate overload, so compiled callers keep their signatures.
    /// </summary>
    public Guid Notify(
        string message,
        OmniSeverity severity,
        string? title,
        TimeSpan? duration,
        string actionText,
        Func<Task> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionText);
        ArgumentNullException.ThrowIfNull(action);
        return _notifications.Add(message, severity, title, duration, detailsHref: null, actionText, action);
    }

    /// <summary>Removes a notification before it expires.</summary>
    /// <param name="id">The id returned by <c>Notify</c>.</param>
    /// <returns>True when the notification was shown and is now removed; false when it was already gone.</returns>
    public bool Dismiss(Guid id) => _notifications.Remove(id);

    /// <summary>Holds a notification's countdown while it is read.</summary>
    internal void PauseNotification(Guid id) => _notifications.Pause(id);

    internal void ResumeNotification(Guid id) => _notifications.Resume(id);

    private void RaiseChanged() => Changed?.Invoke();

    /// <summary>
    /// Closes every open dialog, answers null to every caller still awaiting one, and stops the
    /// notification timers. A host still drawing the service is told once when a dialog was open, so
    /// it takes the dialogs down. A notification cannot be shown afterwards.
    /// </summary>
    public void Dispose()
    {
        // Anything still awaiting a dialog is answered rather than left pending for ever.
        foreach (var completion in _pending.Values)
        {
            completion.TrySetResult(null);
        }

        _pending.Clear();
        var closed = _dialogs.Clear();
        _notifications.Dispose();
        if (closed)
        {
            RaiseChanged();
        }

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
