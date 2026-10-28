using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>The lost-connection overlay driven by the host: an outage that retries by itself then fails, a rejection, and a manual retry.</summary>
public partial class ConnectionOverlayExample : IDisposable
{
    /// <summary>The delay announced before the next retry when the state is set from outside.</summary>
    private static readonly TimeSpan AnnouncedDelay = TimeSpan.FromSeconds(8);

    private readonly CancellationTokenSource _lifetime = new();

    /// <summary>The state of the connection; the overlay shows itself in every state but <see cref="OmniConnectionState.Connected"/>.</summary>
    [Parameter]
    public OmniConnectionState State { get; set; } = OmniConnectionState.Connected;

    /// <summary>Raised when the example changes the state (an outage, a rejection, a successful retry).</summary>
    [Parameter]
    public EventCallback<OmniConnectionState> StateChanged { get; set; }

    private int Countdown { get; set; }

    private bool Reconnecting { get; set; }

    private string? Reason { get; set; }

    private TimeSpan RetryDelay => Countdown > 0 ? TimeSpan.FromSeconds(Countdown) : AnnouncedDelay;

    private string StateText => State switch
    {
        OmniConnectionState.Reconnecting => "reconnecting",
        OmniConnectionState.Failed => "failed",
        OmniConnectionState.Rejected => "rejected",
        _ => "established"
    };

    /// <summary>Two automatic attempts of three seconds, then the failure: only the manual retry remains.</summary>
    private async Task DropAsync()
    {
        Reason = "the server no longer responds (timed out)";
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await SetStateAsync(OmniConnectionState.Reconnecting);
            for (Countdown = 3; Countdown > 0; Countdown--)
            {
                StateHasChanged();
                if (!await DelayAsync(TimeSpan.FromSeconds(1)))
                {
                    return;
                }

                // Restored, rejected or retried by hand meanwhile: the automatic count stops.
                if (State != OmniConnectionState.Reconnecting || Reconnecting)
                {
                    return;
                }
            }
        }

        Countdown = 0;
        await SetStateAsync(OmniConnectionState.Failed);
    }

    private async Task Reject()
    {
        Reason = "session expired on the server";
        Countdown = 0;
        await SetStateAsync(OmniConnectionState.Rejected);
    }

    /// <summary>The manual retry succeeds after one second.</summary>
    private async Task ReconnectAsync()
    {
        Reconnecting = true;
        Countdown = 0;
        StateHasChanged();
        if (!await DelayAsync(TimeSpan.FromSeconds(1)))
        {
            return;
        }

        Reconnecting = false;
        Reason = null;
        await SetStateAsync(OmniConnectionState.Connected);
    }

    private async Task Reload()
    {
        Reason = null;
        await SetStateAsync(OmniConnectionState.Connected);
    }

    private async Task SetStateAsync(OmniConnectionState state)
    {
        State = state;
        await StateChanged.InvokeAsync(state);
    }

    private async Task<bool> DelayAsync(TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay, _lifetime.Token);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
