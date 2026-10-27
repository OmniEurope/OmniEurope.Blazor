using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The Breadcrumbs and links page: the settings of one trail and of a list of links, then three more shapes.</summary>
public partial class BreadcrumbsPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private double DepthValue { get; set; } = 4;

    private int Depth => (int)DepthValue;

    private bool MarkCurrent { get; set; } = true;

    private bool UnlinkedStep { get; set; }

    private bool NewTab { get; set; }
}
