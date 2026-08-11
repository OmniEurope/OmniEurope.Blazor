namespace OmniEurope.Blazor.Components;

public partial class OmniContextMenu
{
    private readonly string _focusKey = $"context-menu-{Guid.NewGuid():N}";
    private ElementReference _trigger;
    private ElementReference _popup;
    private IJSObjectReference? _focusModule;
    private bool _focusActivated;

    [CascadingParameter]
    private OmniOverlayCoordinator? Coordinator { get; set; }

    [Parameter]
    public bool Open { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public string MenuLabel { get; set; } = string.Empty;

    private string EffectiveMenuLabel => string.IsNullOrWhiteSpace(MenuLabel)
        ? Localize("ContextMenuLabel")
        : MenuLabel;

    [Parameter, EditorRequired]
    public RenderFragment? TriggerContent { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Open)
        {
            Coordinator?.Register(this, OmniPortalKind.ContextMenu, Popup, () => OpenChanged.InvokeAsync(false));
        }
        else
        {
            Coordinator?.Unregister(this);
        }
    }

    private RenderFragment Popup => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-context-menu__popup");
        builder.AddAttribute(2, "role", "menu");
        builder.AddAttribute(3, "aria-label", EffectiveMenuLabel);
        builder.AddAttribute(4, "tabindex", "-1");
        builder.AddAttribute(5, "autofocus", true);
        builder.AddAttribute(6, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
        builder.AddElementReferenceCapture(7, reference => _popup = reference);
        builder.AddContent(8, ChildContent);
        builder.CloseElement();
    };

    private Task OpenAsync() => OpenChanged.InvokeAsync(true);

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
            await _focusModule.InvokeVoidAsync("activateMenu", _popup, _focusKey);
        }
        else if (!Open && _focusActivated)
        {
            _focusActivated = false;
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "ContextMenu" || (args.ShiftKey && args.Key == "F10"))
        {
            await OpenChanged.InvokeAsync(true);
            return;
        }

        if (args.Key == "Escape")
        {
            await OpenChanged.InvokeAsync(false);
            return;
        }

        if (Open && args.Key is "ArrowDown" or "ArrowUp" or "Home" or "End" && _focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("moveMenuFocus", _popup, args.Key);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Coordinator?.Unregister(this);
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
