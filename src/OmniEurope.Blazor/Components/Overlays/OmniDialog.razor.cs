namespace OmniEurope.Blazor.Components;

public partial class OmniDialog
{
    private readonly string _focusKey = $"dialog-{Guid.NewGuid():N}";
    private readonly string _generatedId = $"omni-dialog-{Guid.NewGuid():N}";
    private ElementReference _dialog;
    private IJSObjectReference? _focusModule;
    private bool _focusActivated;

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter, EditorRequired]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string CloseLabel { get; set; } = string.Empty;

    private string EffectiveCloseLabel => string.IsNullOrWhiteSpace(CloseLabel)
        ? Localize("Close")
        : CloseLabel;

    [Parameter]
    public bool CloseOnBackdrop { get; set; } = true;

    [Parameter]
    public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Whether the reader can dismiss the dialog. True by default, as before. False removes the
    /// close button, ignores Escape and the backdrop whatever <see cref="CloseOnEscape"/> and
    /// <see cref="CloseOnBackdrop"/> say, and announces the panel as an <c>alertdialog</c>
    /// described by its content: only the host, through <see cref="Open"/>, closes it. Focus stays
    /// trapped inside it; with nothing focusable in its content, the panel itself takes focus.
    /// </summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Footer { get; set; }

    [Parameter]
    public ElementReference? ReturnFocusTo { get; set; }

    private string EffectiveId => Id ?? _generatedId;
    private string TitleId => $"{EffectiveId}-title";
    private string ContentId => $"{EffectiveId}-content";

    private async Task CloseAsync()
    {
        await OpenChanged.InvokeAsync(false);
        if (ReturnFocusTo is { } target)
        {
            await target.FocusAsync();
        }
        else if (_focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _focusModule = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
        }

        if (_focusModule is null)
        {
            return;
        }

        if (Open && !_focusActivated)
        {
            _focusActivated = true;

            // A backdrop that closes nothing must not take focus out of the trap either. Only such a
            // dialog passes the flag: a dialog whose backdrop closes it makes the call it always made.
            if (CloseOnBackdrop && Dismissible)
            {
                await _focusModule.InvokeVoidAsync("activateDialog", _dialog, _focusKey);
            }
            else
            {
                await _focusModule.InvokeVoidAsync("activateDialog", _dialog, _focusKey, true);
            }
        }
        else if (!Open && _focusActivated)
        {
            _focusActivated = false;
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    private Task HandleBackdropAsync() => CloseOnBackdrop && Dismissible ? CloseAsync() : Task.CompletedTask;
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && CloseOnEscape && Dismissible)
        {
            await CloseAsync();
        }
    }

    private Task FocusFirstAsync(FocusEventArgs _) => FocusBoundaryAsync(last: false);
    private Task FocusLastAsync(FocusEventArgs _) => FocusBoundaryAsync(last: true);

    private async Task FocusBoundaryAsync(bool last)
    {
        if (_focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("focusBoundary", _dialog, last);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_focusModule is not null)
        {
            try
            {
                await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
                await _focusModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
