namespace OmniEurope.Blazor.Components;

/// <summary>
/// The application's navigation column, an <c>&lt;aside&gt;</c> landmark. It is controlled: it shows
/// <see cref="Open"/> and asks the host to change it through <see cref="OpenChanged"/>. It either pushes
/// the content aside or floats over it (<see cref="Reveal"/>), and when closed either leaves a rail of
/// icons or disappears (<see cref="Collapse"/>). Its open state is cascaded to the menu inside it.
/// </summary>
public partial class OmniSidebar
{
    private IJSObjectReference? _focusModule;
    private DotNetObjectReference<OmniSidebar>? _selfReference;
    private bool _escapeAttached;
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

    /// <summary>Whether opening pushes the content aside or floats over it.</summary>
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
    /// the sidebar pushes, since nothing is covered then.
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
    private bool Smooth => Transition == OmniSidebarTransition.Smooth && Reveal == OmniSidebarReveal.Push;

    /// <summary>A closed sidebar that leaves no rail is out of reach, even while it stays in the page to slide.</summary>
    private bool Concealed => !Open && Collapse == OmniSidebarCollapse.Hidden;

    private bool ShowBackdrop => Open && Backdrop && Reveal == OmniSidebarReveal.Overlay;

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
    private bool Floating => Open && Reveal == OmniSidebarReveal.Overlay;

    /// <summary>
    /// Attaches the document-wide Escape listener when the sidebar starts floating open, and detaches it
    /// when it stops. A lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the listener is attached or detached.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Floating == _escapeAttached)
        {
            return;
        }

        try
        {
            _focusModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.Focus);
            if (Floating)
            {
                _selfReference ??= DotNetObjectReference.Create(this);
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

    /// <summary>Subscribes to navigation, so that choosing an entry closes a floating sidebar.</summary>
    protected override void OnInitialized() => Navigation.LocationChanged += HandleLocationChanged;

    /// <summary>
    /// A floating sidebar covers the page it leads to, so choosing an entry closes it, veil
    /// included. A pushing sidebar stays: it shares the width and hides nothing.
    /// </summary>
    private void HandleLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs args)
    {
        if (Open && Reveal == OmniSidebarReveal.Overlay)
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
