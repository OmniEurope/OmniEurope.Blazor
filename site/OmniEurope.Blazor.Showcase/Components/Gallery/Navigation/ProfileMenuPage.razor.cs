using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The Profile menu page: the settings of one account menu, then other triggers.</summary>
public partial class ProfileMenuPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private bool ShowIcons { get; set; } = true;

    private bool DisableAdministration { get; set; } = true;

    private bool ShowLinkItem { get; set; } = true;

    private bool Disabled { get; set; }

    /// <summary>
    /// The setting of the component, on by default. The frame marks the settings column data-omni-keep-open,
    /// so changing a setting does not close an open menu.
    /// </summary>
    private bool CloseOnOutsideClick { get; set; } = true;

    private bool Compact { get; set; }
}
