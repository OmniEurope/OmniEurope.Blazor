using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Window controls page: the caption buttons of a borderless window, each switched on or off.</summary>
public partial class WindowControlsPage
{
    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;

    private bool ShowTray { get; set; }

    private bool ShowMinimize { get; set; } = true;

    private bool ShowMaximize { get; set; } = true;

    private bool ShowClose { get; set; } = true;

    private bool ShowActions { get; set; }
}
