using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The Site search page: the search of an app bar over a few pages, with or without groups and icons.</summary>
public partial class SiteSearchPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private bool Grouped { get; set; } = true;

    private bool ShowIcons { get; set; } = true;
}
