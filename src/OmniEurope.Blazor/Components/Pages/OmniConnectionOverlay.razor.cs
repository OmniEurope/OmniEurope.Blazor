namespace OmniEurope.Blazor.Components;

/// <summary>
/// The blocking overlay shown while the application's live connection is down: what happened, a
/// countdown to the next attempt, the reason of the last failure and a manual action. It holds no
/// connection and no clock: the host drives it through <see cref="State"/>,
/// <see cref="TimeUntilRetry"/>, <see cref="Busy"/> and the callbacks.
/// </summary>
/// <remarks>
/// Shown, it takes the focus onto its action and keeps Tab inside itself, as a modal dialog does, and
/// gives the focus back to where it was once the connection is back.
/// </remarks>
public partial class OmniConnectionOverlay
{
    private const string FocusModulePath = Internal.OmniModules.Focus;

    private readonly string _focusKey = $"connection-{Guid.NewGuid():N}";
    private readonly string _generatedId = $"omni-connection-{Guid.NewGuid():N}";
    private ElementReference _card;
    private IJSObjectReference? _focusModule;
    private bool _focusActivated;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The state of the connection; Connected renders nothing.</summary>
    [Parameter]
    public OmniConnectionState State { get; set; }

    /// <summary>
    /// Time left before the next automatic attempt, shown while reconnecting in whole seconds rounded up;
    /// <see cref="TimeSpan.Zero"/> (the default) or less hides the countdown.
    /// </summary>
    [Parameter]
    public TimeSpan TimeUntilRetry { get; set; }

    /// <summary>Why the last attempt failed, shown as given: an overlay that only says "lost" cannot be reported.</summary>
    [Parameter]
    public string? Reason { get; set; }

    /// <summary>True while a manual attempt runs: the action shows busy and the spinner gives way to it.</summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Title of the card; the localized title of the state when empty.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Explanation under the title; the localized explanation of the state when empty.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>Label of the action while reconnecting or failed; the package text when empty.</summary>
    [Parameter]
    public string? ReconnectText { get; set; }

    /// <summary>Label of the action once the session is rejected; the package text when empty.</summary>
    [Parameter]
    public string? ReloadText { get; set; }

    /// <summary>Raised by the action while reconnecting or failed: try to reconnect now.</summary>
    [Parameter]
    public EventCallback OnReconnect { get; set; }

    /// <summary>
    /// Raised by the action once the session is rejected. Without a handler the action reloads the
    /// page, which is what a rejected session needs.
    /// </summary>
    [Parameter]
    public EventCallback OnReload { get; set; }

    private bool Visible => State != OmniConnectionState.Connected;

    private string EffectiveId => Id ?? _generatedId;

    private string TitleId => $"{EffectiveId}-title";

    private string DescriptionId => $"{EffectiveId}-description";

    private string StateClass => $"omni-connection-overlay__card--{State.ToString().ToLowerInvariant()}";

    private string EffectiveTitle => !string.IsNullOrWhiteSpace(Title) ? Title : State switch
    {
        OmniConnectionState.Failed => Localize("ConnectionFailedTitle"),
        OmniConnectionState.Rejected => Localize("ConnectionRejectedTitle"),
        _ => Localize("ConnectionLostTitle")
    };

    private string EffectiveDescription => !string.IsNullOrWhiteSpace(Description) ? Description : State switch
    {
        OmniConnectionState.Failed => Localize("ConnectionFailedDescription"),
        OmniConnectionState.Rejected => Localize("ConnectionRejectedDescription"),
        _ => Localize("ConnectionLostDescription")
    };

    private string ActionText => State == OmniConnectionState.Rejected
        ? (!string.IsNullOrWhiteSpace(ReloadText) ? ReloadText : Localize("ConnectionReload"))
        : (!string.IsNullOrWhiteSpace(ReconnectText) ? ReconnectText : Localize("ConnectionReconnectNow"));

    private bool ShowsCountdown => State == OmniConnectionState.Reconnecting && TimeUntilRetry > TimeSpan.Zero;

    private long CountdownSeconds => (long)Math.Ceiling(TimeUntilRetry.TotalSeconds);

    private bool ShowsReason => !string.IsNullOrWhiteSpace(Reason);

    private bool ShowsSpinner => State == OmniConnectionState.Reconnecting && !Busy;

    private async Task ActAsync()
    {
        if (State != OmniConnectionState.Rejected)
        {
            await OnReconnect.InvokeAsync();
        }
        else if (OnReload.HasDelegate)
        {
            await OnReload.InvokeAsync();
        }
        else
        {
            Navigation.Refresh(forceReload: true);
        }
    }

    /// <summary>
    /// Loads the focus script on the first render, while the connection is still up (again later if it
    /// failed), then traps the focus in the card when the overlay shows and gives it back when it hides.
    /// A script that cannot be loaded or run leaves the overlay working without focus handling.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the focus is handled.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The overlay shows when the server may be unreachable, the static files included: importing
        // the focus script only then failed ("Failed to fetch dynamically imported module"), the
        // exception went unhandled and the host showed its fatal error bar over a page that only had
        // to wait. The script is fetched on the first render, while the connection is still up.
        if (firstRender || (Visible && _focusModule is null))
        {
            await TryLoadFocusModuleAsync();
        }

        if (Visible && !_focusActivated && _focusModule is not null)
        {
            // Focus handling is a comfort: without the script the overlay still shows and still acts.
            _focusActivated = await TryInvokeFocusAsync("activateDialog", _card, _focusKey);
        }
        else if (!Visible && _focusActivated && _focusModule is not null)
        {
            _focusActivated = false;
            await TryInvokeFocusAsync("restoreFocus", _focusKey);
        }
    }

    private async Task TryLoadFocusModuleAsync()
    {
        try
        {
            _focusModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
        }
        catch (JSException)
        {
            // Unreachable for now; the next render tries again.
        }
    }

    private async Task<bool> TryInvokeFocusAsync(string identifier, params object?[] arguments)
    {
        try
        {
            await _focusModule!.InvokeVoidAsync(identifier, arguments);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
    }

    /// <summary>Gives the focus back and releases the focus script.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_focusModule is not null)
        {
            try
            {
                if (_focusActivated)
                {
                    await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
                }

                await _focusModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
