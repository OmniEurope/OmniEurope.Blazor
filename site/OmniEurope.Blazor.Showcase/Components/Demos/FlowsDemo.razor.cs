using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FlowsDemo : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private FlowsDemoModel Model { get; } = new();

    private FlowsDemoModel Credentials { get; } = new();

    private int Step { get; set; }

    private bool OptionsRefused { get; set; }

    private bool DialogOpen { get; set; }

    /// <summary>What the wizard ended with; <see langword="null"/> while it is still in progress.</summary>
    private string? Outcome { get; set; }

    private string? SignInNote { get; set; }

    private OmniConnectionState Connection { get; set; } = OmniConnectionState.Connected;

    private int Countdown { get; set; }

    private bool Reconnecting { get; set; }

    private string? Reason { get; set; }

    private string ConnectionText => Connection switch
    {
        OmniConnectionState.Reconnecting => Text["DemoFlowsConnectionReconnecting"],
        OmniConnectionState.Failed => Text["DemoFlowsConnectionFailed"],
        OmniConnectionState.Rejected => Text["DemoFlowsConnectionRejected"],
        _ => Text["DemoFlowsConnectionEstablished"]
    };

    /// <summary>La validation de l'étape des options : une sauvegarde est exigée, et l'étape le dit.</summary>
    private Task<bool> ValidateOptionsAsync()
    {
        OptionsRefused = !Model.Backup;
        return Task.FromResult(Model.Backup);
    }

    private void Finish() => Outcome = Text["DemoFlowsOutcomeCreated", Model.Name];

    private void Reset()
    {
        Model.Name = string.Empty;
        Model.Backup = false;
        OptionsRefused = false;
        Step = 0;
        Outcome = Text["DemoFlowsOutcomeCancelled"];
    }

    private void SubmitSignIn() => SignInNote = Text["DemoFlowsSignInNote", Credentials.Name];

    /// <summary>Deux tentatives automatiques de trois secondes, puis l'échec : seule la reprise manuelle reste.</summary>
    private async Task DropAsync()
    {
        Reason = Text["DemoFlowsReasonTimeout"];
        for (var attempt = 0; attempt < 2; attempt++)
        {
            Connection = OmniConnectionState.Reconnecting;
            for (Countdown = 3; Countdown > 0; Countdown--)
            {
                StateHasChanged();
                if (!await DelayAsync(TimeSpan.FromSeconds(1)))
                {
                    return;
                }

                // Rétablie, refusée ou reprise à la main entre-temps : le compte automatique s'arrête.
                if (Connection != OmniConnectionState.Reconnecting || Reconnecting)
                {
                    return;
                }
            }
        }

        Countdown = 0;
        Connection = OmniConnectionState.Failed;
    }

    private void Reject()
    {
        Reason = Text["DemoFlowsReasonExpired"];
        Countdown = 0;
        Connection = OmniConnectionState.Rejected;
    }

    /// <summary>La reprise manuelle aboutit après une seconde.</summary>
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
        Connection = OmniConnectionState.Connected;
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

    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
