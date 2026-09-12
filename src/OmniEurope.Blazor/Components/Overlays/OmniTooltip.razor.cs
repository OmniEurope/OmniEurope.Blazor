using System.Globalization;

namespace OmniEurope.Blazor.Components;

public partial class OmniTooltip
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public int? TabIndex { get; set; }

    /// <summary>
    /// Whether the open tooltip follows the pointer or stays where it first appeared.
    /// </summary>
    [Parameter]
    public OmniTooltipTracking Tracking { get; set; } = OmniTooltipTracking.Pointer;

    /// <summary>
    /// How long the pointer rests on the trigger before the tooltip shows. Null keeps the stylesheet's
    /// <c>--omni-tooltip-delay</c>, 700 ms unless a host redefines it on <c>.omni-tooltip</c> itself
    /// (the variable is declared there, so a value set on an ancestor never reaches it). Drawn at a 100 ms step between
    /// 0 and 2 s: the delay lives in the stylesheet, not in an inline style the content policy would
    /// reject.
    /// </summary>
    [Parameter]
    public TimeSpan? Delay { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    internal const int MaxDelayMilliseconds = 2000;

    private string? DelayClass
    {
        get
        {
            if (Delay is not { } delay)
            {
                return null;
            }

            var steps = Math.Clamp((int)Math.Round(delay.TotalMilliseconds / 100, MidpointRounding.AwayFromZero), 0, MaxDelayMilliseconds / 100);
            return $"omni-tooltip--delay-{(steps * 100).ToString(CultureInfo.InvariantCulture)}";
        }
    }

    /// <summary>
    /// Resolved through the provider rather than injected directly: a host that never called
    /// <c>AddOmniEuropeBlazor</c> would fail to render the component at all under <c>[Inject]</c>,
    /// where here it simply keeps the stylesheet's anchored tooltip instead of the pointer-tracking
    /// one.
    /// </summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private string TooltipId => $"{Id ?? "omni-tooltip"}-content";

    /// <summary>
    /// Read by omni-tooltip.js, which places the box: the choice has to reach the one script that
    /// owns every tooltip on the page, and an attribute is the only channel it reads.
    /// </summary>
    private string TrackingAttribute => Tracking.ToString().ToLowerInvariant();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // Scoped, so the tooltips of one page share a single import and a single set of listeners
        // however many of them a grid renders.
        if (Services.GetService(typeof(OmniTooltipInterop)) is OmniTooltipInterop interop)
        {
            await interop.EnsureInstalledAsync();
        }
    }
}
