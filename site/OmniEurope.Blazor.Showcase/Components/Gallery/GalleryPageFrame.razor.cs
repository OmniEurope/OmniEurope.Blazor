using Microsoft.AspNetCore.Components.Routing;
using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>
/// The common frame of a gallery page: header, description, component badges, theme picker and the
/// Preview, Mobile and Code tabs.
/// </summary>
public partial class GalleryPageFrame : IDisposable
{
    /// <summary>The preview at its own width.</summary>
    private const string PreviewView = "preview";

    /// <summary>The preview in the screen of a phone.</summary>
    private const string MobileView = "mobile";

    /// <summary>The sources of the page.</summary>
    private const string CodeView = "code";

    /// <summary>The address parameter of the selected view, kept on reload and in a shared link.</summary>
    private const string ViewParameter = "view";

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private IStringLocalizerFactory Localizers { get; set; } = default!;

    /// <summary>The slug of the page in <see cref="GalleryCatalog"/>.</summary>
    [Parameter, EditorRequired]
    public string Slug { get; set; } = string.Empty;

    /// <summary>The settings, on the left on a wide screen.</summary>
    [Parameter]
    public RenderFragment? Settings { get; set; }

    /// <summary>What the settings change, on the right on a wide screen.</summary>
    [Parameter]
    public RenderFragment? Preview { get; set; }

    private GalleryPage Page { get; set; } = default!;

    private IStringLocalizer Strings { get; set; } = default!;

    private string Title => Strings[Page.TitleKey];

    /// <summary>
    /// The view shown, kept here rather than read back from the address at render: the render that
    /// follows a click on a tab runs before the navigation changes the address, so the view would lag
    /// one click behind. The address stays the source on load and on going back.
    /// </summary>
    private string View { get; set; } = PreviewView;

    protected override void OnInitialized()
    {
        View = ViewInAddress();
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override void OnParametersSet()
    {
        Page = GalleryCatalog.Find(Slug)
            ?? throw new InvalidOperationException($"No gallery page is catalogued under '{Slug}'.");
        Strings = Localizers.Create(GalleryCatalog.FamilyOf(Page).Strings);
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        GC.SuppressFinalize(this);
    }

    private static string Normalize(string? view) => view is MobileView or CodeView ? view : PreviewView;

    private void SetView(string? view)
    {
        View = Normalize(view);
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter(ViewParameter, View == PreviewView ? null : View), replace: true);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var view = ViewInAddress();
        if (view != View)
        {
            View = view;
            InvokeAsync(StateHasChanged);
        }
    }

    private string ViewInAddress() => Normalize(Query(ViewParameter));

    private string? Query(string name) =>
        Navigation.ToAbsoluteUri(Navigation.Uri).Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => pair[0] == name)
            .Select(pair => pair.Length > 1 ? Uri.UnescapeDataString(pair[1]) : string.Empty)
            .FirstOrDefault();
}
