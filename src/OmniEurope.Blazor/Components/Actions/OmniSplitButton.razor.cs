namespace OmniEurope.Blazor.Components;

/// <summary>
/// A button split in two: the main part runs the main action (<see cref="OnClick"/>), the chevron
/// opens a menu of <see cref="OmniMenuItem"/> for the others. The chevron opens and closes the menu on
/// its own click, ArrowDown or ArrowUp open it on its first or last item; in the menu the arrows, Home
/// and End move, Escape closes and gives the focus back to the chevron, Tab and a press outside close,
/// choosing an item closes before its action runs.
/// </summary>
public partial class OmniSplitButton
{
    private readonly string _generatedId = $"omni-split-button-{Guid.NewGuid():N}";
    private ElementReference _root;
    private OmniMenuController? _menu;
    private RenderFragment? _popupFragment;

    [CascadingParameter]
    private OmniOverlayCoordinator? Coordinator { get; set; }

    /// <summary>The text of the main part.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Accessible name of the chevron and of the menu it opens. Null, the default, is the localized
    /// "More options".
    /// </summary>
    [Parameter]
    public string? MenuLabel { get; set; }

    /// <summary>
    /// Accessible name of the main part, for an icon-only split button (an empty <see cref="Text"/> in a grid
    /// action column). Null by default: the main part is named by its <see cref="Text"/>.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The same three sizes as <see cref="OmniButton"/>, main part and menu part together.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>
    /// What the button means, main part and menu part together, with the fills of
    /// <see cref="OmniButton"/>. <see cref="OmniButtonVariant.Secondary"/> by default: the surface,
    /// border and text colours the split button has always had, rendered without a variant class.
    /// </summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    /// <summary>
    /// Icon drawn before <see cref="Text"/> in the main part, in general an <see cref="OmniIcon"/>, like the
    /// <c>Icon</c> of the other components. None by default; the menu part keeps its chevron either way.
    /// The split button sizes it to its own size (small in a small split button, medium otherwise); a
    /// size set on the icon wins.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Disables both parts: the main action does not run and the menu does not open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Marks the button as working: both parts keep their size and stay focusable, announce
    /// <c>aria-busy</c>, and neither runs the main action nor opens the menu.
    /// </summary>
    [Parameter]
    public bool Busy { get; set; }

    /// <summary>Raised with the click of the main part.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>The other actions, <see cref="OmniMenuItem"/> in the order they are listed.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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

    private string EffectiveMenuLabel => LocalizeOr(MenuLabel, "SplitButtonMenuLabel");

    private string MenuId => $"{Id ?? _generatedId}-menu";

    private string? VariantClass => Variant == OmniButtonVariant.Secondary
        ? null
        : $"omni-split-button--{Variant.ToString().ToLowerInvariant()}";

    /// <summary>One fragment for the life of the component: the portal renders it far from here.</summary>
    private RenderFragment Popup => _popupFragment ??= builder => OmniMenuController.BuildMenuList(
        builder,
        this,
        MenuId,
        "omni-menu omni-split-button__menu",
        EffectiveMenuLabel,
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
            Coordinator?.Register(this, OmniPortalKind.SplitButtonMenu, Popup, () => CloseAsync(restoreFocus: false));
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

    private Task InvokeMainAsync(MouseEventArgs args) => Disabled || Busy ? Task.CompletedTask : OnClick.InvokeAsync(args);

    private Task ToggleMenuAsync() => IsOpen ? CloseAsync(restoreFocus: true) : OpenAsync(focusLast: false);

    // A busy button stays focusable, so the keyboard would otherwise open the menu that disabled blocks.
    private Task OpenAsync(bool focusLast)
    {
        if (Disabled || Busy)
        {
            return Task.CompletedTask;
        }

        Menu.RequestPlacement(focusLast);
        return Menu.SetOpenAsync(true, Open, OpenChanged, Refresh);
    }

    Task IOmniMenu.CloseAsync(bool restoreFocus) => CloseAsync(restoreFocus);

    private Task CloseAsync(bool restoreFocus) => Menu.SetOpenAsync(false, Open, OpenChanged, Refresh, restoreFocus);

    private Task HandleToggleKeyDownAsync(KeyboardEventArgs args) => !IsOpen && args.Key is "ArrowDown" or "ArrowUp"
        ? OpenAsync(focusLast: args.Key == "ArrowUp")
        : Task.CompletedTask;

    private Task HandleMenuKeyDownAsync(KeyboardEventArgs args) => Menu.HandleMenuKeyAsync(args, MenuId, CloseAsync);

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) =>
        Menu.SyncAsync(IsOpen, MenuId, _root, OmniMenuPlacement.Start);

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
