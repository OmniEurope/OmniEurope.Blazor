namespace OmniEurope.Blazor.Components;

public partial class OmniContextMenu
{
    private readonly string _focusKey = $"context-menu-{Guid.NewGuid():N}";
    private RenderFragment? _popupFragment;
    private ElementReference _trigger;
    private IJSObjectReference? _focusModule;
    private DotNetObjectReference<DismissInterop>? _dismissReference;
    private bool _menuActive;
    private bool _placementPending;
    private double? _pointerX;
    private double? _pointerY;

    /// <summary>
    /// One fragment for the whole life of the component: the portal renders it far from here, and it
    /// reads the current items each time it runs.
    /// </summary>
    private RenderFragment Popup => _popupFragment ??= BuildPopup;

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

    /// <summary>
    /// The popup is found by this id rather than by an element reference: rendered by the portal, it
    /// is not an element this component captures.
    /// </summary>
    private string PopupId => $"{Id ?? _focusKey}-menu";

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Open)
        {
            Coordinator?.Register(this, OmniPortalKind.ContextMenu, Popup, CloseAsync);
        }
        else
        {
            Coordinator?.Unregister(this);
        }
    }

    private void BuildPopup(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", PopupId);
        builder.AddAttribute(2, "class", "omni-context-menu__popup");
        builder.AddAttribute(3, "role", "menu");
        builder.AddAttribute(4, "aria-label", EffectiveMenuLabel);
        builder.AddAttribute(5, "tabindex", "-1");
        // Carried by the popup as well: in the overlay portal it no longer sits inside the trigger.
        builder.AddAttribute(6, "data-omni-density", OmniDensityAttribute.Of(Density));
        builder.AddAttribute(7, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
        // Rendered in place, the popup sits inside the trigger: one key must not be handled twice.
        builder.AddEventStopPropagationAttribute(8, "onkeydown", true);
        builder.AddContent(9, ChildContent);
        builder.CloseElement();
    }

    /// <summary>Opens at the pointer; a right-click while open moves the menu there.</summary>
    private Task OpenAtPointerAsync(MouseEventArgs args)
    {
        _pointerX = args.ClientX;
        _pointerY = args.ClientY;
        return RequestOpenAsync();
    }

    /// <summary>From the keyboard, the menu opens under its trigger.</summary>
    private Task OpenFromKeyboardAsync()
    {
        _pointerX = null;
        _pointerY = null;
        return RequestOpenAsync();
    }

    private async Task RequestOpenAsync()
    {
        _placementPending = true;
        if (Open)
        {
            StateHasChanged();
            return;
        }

        await OpenChanged.InvokeAsync(true);
    }

    private Task CloseAsync() => OpenChanged.InvokeAsync(false);

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

        if (Open && (!_menuActive || _placementPending))
        {
            _menuActive = true;
            _placementPending = false;
            _dismissReference ??= DotNetObjectReference.Create(new DismissInterop(() => InvokeAsync(CloseAsync)));
            await _focusModule.InvokeVoidAsync("openContextMenu", PopupId, _focusKey, _trigger, _pointerX, _pointerY, _dismissReference);
        }
        else if (!Open && _menuActive)
        {
            _menuActive = false;
            await _focusModule.InvokeVoidAsync("closeContextMenu", _focusKey);
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "ContextMenu" || (args.ShiftKey && args.Key == "F10"))
        {
            await OpenFromKeyboardAsync();
            return;
        }

        if (args.Key == "Escape")
        {
            await CloseAsync();
            return;
        }

        if (Open && args.Key is "ArrowDown" or "ArrowUp" or "Home" or "End" && _focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("moveContextMenuFocus", PopupId, args.Key);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Coordinator?.Unregister(this);
        if (_focusModule is not null)
        {
            try
            {
                await _focusModule.InvokeVoidAsync("closeContextMenu", _focusKey);
                await _focusModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _dismissReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>What the script calls when a press lands outside the open menu.</summary>
    private sealed class DismissInterop(Func<Task> dismiss)
    {
        [JSInvokable("OmniContextMenu.Dismiss")]
        public Task DismissAsync() => dismiss();
    }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
