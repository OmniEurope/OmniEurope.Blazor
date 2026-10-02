namespace OmniEurope.Blazor.Components;

/// <summary>
/// The header of a page, as one block of fixed height so every page starts its content at the same
/// place. Line 1: back button, icon, title, badges, actions and a "⋮" menu. Line 2, always reserved:
/// the breadcrumb, its last item being the title, or the subtitle when the page has no ancestor. Under
/// the block: the subtitle when line 2 carries the breadcrumb, then a row of filters.
/// </summary>
/// <remarks>
/// <para>
/// The breadcrumb and, when no <see cref="Title"/> is given, the title itself come from
/// <see cref="OmniBreadcrumbService"/>, so a page names itself through its route and renames itself by
/// updating the service.
/// </para>
/// <para>
/// A title longer than its line is not cut: it scrolls sideways on one line, and a chevron button
/// shows on each side that still has text (the module <c>omni-page-header.js</c> measures it and marks
/// the block; it writes no style).
/// </para>
/// <para>
/// The badges and actions fold behind a "Show more" toggle, whose <c>aria-expanded</c> says whether
/// they are shown: on a phone, and at any width as soon as line 1 has no room for them beside the
/// title. The same module measures line 1 unfolded when the header is resized or its content changes,
/// and marks the block <c>data-compact</c> when it overflows; the stylesheet then applies the phone
/// fold. Before the script runs (prerendering), only the phone fold applies. The "⋮" menu stays in
/// sight either way.
/// </para>
/// </remarks>
public partial class OmniPageHeader
{
    private readonly string _generatedId = $"omni-page-header-{Guid.NewGuid():N}";
    private OmniBreadcrumbService? _breadcrumb;
    private ElementReference _frame;
    private IJSObjectReference? _module;
    private bool _expanded;
    private bool _disposed;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>
    /// The title. Left empty, the current crumb of the breadcrumb service is the title, and a crumb
    /// still loading shows a placeholder instead.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// A line of explanation. On line 2 when the page has no ancestor to show, under the block when
    /// line 2 carries the breadcrumb.
    /// </summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Whether the block is drawn as a bordered frame on the surface colour. False, the default, draws
    /// the two lines straight on the page, without border, background or inner padding, the title then
    /// starting on the same vertical as the content under it; the block keeps its fixed height either way.
    /// </summary>
    [Parameter]
    public bool Framed { get; set; }

    /// <summary>Heading level of the title; the header of a page is its first heading. Its size stays the same whatever the level.</summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H1;

    /// <summary>The size class of the heading level, which the icon box takes to share the font of the title.</summary>
    private string IconLevelClass => $"omni-heading--{Level.ToString().ToLowerInvariant()}";

    /// <summary>
    /// Whether line 2 shows the breadcrumb when the page has ancestors; true by default. False, line 2
    /// shows the subtitle instead. The line keeps its height either way.
    /// </summary>
    [Parameter]
    public bool ShowTrail { get; set; } = true;

    /// <summary>Accessible name of the breadcrumb; the localized "Breadcrumb" when empty.</summary>
    [Parameter]
    public string? TrailLabel { get; set; }

    /// <summary>
    /// Shows a back button before the title. It leads to <see cref="BackHref"/>, else to the nearest
    /// ancestor of the trail that is a link, else one step back in the browser history.
    /// </summary>
    [Parameter]
    public bool ShowBack { get; set; }

    /// <summary>Where the back button leads, when the trail is not the right answer.</summary>
    [Parameter]
    public string? BackHref { get; set; }

    /// <summary>Accessible name and tooltip of the back button; the localized "Back" when empty.</summary>
    [Parameter]
    public string? BackLabel { get; set; }

    /// <summary>
    /// Look of the back button: <see cref="OmniButtonVariant.Primary"/> by default, the same accent
    /// arrow on every page; a site that wants it discreet passes <see cref="OmniButtonVariant.Ghost"/>.
    /// </summary>
    [Parameter]
    public OmniButtonVariant BackVariant { get; set; } = OmniButtonVariant.Primary;

