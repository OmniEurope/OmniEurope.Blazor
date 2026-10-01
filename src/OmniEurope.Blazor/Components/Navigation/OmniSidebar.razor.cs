namespace OmniEurope.Blazor.Components;

/// <summary>
/// The application's navigation column, an <c>&lt;aside&gt;</c> landmark. It is controlled: it shows
/// <see cref="Open"/> and asks the host to change it through <see cref="OpenChanged"/>. It either pushes
/// the content aside or floats over it (<see cref="Reveal"/>), and when closed either leaves a rail of
/// icons or disappears (<see cref="Collapse"/>). On a phone (under 40rem) a pushing sidebar the host
/// can close (<see cref="OpenChanged"/> bound) floats over the content instead, with its veil, since
/// pushing would leave the page a sliver of width. Its open state is cascaded to the menu inside it.
/// </summary>
public partial class OmniSidebar
{
    private IJSObjectReference? _focusModule;
    private DotNetObjectReference<OmniSidebar>? _selfReference;
    private bool _escapeAttached;
    private bool _narrowWatched;
    private bool _narrow;
    private readonly string _escapeKey = Guid.NewGuid().ToString("N");

    /// <summary>The content of the sidebar, typically an <see cref="OmniPanelMenu"/>. Required.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// True when the sidebar is open. False by default. The sidebar never changes it itself: it raises
    /// <see cref="OpenChanged"/> and waits for the host to pass the new value back.
    /// </summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>
    /// Raised when the sidebar closes itself: the veil was clicked, Escape was pressed while it floats
    /// open, or an entry was chosen. Without a handler the veil still renders and still swallows the
    /// click, so a floating sidebar that can be dismissed needs this bound. Raised with true when a
    /// group is clicked on the rail: the sidebar opens on that group, unfolded.
    /// </summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The side of the page the sidebar sits on. <see cref="OmniSidebarPosition.Start"/> by default.</summary>
    [Parameter]
    public OmniSidebarPosition Position { get; set; }

    /// <summary>
    /// Whether opening pushes the content aside or floats over it. A pushing sidebar floats on a phone
    /// (under 40rem), veil included, once the page is interactive, when <see cref="OpenChanged"/> is bound:
    /// one the host never closes keeps pushing, since a veil nothing can lift would lock the page.
    /// </summary>
    [Parameter]
    public OmniSidebarReveal Reveal { get; set; } = OmniSidebarReveal.Push;

    /// <summary>Whether closing removes the sidebar or leaves a rail of icons.</summary>
    [Parameter]
    public OmniSidebarCollapse Collapse { get; set; } = OmniSidebarCollapse.Icons;

    /// <summary>
    /// Whether a pushing sidebar opens and closes at once or slides. Smooth keeps a closed sidebar in
    /// the page, reduced to no width and hidden from assistive technology, so the width it gives back
    /// can be animated.
    /// </summary>
    [Parameter]
    public OmniSidebarTransition Transition { get; set; } = OmniSidebarTransition.Smooth;

    /// <summary>
    /// Dims the content behind an open floating sidebar and closes it on click. Meaningless while
    /// the sidebar pushes, since nothing is covered then; a pushing sidebar that floats on a phone
    /// always has its veil.
    /// </summary>
    [Parameter]
    public bool Backdrop { get; set; }

    /// <summary>
    /// What the application header shows over the sidebar's column, typically the toggle and the
    /// brand, rendered at the top of the panel while it floats open. Given this, an open floating
    /// panel rises to the top of the viewport instead of starting under the header: the veil then
    /// dims the rest of the header but not the part the menu belongs to, and since the copy sits
    /// where the original was, nothing moves as the panel opens. Without it, the panel starts under
    /// the header as before.
    /// </summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>Accessible name of the sidebar landmark. Null, the default, is the localized "Navigation".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Accessible name and tooltip of the veil that closes an open floating sidebar. Null, the default, is the localized "Close navigation".</summary>
    [Parameter]
    public string? CloseLabel { get; set; }

    /// <summary>
    /// A closed sidebar still renders when it leaves a rail behind: the rail is what is left of it,
    /// not a separate control.
    /// </summary>
    private bool Rendered => Open || Collapse == OmniSidebarCollapse.Icons || Smooth;

    /// <summary>The smooth transition applies to the push mode only.</summary>
    private bool Smooth => Transition == OmniSidebarTransition.Smooth && EffectiveReveal == OmniSidebarReveal.Push;

    /// <summary>The mode in force: <see cref="Reveal"/>, except a pushing sidebar the host can close, which floats on a phone.</summary>
    private OmniSidebarReveal EffectiveReveal => _narrow && FloatsOnPhone ? OmniSidebarReveal.Overlay : Reveal;

