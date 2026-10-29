using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Navigation;

/// <summary>The App menu page: the rows of the standard application menu, each switched on or off.</summary>
public partial class AppMenuPage
{
    [Inject]
    private IStringLocalizer<NavigationStrings> Text { get; set; } = default!;

    private bool WithAccount { get; set; } = true;

    private OmniAppMenuUserDisplay UserDisplay { get; set; } = OmniAppMenuUserDisplay.Name;

    private IReadOnlyList<OmniOption<OmniAppMenuUserDisplay>> UserDisplayOptions =>
    [
        new(OmniAppMenuUserDisplay.Name, Text["AppMenuUserDisplayName"]),
        new(OmniAppMenuUserDisplay.Initials, Text["AppMenuUserDisplayInitials"]),
        new(OmniAppMenuUserDisplay.Icon, Text["AppMenuUserDisplayIcon"])
    ];

    private bool WithLanguages { get; set; } = true;

    private bool ShowFlags { get; set; }

    private bool WithSettings { get; set; } = true;

    private bool WithSignOut { get; set; } = true;
}
