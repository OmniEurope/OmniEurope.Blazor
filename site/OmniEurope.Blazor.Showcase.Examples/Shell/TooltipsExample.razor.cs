using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>Three tooltips of increasing length whose placement, maximum width and delay are parameters.</summary>
public partial class TooltipsExample
{
    private const string VeryLongText =
        "A merged record comes from two source records, once both have reached their final status. The merge then spreads to every linked entry around it: their counts are combined, the number of references grows by a quarter and the review delay lengthens as much. Beyond 240 characters the tooltip opens on a preview and \"Show more\" unfolds the full text.";

    /// <summary>Whether the tooltip follows the pointer or stays where it appeared.</summary>
    [Parameter]
    public OmniTooltipTracking Tracking { get; set; } = OmniTooltipTracking.Pinned;

    /// <summary>The width beyond which the text wraps.</summary>
    [Parameter]
    public OmniTooltipWidth MaxWidth { get; set; } = OmniTooltipWidth.Standard;

    /// <summary>How long the pointer rests before the tooltip shows; null is the delay of the package.</summary>
    [Parameter]
    public TimeSpan? Delay { get; set; }
}
