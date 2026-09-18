namespace OmniEurope.Blazor.Components;

/// <summary>
/// The frame of a detail page. The header is painted before the entity arrives, so nothing below it
/// moves when it does; the content stays mounted but hidden until the entity is there; an entity that
/// is not found gives way to a not-found state with a back action. The shell holds no data: the page
/// tells it where the load stands through <see cref="State"/>.
/// </summary>
public partial class OmniDetailShell
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>Where the entity stands; Loading until the page says otherwise.</summary>
    [Parameter]
    public OmniDetailState State { get; set; }

    /// <summary>The page header, painted while loading and once found; the not-found state replaces it.</summary>
    [Parameter]
    public RenderFragment? Header { get; set; }

    /// <summary>
    /// The content of the page. Always rendered, and hidden until <see cref="State"/> is Found, so a
    /// child that starts the load from its own lifecycle does start it.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>What shows under the header while loading; a text skeleton when not given.</summary>
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>Replaces the whole default not-found state.</summary>
    [Parameter]
    public RenderFragment? NotFoundContent { get; set; }

    /// <summary>Title of the default not-found state; the localized "Item not found" when empty.</summary>
    [Parameter]
    public string? NotFoundTitle { get; set; }

    /// <summary>Explanation under the not-found title.</summary>
    [Parameter]
    public string? NotFoundDescription { get; set; }

    /// <summary>
    /// Where the back action of the not-found state leads; one step back in the browser history when
    /// empty.
    /// </summary>
    [Parameter]
    public string? BackHref { get; set; }

    /// <summary>Text of the back action; the localized "Back" when empty.</summary>
    [Parameter]
    public string? BackText { get; set; }

    private string EffectiveNotFoundTitle => string.IsNullOrWhiteSpace(NotFoundTitle) ? Localize("DetailNotFound") : NotFoundTitle;

    private string EffectiveBackText => string.IsNullOrWhiteSpace(BackText) ? Localize("GoBack") : BackText;

    private Task GoBackAsync() => OmniBackNavigation.GoBackAsync(Navigation, JavaScript, BackHref);
}