    /// <summary>
    /// Icon drawn before the title in the accent colour, centred on the title line. Null, the default,
    /// renders the title alone.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// Status badges right after the title. Folded with the actions on a phone, and at any width when
    /// line 1 has no room for them.
    /// </summary>
    [Parameter]
    public RenderFragment? Badges { get; set; }

    /// <summary>
    /// Actions at the end of line 1, before the "⋮" menu. Folded with the badges on a phone, and at any
    /// width when line 1 has no room for them.
    /// </summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>
    /// The items of the "⋮" menu at the end of line 1: the menu items an <see cref="OmniOverflowMenu"/>
    /// takes. No menu when null (the default). It stays in sight on a phone.
    /// </summary>
    [Parameter]
    public RenderFragment? MenuContent { get; set; }

    /// <summary>Search and filter controls, on a row of their own under the block.</summary>
    [Parameter]
    public RenderFragment? Filters { get; set; }

    private IReadOnlyList<OmniBreadcrumbEntry> TrailAncestors => _breadcrumb?.Ancestors ?? [];

    private OmniBreadcrumbEntry? CurrentCrumb => _breadcrumb?.Current;

    /// <summary>Line 2 carries the breadcrumb: the page has ancestors and the trail is not switched off.</summary>
    private bool TrailShown => ShowTrail && TrailAncestors.Count > 0;

    private bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool TitleLoading => string.IsNullOrWhiteSpace(Title) && CurrentCrumb is { Loading: true };

    private string EffectiveTitle => !string.IsNullOrWhiteSpace(Title) ? Title : CurrentCrumb?.Text ?? string.Empty;

    private string EffectiveBackLabel => string.IsNullOrWhiteSpace(BackLabel) ? Localize("GoBack") : BackLabel;

    private bool HasDetails => Badges is not null || Actions is not null;

    private string DetailsId => $"{Id ?? _generatedId}-details";

    private string FrameClass => Framed ? "omni-page-header__frame" : "omni-page-header__frame omni-page-header__frame--plain";

    private string DetailsClass => _expanded
        ? "omni-page-header__details omni-page-header__details--open"
        : "omni-page-header__details";

    /// <summary>
    /// Starts listening to the breadcrumb service when one is registered; without it the header is
    /// titled by <see cref="Title"/> alone.
    /// </summary>
    protected override void OnInitialized()
    {
        // Resolved through the provider: a host that never called AddOmniEuropeBlazor still gets a
        // header, titled by Title alone, instead of a component that cannot be built.
        _breadcrumb = Services.GetService(typeof(OmniBreadcrumbService)) as OmniBreadcrumbService;
        if (_breadcrumb is not null)
        {
            _breadcrumb.Changed += OnBreadcrumbChanged;
        }
    }

    /// <summary>
    /// On the first render, attaches the script that folds line 1 when it overflows and scrolls the
    /// title; a lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script is attached.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", Internal.OmniModules.PageHeader);
            if (_disposed)
            {
                await module.DisposeAsync();
                return;
            }

            _module = module;
            await _module.InvokeVoidAsync("attach", _frame);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the header with it.
        }
    }

    private void OnBreadcrumbChanged() => _ = InvokeAsync(StateHasChanged);

    private void ToggleDetails() => _expanded = !_expanded;

    private Task GoBackAsync() =>
        Internal.OmniBackNavigation.GoBackAsync(Navigation, JavaScript, !string.IsNullOrWhiteSpace(BackHref) ? BackHref : _breadcrumb?.ParentHref);

    /// <summary>
    /// Stops listening to the breadcrumb service and detaches the script: its observers and listeners
    /// are released and the frame loses its <c>data-compact</c> mark.
    /// </summary>
    /// <returns>A task that completes once the script is detached; a lost circuit is ignored.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_breadcrumb is not null)
        {
            _breadcrumb.Changed -= OnBreadcrumbChanged;
        }

        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach", _frame);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
