using OmniEurope.Blazor.Showcase.Examples.Layout;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Settings sections page: a section of tiles shaped by its own settings, then a privacy section as an application has one.</summary>
public partial class SettingsSectionsPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private bool ShowSectionDescription { get; set; } = true;

    private bool ShowIcons { get; set; } = true;

    private bool ShowDescriptions { get; set; } = true;

    private bool Flat { get; set; }

    private SettingsTileContent Content { get; set; } = SettingsTileContent.Both;

    private IReadOnlyList<OmniOption<SettingsTileContent>> ContentOptions =>
    [
        new(SettingsTileContent.Control, Text["SettingsSectionsContentControl"]),
        new(SettingsTileContent.Details, Text["SettingsSectionsContentDetails"]),
        new(SettingsTileContent.Both, Text["SettingsSectionsContentBoth"]),
        new(SettingsTileContent.None, Text["SettingsSectionsContentNone"])
    ];
}
