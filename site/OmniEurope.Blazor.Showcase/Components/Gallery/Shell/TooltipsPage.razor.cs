using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Tooltips page: placement, width and delay of a tooltip, then its variants and the title attribute.</summary>
public partial class TooltipsPage
{
    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;

    private OmniTooltipTracking Tracking { get; set; } = OmniTooltipTracking.Pointer;

    private OmniTooltipWidth Width { get; set; } = OmniTooltipWidth.Standard;

    /// <summary>The delay as the slider writes it; 300 ms is the delay of the package.</summary>
    private double DelayMilliseconds { get; set; } = 300;

    private TimeSpan Delay => TimeSpan.FromMilliseconds(DelayMilliseconds);

    private string FormatDelay(double milliseconds) =>
        string.Format(CultureInfo.CurrentCulture, Text["TooltipsDelayValue"], milliseconds.ToString("0", CultureInfo.CurrentCulture));

    private IReadOnlyList<OmniOption<OmniTooltipTracking>> TrackingOptions =>
    [
        new(OmniTooltipTracking.Pointer, Text["TooltipsTrackingPointer"]),
        new(OmniTooltipTracking.Pinned, Text["TooltipsTrackingPinned"])
    ];

    private IReadOnlyList<OmniOption<OmniTooltipWidth>> WidthOptions =>
    [
        new(OmniTooltipWidth.Standard, Text["TooltipsWidthStandard"]),
        new(OmniTooltipWidth.Narrow, Text["TooltipsWidthNarrow"]),
        new(OmniTooltipWidth.Wide, Text["TooltipsWidthWide"])
    ];
}
