namespace OmniEurope.Blazor.Components;

/// <summary>
/// The header of a page: breadcrumb ancestors, back action, title, badges and actions in one framed
/// block, then an optional subtitle and a row of filters under it. The trail and, when no
/// <see cref="Title"/> is given, the title itself come from <see cref="OmniBreadcrumbService"/>, so a
/// page names itself through its route and renames itself by updating the service.
/// </summary>
public partial class OmniPageHeader : IDisposable
{
    private readonly string _generatedId = $"omni-page-header-{Guid.NewGuid():N}";
    private OmniBreadcrumbService? _breadcrumb;
    private bool _expanded;

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

    /// <summary>A line of explanation under the frame.</summary>
    [Parameter]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Whether the back action, title, badges and actions sit in a bordered block on the surface
    /// colour. True, the default, keeps that frame; false draws them straight on the page, without
    /// border, background or inner padding.
    /// </summary>
    [Parameter]
    public bool Framed { get; set; } = true;

    /// <summary>Heading level of the title; the header of a page is its first heading.</summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H1;

    /// <summary>
    /// Whether the first line shows the ancestors of the page. The line keeps its height when the
    /// page has none, so every page starts its content at the same height; false removes it.
    /// </summary>
    [Parameter]
    public bool ShowTrail { get; set; } = true;

    /// <summary>
    /// Accessible name of the trail; the localized "Breadcrumb" when empty.
    /// </summary>
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
    /// Icon drawn before the title, its centre on the centre of the title's capital letters. Null, the
    /// default, renders the title alone.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Status badges right after the title. Folded with the actions on a phone.</summary>
    [Parameter]
    public RenderFragment? Badges { get; set; }

    /// <summary>Actions at the end of the row. Folded with the badges on a phone.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>Search and filter controls, on a row of their own under the frame.</summary>
    [Parameter]
    public RenderFragment? Filters { get; set; }

    private IReadOnlyList<OmniBreadcrumbEntry> TrailAncestors => _breadcrumb?.Ancestors ?? [];

    private OmniBreadcrumbEntry? CurrentCrumb => _breadcrumb?.Current;

    private bool TitleLoading => string.IsNullOrWhiteSpace(Title) && CurrentCrumb is { Loading: true };

    private string EffectiveTitle => !string.IsNullOrWhiteSpace(Title) ? Title : CurrentCrumb?.Text ?? string.Empty;

    private string EffectiveBackLabel => string.IsNullOrWhiteSpace(BackLabel) ? Localize("GoBack") : BackLabel;

    private string ToggleLabel => Localize(_expanded ? "PageHeaderHideDetails" : "PageHeaderShowDetails");

    private bool HasDetails => Badges is not null || Actions is not null;

    private string DetailsId => $"{Id ?? _generatedId}-details";

    private string DetailsClass => _expanded
        ? "omni-page-header__details omni-page-header__details--open"
        : "omni-page-header__details";

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

    private void OnBreadcrumbChanged() => _ = InvokeAsync(StateHasChanged);

    private void ToggleDetails() => _expanded = !_expanded;

    private Task GoBackAsync() =>
        OmniBackNavigation.GoBackAsync(Navigation, JavaScript, !string.IsNullOrWhiteSpace(BackHref) ? BackHref : _breadcrumb?.ParentHref);

    /// <summary>Stops listening to the breadcrumb service.</summary>
    public void Dispose()
    {
        if (_breadcrumb is not null)
        {
            _breadcrumb.Changed -= OnBreadcrumbChanged;
        }

        GC.SuppressFinalize(this);
    }
}
