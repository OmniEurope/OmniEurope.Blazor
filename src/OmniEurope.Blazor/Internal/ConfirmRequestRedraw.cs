
namespace OmniEurope.Blazor.Components;

/// <summary>
/// Keeps a part of a confirmation subscribed to the <see cref="OmniConfirmRequest.Refreshed"/> event of the
/// request it draws: it moves to a new request when the parameter changes and lets go on disposal. The part
/// passes its own redraw, so this helper never depends on it.
/// </summary>
internal sealed class ConfirmRequestRedraw(Action redraw) : IDisposable
{
    private OmniConfirmRequest? _subscribed;

    /// <summary>Subscribes to <paramref name="request"/> unless already subscribed to it, leaving the previous one.</summary>
    internal void Follow(OmniConfirmRequest request)
    {
        if (ReferenceEquals(_subscribed, request))
        {
            return;
        }

        Dispose();
        _subscribed = request;
        _subscribed.Refreshed += redraw;
    }

    /// <summary>Leaves the request followed, if any.</summary>
    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.Refreshed -= redraw;
            _subscribed = null;
        }
    }
}
