using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A short text shown over its trigger (<see cref="ChildContent"/>) on hover and focus, and read as the
/// trigger's accessible description. A long text opens on a preview with a "Show more" action.
/// </summary>
public partial class OmniTooltip
{
    private readonly string _generatedId = $"omni-tooltip-{Guid.NewGuid():N}";

    /// <summary>The text of the tooltip, also the accessible description of the trigger.</summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Puts the trigger in the Tab order (<c>tabindex="0"</c>), so a keyboard reader reaches the tooltip
    /// of content that takes no focus of its own (a text, an icon). False, the default, leaves the
    /// trigger out of it: a focusable control inside it shows the tooltip on its own focus.
    /// </summary>
    [Parameter]
    public bool Focusable { get; set; }

    /// <summary>
    /// Whether the open tooltip follows the pointer or stays where it first appeared.
    /// </summary>
    [Parameter]
    public OmniTooltipTracking Tracking { get; set; } = OmniTooltipTracking.Pointer;

    /// <summary>
    /// How long the pointer rests on the trigger before the tooltip shows. Null keeps the stylesheet's
    /// <c>--omni-tooltip-delay</c>, 300 ms unless a host redefines it on <c>.omni-tooltip</c> itself
    /// (the variable is declared there, so a value set on an ancestor never reaches it). Drawn at a 100 ms step between
    /// 0 and 2 s: the delay lives in the stylesheet, not in an inline style the content policy would
    /// reject.
    /// </summary>
    [Parameter]
    public TimeSpan? Delay { get; set; }

    /// <summary>The trigger the tooltip describes.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The widest the open tooltip grows before its text wraps: 18rem by default, 12rem narrow, 28rem
    /// wide. The text is never cut by the width, it wraps.
    /// </summary>
    [Parameter]
    public OmniTooltipWidth MaxWidth { get; set; } = OmniTooltipWidth.Standard;

    /// <summary>
    /// Beyond this many characters the tooltip opens on a preview ending with an ellipsis and a
    /// "Show more" action that unfolds the full text; the pointer can then move into the tooltip to
    /// reach it. Null or zero never shortens the text. The full text remains the accessible
    /// description either way.
    /// </summary>
    [Parameter]
    public int? CompactLength { get; set; } = DefaultCompactLength;

    internal const int DefaultCompactLength = 240;

    internal const int MaxDelayMilliseconds = 2000;

    private bool _expanded;

    private bool IsExpandable => CompactLength is > 0 and var limit && Text.Length > limit;

    /// <summary>
    /// The preview cut at the last word boundary before the limit, so a word is never split in two.
    /// </summary>
    private string CompactText
    {
        get
        {
            // Read only when IsExpandable, which needs a positive CompactLength.
            var limit = CompactLength!.Value;
            var cut = Text.LastIndexOf(' ', Math.Min(limit, Text.Length - 1));
            var end = cut > limit / 2 ? cut : limit;
            return string.Concat(Text.AsSpan(0, end).TrimEnd(), "…");
        }
    }

    private string? WidthClass => MaxWidth switch
    {
        OmniTooltipWidth.Narrow => "omni-tooltip--narrow",
        OmniTooltipWidth.Wide => "omni-tooltip--wide",
        _ => null
    };

    private string FullTextId => $"{TooltipId}-full";

    private void ToggleExpanded() => _expanded = !_expanded;

    // Leaving the tooltip folds it back, so the next visit opens on the short preview again.
    private void Collapse() => _expanded = false;

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

    private string TooltipId => $"{Id ?? _generatedId}-content";

    /// <summary>
    /// Read by omni-tooltip.js, which places the box: the choice has to reach the one script that
    /// owns every tooltip on the page, and an attribute is the only channel it reads.
    /// </summary>
    private string TrackingAttribute => Tracking.ToString().ToLowerInvariant();

    /// <summary>
    /// On the first render, installs the script that places the tooltips, once for the whole page. Without
    /// the package's services registered, it installs nothing and the stylesheet's anchored tooltip remains.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script is installed.</returns>
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
