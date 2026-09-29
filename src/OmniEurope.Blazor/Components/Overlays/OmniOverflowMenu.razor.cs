namespace OmniEurope.Blazor.Components;

/// <summary>
/// The "⋮" menu of a row, a card or a page header: a borderless three-dot button that opens a vertical
/// list of <see cref="OmniMenuItem"/>, each an icon and a label. The trigger opens and closes the menu
/// on its own click, ArrowDown or ArrowUp open it on its first or last item; in the menu the arrows,
/// Home and End move through the items, Escape closes and gives the focus back to the trigger, Tab
/// closes and goes on to the next control, a press outside closes. The menu stays whole inside the
/// window, to the side or above the trigger when there is no room.
/// </summary>
public partial class OmniOverflowMenu
{
    private readonly string _generatedId = $"omni-overflow-menu-{Guid.NewGuid():N}";
    private ElementReference _root;
    private OmniMenuController? _menu;
    private RenderFragment? _popupFragment;

    [CascadingParameter]
    private OmniOverlayCoordinator? Coordinator { get; set; }

    /// <summary>The actions, <see cref="OmniMenuItem"/> in the order they are listed.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Accessible name and tooltip of the trigger. Null, the default, is the localized "More actions".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Accessible name of the open menu. Null, the default, repeats the trigger's <see cref="Label"/>.</summary>
    [Parameter]
    public string? MenuLabel { get; set; }

    /// <summary>Size of the trigger; in a data grid row, the row gives it the badge height whatever this says.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Disables the trigger: the menu does not open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the menu is open, for a host that controls it (<c>@bind-Open</c>). Null, the default,
    /// leaves the menu its own state.
    /// </summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Raised with the new state when the menu opens or closes, whether the host controls it or not.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private OmniMenuController Menu => _menu ??= new OmniMenuController(JavaScript, restore => InvokeAsync(() => CloseAsync(restore)));

    private bool IsOpen => Menu.IsOpen(Open);

    private string EffectiveLabel => LocalizeOr(Label, "MoreActions");

    private string MenuId => $"{Id ?? _generatedId}-menu";

    /// <summary>One fragment for the life of the component: the portal renders it far from here.</summary>
    private RenderFragment Popup => _popupFragment ??= builder => OmniMenuController.BuildMenuList(
        builder,
        this,
        MenuId,
        "omni-menu omni-overflow-menu__popup",
        string.IsNullOrWhiteSpace(MenuLabel) ? EffectiveLabel : MenuLabel,
        null,
        this,
        ChildContent,
        HandleMenuKeyDownAsync);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SyncPortal();
    }

    private void SyncPortal()
    {
        if (IsOpen)
        {
            Coordinator?.Register(this, OmniPortalKind.OverflowMenu, Popup, () => CloseAsync(restoreFocus: false));
        }
        else
        {
            Coordinator?.Unregister(this);
        }
    }

    private void Refresh()
    {
        SyncPortal();
        StateHasChanged();
    }

    private Task ToggleAsync() => IsOpen ? CloseAsync(restoreFocus: true) : OpenAsync(focusLast: false);

    private Task OpenAsync(bool focusLast)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        Menu.RequestPlacement(focusLast);
        return Menu.SetOpenAsync(true, Open, OpenChanged, Refresh);
    }

    /// <summary>
    /// Closes the menu. <paramref name="restoreFocus"/> gives the focus back to the trigger at once, so
    /// a dialog the chosen action opens next finds it there and gives it back when it closes.
    /// </summary>
    Task IOmniMenu.CloseAsync(bool restoreFocus) => CloseAsync(restoreFocus);

    private Task CloseAsync(bool restoreFocus) => Menu.SetOpenAsync(false, Open, OpenChanged, Refresh, restoreFocus);

    private Task HandleTriggerKeyDownAsync(KeyboardEventArgs args) => !IsOpen && args.Key is "ArrowDown" or "ArrowUp"
        ? OpenAsync(focusLast: args.Key == "ArrowUp")
        : Task.CompletedTask;

    private Task HandleMenuKeyDownAsync(KeyboardEventArgs args) => Menu.HandleMenuKeyAsync(args, MenuId, CloseAsync);

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        Menu.SyncAsync(IsOpen, MenuId, _root, OmniMenuPlacement.End);

    /// <summary>Takes the menu out of the portal and detaches the script.</summary>
    public async ValueTask DisposeAsync()
    {
        Coordinator?.Unregister(this);
        if (_menu is not null)
        {
            await _menu.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
