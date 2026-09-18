namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class FlowsDemo : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();

    private FlowsDemoModel Model { get; } = new();

    private FlowsDemoModel Credentials { get; } = new();

    private int Step { get; set; }

    private bool OptionsRefused { get; set; }

    private bool DialogOpen { get; set; }

    private string Outcome { get; set; } = "en cours";

    private string? SignInNote { get; set; }

    private OmniConnectionState Connection { get; set; } = OmniConnectionState.Connected;

    private int Countdown { get; set; }

    private bool Reconnecting { get; set; }

    private string? Reason { get; set; }

    private string ConnectionText => Connection switch
    {
        OmniConnectionState.Reconnecting => "reconnexion",
        OmniConnectionState.Failed => "échec",
        OmniConnectionState.Rejected => "refusée",
        _ => "établie"
    };

    /// <summary>La validation de l'étape des options : une sauvegarde est exigée, et l'étape le dit.</summary>
    private Task<bool> ValidateOptionsAsync()
    {
        OptionsRefused = !Model.Backup;
        return Task.FromResult(Model.Backup);
    }

    private void Finish() => Outcome = $"serveur « {Model.Name} » créé";

    private void Reset()
    {
        Model.Name = string.Empty;
        Model.Backup = false;
        OptionsRefused = false;
        Step = 0;
        Outcome = "annulé";
    }

    private void SubmitSignIn() => SignInNote = $"Formulaire remis à l'hôte pour « {Credentials.Name} ».";

    /// <summary>Deux tentatives automatiques de trois secondes, puis l'échec : seule la reprise manuelle reste.</summary>
    private async Task DropAsync()
    {
        Reason = "le serveur ne répond plus (délai dépassé)";
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
        Reason = "session expirée côté serveur";
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
