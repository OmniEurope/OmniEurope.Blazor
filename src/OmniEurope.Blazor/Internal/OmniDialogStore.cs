using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The stack of the open dialogs. A host may open or close a dialog from a background service while the
/// renderer draws them, so one lock guards the stack and readers get an immutable snapshot, never the stack
/// itself (the same contract as <see cref="OmniNotificationStore"/>).
/// </summary>
internal sealed class OmniDialogStore
{
    private readonly Lock _gate = new();
    private DialogSnapshot _snapshot = DialogSnapshot.Empty;

    /// <summary>The dialogs open at the time of the call, oldest first; a later change does not reach it.</summary>
    internal DialogSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    internal OmniDialogRequest? Current => Snapshot.Requests.LastOrDefault();

    internal IReadOnlyList<OmniDialogRequest> Items => Snapshot.Requests;

    internal void Push(OmniDialogRequest request)
    {
        lock (_gate)
        {
            // One identity per opened dialog, its render key: requests are records, so two dialogs opened
            // with equal values (a button pressed twice) would otherwise share a key and break the render.
            _snapshot = new([.. _snapshot.Requests, request], [.. _snapshot.Keys, new object()]);
        }
    }

    /// <summary>Removes every dialog; true when there was at least one.</summary>
    internal bool Clear()
    {
        lock (_gate)
        {
            var had = _snapshot.Requests.Count > 0;
            _snapshot = DialogSnapshot.Empty;
            return had;
        }
    }

    /// <summary>Removes the last dialog opened, handing it back; false when none is open.</summary>
    internal bool TryPop(out OmniDialogRequest? popped)
    {
        lock (_gate)
        {
            var requests = _snapshot.Requests;
            if (requests.Count == 0)
            {
                popped = null;
                return false;
            }

            popped = requests[^1];
            _snapshot = new([.. requests.Take(requests.Count - 1)], [.. _snapshot.Keys.Take(requests.Count - 1)]);
            return true;
        }
    }

    /// <summary>The dialogs open at one moment, with the render key of each, unique to its opening.</summary>
    /// <param name="Requests">The dialogs, oldest first.</param>
    /// <param name="Keys">The render key of the dialog at the same place in <paramref name="Requests"/>.</param>
    internal sealed record DialogSnapshot(IReadOnlyList<OmniDialogRequest> Requests, IReadOnlyList<object> Keys)
    {
        internal static DialogSnapshot Empty { get; } = new([], []);
    }
}
