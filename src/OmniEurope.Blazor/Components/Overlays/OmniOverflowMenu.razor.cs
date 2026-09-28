namespace OmniEurope.Blazor.Components;

/// <summary>
/// The "⋮" menu of a row, a card or a page header: a borderless three-dot button that opens a vertical
/// list of <see cref="OmniOverflowMenuItem"/>, each an icon and a label. The trigger opens and closes
/// the menu on its own click; the arrows, Home and End move through the items, Escape closes and gives
/// the focus back to the trigger, Tab closes and goes on to the next control, a press outside closes.
/// The menu stays whole inside the window, to the side or above the trigger when there is no room.
/// </summary>
public partial class OmniOverflowMenu
{
    private readonly string _key = $"overflow-menu-{Guid.NewGuid():N}";
    private ElementReference _root;
    private IJSObjectReference? _module;
    private DotNetObjectReference<DismissInterop>? _dismissReference;
    private RenderFragment? _popupFragment;
    private bool _open;
    private bool _active;
    private bool _focusLast;

    [CascadingParameter]
    private OmniOverlayCoordinator? Coordinator { get; set; }

    /// <summary>The actions, <see cref="OmniOverflowMenuItem"/> in the order they are listed.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Accessible name and tooltip of the trigger, and name of the menu; the localized "More actions" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Size of the trigger; in a data grid row, the row gives it the badge height whatever this says.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Disables the trigger.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen => _open;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("MoreActions") : Label;

    private string MenuId => $"{Id ?? _key}-menu";

    /// <summary>One fragment for the life of the component: the portal renders it far from here.</summary>
    private RenderFragment Popup => _popupFragment ??= BuildPopup;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_open)
        {
            Coordinator?.Register(this, OmniPortalKind.OverflowMenu, Popup, () => CloseAsync(restoreFocus: false));
        }
    }

    private void BuildPopup(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", MenuId);
        builder.AddAttribute(2, "class", "omni-overflow-menu__popup");
        builder.AddAttribute(3, "role", "menu");
        builder.AddAttribute(4, "aria-label", EffectiveLabel);
        builder.AddAttribute(5, "tabindex", "-1");
        builder.OpenComponent<CascadingValue<OmniOverflowMenu>>(6);
        builder.AddComponentParameter(7, nameof(CascadingValue<OmniOverflowMenu>.Value), this);
        builder.AddComponentParameter(8, nameof(CascadingValue<OmniOverflowMenu>.IsFixed), true);
        builder.AddComponentParameter(9, nameof(CascadingValue<OmniOverflowMenu>.ChildContent), ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    private Task ToggleAsync() => _open ? CloseAsync(restoreFocus: true) : OpenAsync(focusLast: false);

    private Task OpenAsync(bool focusLast)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        _open = true;
        _focusLast = focusLast;
        Coordinator?.Register(this, OmniPortalKind.OverflowMenu, Popup, () => CloseAsync(restoreFocus: false));
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Closes the menu. <paramref name="restoreFocus"/> gives the focus back to the trigger at once, so
    /// a dialog the chosen action opens next finds it there and gives it back when it closes.
    /// </summary>
    internal async Task CloseAsync(bool restoreFocus)
    {
        if (!_open)
        {
            return;
        }

        _open = false;
        _active = false;
        Coordinator?.Unregister(this);
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("closeOverflowMenu", _key, restoreFocus);
            }
            catch (JSDisconnectedException)
            {
            }
        }

        StateHasChanged();
    }

    private Task HandleTriggerKeyDownAsync(KeyboardEventArgs args) => !_open && args.Key is "ArrowDown" or "ArrowUp"
        ? OpenAsync(focusLast: args.Key == "ArrowUp")
        : Task.CompletedTask;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_open || _active)
        {
            return;
        }

        _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
        if (!_open || _active)
        {
            return;
        }

        _active = true;
        _dismissReference ??= DotNetObjectReference.Create(new DismissInterop(restore => InvokeAsync(() => CloseAsync(restore))));
        await _module.InvokeVoidAsync("openOverflowMenu", MenuId, _key, _root, _dismissReference, _focusLast);
    }

    public async ValueTask DisposeAsync()
    {
        Coordinator?.Unregister(this);
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("closeOverflowMenu", _key, false);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _dismissReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>What the script calls when a press outside, Escape or Tab closes the menu.</summary>
    private sealed class DismissInterop(Func<bool, Task> dismiss)
    {
        [JSInvokable("OmniOverflowMenu.Dismiss")]
        public Task DismissAsync(bool restoreFocus) => dismiss(restoreFocus);
    }
}
