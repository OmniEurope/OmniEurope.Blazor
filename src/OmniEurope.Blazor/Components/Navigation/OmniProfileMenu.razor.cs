namespace OmniEurope.Blazor.Components;

/// <summary>
/// The account menu of an application header: an avatar (or a <see cref="Summary"/>) that opens a
/// menu of <see cref="OmniMenuItem"/>, under an optional identity <see cref="Header"/>. The trigger
/// opens and closes the menu on its own click, ArrowDown or ArrowUp open it on its first or last item;
/// in the menu the arrows, Home and End move, Escape closes and gives the focus back to the trigger,
/// Tab closes, a press outside closes unless <see cref="CloseOnOutsideClick"/> is off.
/// </summary>
public partial class OmniProfileMenu
{
    private readonly string _generatedId = $"omni-profile-menu-{Guid.NewGuid():N}";
    private ElementReference _root;
    private OmniMenuController? _menu;
    private RenderFragment? _popupFragment;

    [CascadingParameter]
    private OmniOverlayCoordinator? Coordinator { get; set; }

    /// <summary>
    /// The accessible name of the trigger. Null, the default, is the localized "Profile menu".
    /// With the avatar trigger it is the only name the trigger has, so it should name the account.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Accessible name of the open menu. Null, the default, repeats the trigger's <see cref="Label"/>.</summary>
    [Parameter]
    public string? MenuLabel { get; set; }

    /// <summary>
    /// What the trigger shows. Null, the default, draws the avatar: a round disc holding
    /// <see cref="Initials"/>, or the user glyph without them. The trigger is a button, so the fragment
    /// holds no interactive content; it is named by <see cref="Label"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? Summary { get; set; }

    /// <summary>
    /// A few letters (typically one to three) drawn in the avatar disc, of the trigger and of the <see cref="Header"/>,
    /// in place of the user glyph. Decorative: the trigger is named by <see cref="Label"/>.
    /// </summary>
    [Parameter]
    public string? Initials { get; set; }

    /// <summary>
    /// The identity shown at the top of the open menu, beside a large avatar disc: its first element
    /// reads as the name, the next ones as muted details (a role, an organisation, a link to the
    /// profile). Rendered outside the <c>role="menu"</c> list. Null, the default, renders no header.
    /// </summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>The items, <see cref="OmniMenuItem"/> in the order they are listed; an item's icon is drawn in a disc.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Closes the open menu when a press lands outside it, which is what a menu is expected to do.
    /// On by default; a page that drives the menu from controls of its own can turn it off, or mark
    /// those controls with <c>data-omni-keep-open</c> so a press on them never counts as outside.
    /// Escape, Tab and choosing an item close the menu either way.
    /// </summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

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

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }

    private OmniMenuController Menu => _menu ??= new OmniMenuController(JavaScript, restore => InvokeAsync(() => CloseAsync(restore)));

    private bool IsOpen => Menu.IsOpen(Open);

    private string EffectiveLabel => LocalizeOr(Label, "ProfileMenuLabel");

    private string MenuId => $"{Id ?? _generatedId}-menu";

    /// <summary>The avatar disc's content: the initials, or the user glyph.</summary>
    private RenderFragment AvatarContent => builder =>
    {
        if (string.IsNullOrWhiteSpace(Initials))
        {
            builder.OpenComponent<OmniIcon>(0);
            builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.User);
            builder.CloseComponent();
        }
        else
        {
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "class", "omni-profile-menu__initials");
            builder.AddContent(4, Initials.Trim());
            builder.CloseElement();
        }
    };

    /// <summary>
    /// One fragment for the life of the component: the portal renders it far from here. With a
    /// <see cref="Header"/> the surface holds the identity, then the list; without one the list is
    /// the surface.
    /// </summary>
    private RenderFragment Popup => _popupFragment ??= builder =>
    {
        var label = string.IsNullOrWhiteSpace(MenuLabel) ? EffectiveLabel : MenuLabel;
        var density = OmniDensityAttribute.Of(Density);
        if (Header is null)
        {
            builder.OpenRegion(0);
            OmniMenuController.BuildMenuList(builder, this, MenuId, "omni-menu omni-profile-menu__popup", label, density, this, ChildContent, HandleMenuKeyDownAsync);
            builder.CloseRegion();
            return;
        }

        builder.OpenElement(1, "div");
        builder.AddAttribute(2, "class", "omni-menu omni-profile-menu__popup omni-profile-menu__popup--header");
        builder.AddAttribute(3, "data-omni-menu-surface", string.Empty);
        builder.AddAttribute(4, "data-omni-density", density);
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "class", "omni-profile-menu__header");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "class", "omni-disc omni-disc--large omni-profile-menu__avatar");
        builder.AddAttribute(9, "aria-hidden", "true");
        builder.AddContent(10, AvatarContent);
        builder.CloseElement();
        builder.OpenElement(11, "div");
        builder.AddAttribute(12, "class", "omni-profile-menu__identity");
        builder.AddContent(13, Header);
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenRegion(14);
        OmniMenuController.BuildMenuList(builder, this, MenuId, "omni-profile-menu__items", label, null, this, ChildContent, HandleMenuKeyDownAsync);
        builder.CloseRegion();
        builder.CloseElement();
    };

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
            Coordinator?.Register(this, OmniPortalKind.ProfileMenu, Popup, () => CloseAsync(restoreFocus: false));
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

    Task IOmniMenu.CloseAsync(bool restoreFocus) => CloseAsync(restoreFocus);

    private Task CloseAsync(bool restoreFocus) => Menu.SetOpenAsync(false, Open, OpenChanged, Refresh, restoreFocus);

    private Task HandleTriggerKeyDownAsync(KeyboardEventArgs args) => !IsOpen && args.Key is "ArrowDown" or "ArrowUp"
        ? OpenAsync(focusLast: args.Key == "ArrowUp")
        : Task.CompletedTask;

    private Task HandleMenuKeyDownAsync(KeyboardEventArgs args) => Menu.HandleMenuKeyAsync(args, MenuId, CloseAsync);

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        Menu.SyncAsync(IsOpen, MenuId, _root, OmniMenuPlacement.End, CloseOnOutsideClick);

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
