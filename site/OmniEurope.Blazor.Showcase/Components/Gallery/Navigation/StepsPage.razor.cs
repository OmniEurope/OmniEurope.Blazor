using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The Steps page: the settings of one strip of steps, then the same steps in four positions.</summary>
public partial class StepsPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private bool LockAhead { get; set; }

    private bool DisableReview { get; set; }
}
