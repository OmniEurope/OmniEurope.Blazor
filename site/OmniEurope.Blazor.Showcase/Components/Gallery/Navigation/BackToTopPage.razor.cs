using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The Back to top page: a long article and the button that brings the page back to its top.</summary>
public partial class BackToTopPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private bool LowThreshold { get; set; }

    private bool CustomLabel { get; set; }
}
