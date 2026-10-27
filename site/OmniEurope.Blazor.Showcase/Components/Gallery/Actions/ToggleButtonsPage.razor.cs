using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Actions;

/// <summary>The Toggle buttons page: the settings of one toggle button, then a toolbar, its states and its sizes.</summary>
public partial class ToggleButtonsPage
{
    [Inject]
    private IStringLocalizer<ActionsStrings> Text { get; set; } = default!;

    /// <summary>The switch of the settings and the button of the preview share this one state.</summary>
    private bool Pinned { get; set; }

    private OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    private bool IconOnly { get; set; }

    private bool Disabled { get; set; }

    private bool Busy { get; set; }

    private IReadOnlyList<OmniOption<OmniControlSize>> SizeOptions =>
    [
        new(OmniControlSize.Small, Text["ToggleButtonsSizeSmall"]),
        new(OmniControlSize.Medium, Text["ToggleButtonsSizeMedium"]),
        new(OmniControlSize.Large, Text["ToggleButtonsSizeLarge"])
    ];

    private IReadOnlyList<OmniOption<bool>> ContentOptions =>
    [
        new(false, Text["ToggleButtonsContentIconAndLabel"]),
        new(true, Text["ToggleButtonsContentIconOnly"])
    ];
}
