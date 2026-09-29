namespace OmniEurope.Blazor.Components;

/// <summary>
/// A menu of <see cref="OmniMenuItem"/> opened on a region, the <see cref="TriggerContent"/>: a
/// right-click opens it at the pointer (a second one moves it), the ContextMenu key or Shift+F10
/// opens it under the region. In the menu the arrows, Home and End move through the items, Escape
/// closes and gives the focus back, Tab and a press outside close. The menu is drawn in the overlay
/// portal of <see cref="OmniComponentsHost"/> and kept inside the window.
/// </summary>
public partial class OmniContextMenu
{
    private readonly string _generatedId = $"omni-context-menu-{Guid.NewGuid():N}";
    private RenderFragment? _popupFragment;
    private ElementReference _trigger;
    private OmniMenuController? _menu;

    [CascadingParameter]
    private OmniOverlayCoordinator? Coordinator { get; set; }

    /// <summary>
    /// Whether the menu is open, for a host that controls it (<c>@bind-Open</c>). Null, the default,
    /// leaves the menu its own state.
    /// </summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Raised with the new state when the menu opens or closes, whether the host controls it or not.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Accessible name of the open menu. Null, the default, is the localized "Context menu".</summary>
    [Parameter]
    public string? MenuLabel { get; set; }

    /// <summary>The region the menu belongs to: a right-click or the context-menu keys on it open the menu.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? TriggerContent { get; set; }

    /// <summary>The items, <see cref="OmniMenuItem"/> in the order they are listed.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Leaves the region without its menu: the browser's own context menu shows instead.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }

    private OmniMenuController Menu => _menu ??= new OmniMenuController(JavaScript, restore => InvokeAsync(() => CloseAsync(restore)));

    private bool IsOpen => Menu.IsOpen(Open);

    /// <summary>
    /// The menu is found by this id rather than by an element reference: rendered by the portal, it
    /// is not an element this component captures.
    /// </summary>
    private string MenuId => $"{Id ?? _generatedId}-menu";

    /// <summary>
    /// One fragment for the whole life of the component: the portal renders it far from here, and it
    /// reads the current items each time it runs.
    /// </summary>
    private RenderFragment Popup => _popupFragment ??= builder => OmniMenuController.BuildMenuList(
        builder,
        this,
        MenuId,
        "omni-menu omni-context-menu__popup",
        LocalizeOr(MenuLabel, "ContextMenuLabel"),
        OmniDensityAttribute.Of(Density),
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
            Coordinator?.Register(this, OmniPortalKind.ContextMenu, Popup, () => CloseAsync(restoreFocus: false));
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

    /// <summary>Opens at the pointer; a right-click while open moves the menu there.</summary>
    private Task OpenAtPointerAsync(MouseEventArgs args) => OpenAsync(args.ClientX, args.ClientY);

    private Task OpenAsync(double? x, double? y)
    {
        if (Disabled)
        {
            return Task.CompletedTask;
        }

        Menu.RequestPlacement(focusLast: false, x, y);
        return Menu.SetOpenAsync(true, Open, OpenChanged, Refresh);
    }

    Task IOmniMenu.CloseAsync(bool restoreFocus) => CloseAsync(restoreFocus);

    private Task CloseAsync(bool restoreFocus) => Menu.SetOpenAsync(false, Open, OpenChanged, Refresh, restoreFocus);

    /// <summary>From the keyboard, the menu opens under its trigger.</summary>
    private Task HandleTriggerKeyDownAsync(KeyboardEventArgs args) => args.Key is "ContextMenu" || (args.ShiftKey && args.Key == "F10")
        ? OpenAsync(null, null)
        : Task.CompletedTask;

    private Task HandleMenuKeyDownAsync(KeyboardEventArgs args) => Menu.HandleMenuKeyAsync(args, MenuId, CloseAsync);

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        Menu.SyncAsync(IsOpen, MenuId, _trigger, OmniMenuPlacement.Pointer);

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
