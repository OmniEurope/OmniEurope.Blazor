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

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? Footer { get; set; }

    [Parameter]
    public ElementReference? ReturnFocusTo { get; set; }

    private string EffectiveId => Id ?? _generatedId;
    private string TitleId => $"{EffectiveId}-title";

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
            await _focusModule.InvokeVoidAsync("activateDialog", _dialog, _focusKey);
        }
        else if (!Open && _focusActivated)
        {
            _focusActivated = false;
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    private Task HandleBackdropAsync() => CloseOnBackdrop ? CloseAsync() : Task.CompletedTask;
    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && CloseOnEscape)
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