    /// <summary>A pushing sidebar floats on a phone only when the host can close it.</summary>
    private bool FloatsOnPhone => Reveal == OmniSidebarReveal.Push && OpenChanged.HasDelegate;

    /// <summary>A closed sidebar that leaves no rail is out of reach, even while it stays in the page to slide.</summary>
    private bool Concealed => !Open && Collapse == OmniSidebarCollapse.Hidden;

    private bool ShowBackdrop => Open && EffectiveReveal == OmniSidebarReveal.Overlay && (Backdrop || FloatsOnPhone);

    /// <summary>
    /// Cached so the cascaded state compares equal between renders while nothing changed: a new
    /// delegate on every render would re-render the whole menu for nothing.
    /// </summary>
    private Func<Task>? _expand;

    private OmniSidebarState State => new(Open, Collapse) { Expand = OpenChanged.HasDelegate ? _expand ??= () => OpenChanged.InvokeAsync(true) : null };

    private string EffectiveLabel => LocalizeOr(Label, "SidebarLabel");

    private string EffectiveCloseLabel => LocalizeOr(CloseLabel, "SidebarClose");

    private Task CloseAsync() => OpenChanged.InvokeAsync(false);

    /// <summary>A floating sidebar listens for Escape on the whole document while it is open.</summary>
    private bool Floating => Open && EffectiveReveal == OmniSidebarReveal.Overlay;

    /// <summary>
    /// Watches the phone width while the sidebar pushes, attaches the document-wide Escape listener when
    /// the sidebar starts floating open, and detaches it when it stops. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the listeners are attached or detached.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var watchesNarrow = FloatsOnPhone;
        if (Floating == _escapeAttached && watchesNarrow == _narrowWatched)
        {
            return;
        }

        try
        {
            _focusModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Focus);
            _selfReference ??= DotNetObjectReference.Create(this);
            if (watchesNarrow != _narrowWatched)
            {
                _narrowWatched = watchesNarrow;
                await _focusModule.InvokeVoidAsync(watchesNarrow ? "watchNarrow" : "unwatchNarrow", _escapeKey, _selfReference);
                _narrow &= watchesNarrow;
            }

            if (Floating == _escapeAttached)
            {
                return;
            }

            if (Floating)
            {
                await _focusModule.InvokeVoidAsync("attachEscape", _escapeKey, _selfReference);
            }
            else
            {
                await _focusModule.InvokeVoidAsync("detachEscape", _escapeKey);
            }

            _escapeAttached = Floating;
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Called by the document listener; closes only a sidebar that still floats open.</summary>
    [JSInvokable]
    public Task CloseFromEscapeAsync() => Floating ? InvokeAsync(CloseAsync) : Task.CompletedTask;

    /// <summary>Called by the width watch: whether the page is phone width (under 40rem).</summary>
    /// <param name="narrow">True under 40rem.</param>
    /// <returns>A task that completes once the sidebar is drawn in the mode that fits.</returns>
    [JSInvokable]
    public Task SetNarrowAsync(bool narrow)
    {
        if (narrow == _narrow)
        {
            return Task.CompletedTask;
        }

        _narrow = narrow;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Subscribes to navigation, so that choosing an entry closes a floating sidebar.</summary>
    protected override void OnInitialized() => Navigation.LocationChanged += HandleLocationChanged;

    /// <summary>
    /// A floating sidebar covers the page it leads to, so choosing an entry closes it, veil
    /// included. A pushing sidebar stays: it shares the width and hides nothing.
    /// </summary>
    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        if (Open && EffectiveReveal == OmniSidebarReveal.Overlay)
        {
            _ = InvokeAsync(CloseAsync);
        }
    }

    /// <summary>Unsubscribes from navigation.</summary>
    public void Dispose() => Navigation.LocationChanged -= HandleLocationChanged;

    /// <summary>
    /// Unsubscribes from navigation, detaches the Escape listener if it is attached, and releases the
    /// script module and the .NET reference. A lost circuit is ignored.
    /// </summary>
    /// <returns>A task that completes once the script resources are released.</returns>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        try
        {
            if (_focusModule is not null)
            {
                if (_escapeAttached)
                {
                    await _focusModule.InvokeVoidAsync("detachEscape", _escapeKey);
                }

                if (_narrowWatched)
                {
                    await _focusModule.InvokeVoidAsync("unwatchNarrow", _escapeKey);
                }

                await _focusModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
        }

        _selfReference?.Dispose();
        GC.SuppressFinalize(this);
    }
}
