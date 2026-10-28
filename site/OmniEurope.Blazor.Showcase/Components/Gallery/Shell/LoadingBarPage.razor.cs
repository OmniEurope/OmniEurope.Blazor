using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Loading bar page: the mode and the colour of a bar that follows the loading state.</summary>
public partial class LoadingBarPage
{
    /// <summary>The colours of the bar, each a class of the showcase stylesheet that redefines the bar's variable.</summary>
    private static readonly string[] Colors = ["accent", "success", "warning", "danger"];

    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;

    private OmniLoadingBarMode Mode { get; set; } = OmniLoadingBarMode.Sweep;

    private string Color { get; set; } = Colors[0];

    /// <summary>The class set on the bar itself: the variable of the colour is declared there, not on an ancestor.</summary>
    private string BarClass => $"gallery-loading-bar-{Color}";

    private IReadOnlyList<OmniOption<OmniLoadingBarMode>> ModeOptions =>
    [
        new(OmniLoadingBarMode.Sweep, Text["LoadingBarModeSweep"]),
        new(OmniLoadingBarMode.Continuous, Text["LoadingBarModeContinuous"])
    ];

    private IReadOnlyList<OmniOption<string>> ColorOptions =>
    [
        new(Colors[0], Text["LoadingBarColorAccent"]),
        new(Colors[1], Text["LoadingBarColorSuccess"]),
        new(Colors[2], Text["LoadingBarColorWarning"]),
        new(Colors[3], Text["LoadingBarColorDanger"])
    ];
}
