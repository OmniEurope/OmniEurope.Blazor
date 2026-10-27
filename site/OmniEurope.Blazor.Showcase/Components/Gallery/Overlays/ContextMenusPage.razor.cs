using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Overlays;

/// <summary>The Context menus page: the settings of one menu, then a menu per row of a list.</summary>
public partial class ContextMenusPage
{
    [Inject]
    private IStringLocalizer<OverlaysStrings> Text { get; set; } = default!;

    private bool ShowDelete { get; set; } = true;

    private bool DisableRename { get; set; }

    private bool KeepOpen { get; set; }

    private bool Disabled { get; set; }
}
