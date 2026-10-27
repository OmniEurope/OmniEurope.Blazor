using System.Globalization;
using OmniEurope.Blazor.Showcase.Examples.Layout;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Page shell page: a miniature layout shaped by its own settings, the three content widths, then a whole application shell.</summary>
public partial class PageShellPage
{
    private const string MainId = "page-shell-main";

    private const string TitleId = "page-shell-title";

    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    // The setting of an application that centres its content: a full-width shell, a centred column, the scrollbar at the right edge.
    private OmniLayoutWidth ContentWidth { get; set; } = OmniLayoutWidth.Content;

    private bool Scrollable { get; set; } = true;

    private bool AutoHideScrollbar { get; set; } = true;

    private bool Sticky { get; set; } = true;

    private OmniHeaderSurface Surface { get; set; } = OmniHeaderSurface.Surface;

    private bool FocusTarget { get; set; } = true;

    private bool LabelledBy { get; set; } = true;

    private bool WideCanvas { get; set; } = true;

    /// <summary>
    /// The Wide and Content widths cap at 90rem and 72rem: in a card narrower than that, the three look
    /// the same. The wide canvas, which scrolls sideways, tells them apart. With its own scrolling, the
    /// shell takes the height of the frame so that the main area is what scrolls.
    /// </summary>
    private string CanvasClass => string.Join(' ', new[]
    {
        "gallery-shell-canvas",
        WideCanvas ? "gallery-shell-canvas--wide" : null,
        Scrollable ? "gallery-shell-canvas--fill" : null
    }.Where(value => value is not null));

    /// <summary>What the main element renders, so tabindex and aria-labelledby can be read without the browser tools.</summary>
    private string MainMarkup
    {
        get
        {
            var tabIndex = FocusTarget ? " tabindex=\"-1\"" : string.Empty;
            var labelledBy = LabelledBy ? $" aria-labelledby=\"{TitleId}\"" : string.Empty;
            var classes = string.Join(' ', new[]
            {
                "omni-main",
                Scrollable ? "omni-main--scrollable" : null,
                ContentWidth == OmniLayoutWidth.FullWidth ? null : $"omni-main--{ContentWidth.ToString().ToLowerInvariant()}"
            }.Where(value => value is not null));
            var main = $"<main id=\"{MainId}\" class=\"{classes}\"{tabIndex}{labelledBy}>";
            return Scrollable ? $"<div class=\"omni-main-scroll\">{main}" : main;
        }
    }

    private IReadOnlyList<OmniOption<OmniLayoutWidth>> WidthOptions =>
        [.. PageShellWidths.All.Select(width => new OmniOption<OmniLayoutWidth>(width, WidthLabel(width)))];

    private IReadOnlyList<OmniOption<OmniHeaderSurface>> SurfaceOptions =>
    [
        new(OmniHeaderSurface.Surface, Text["PageShellSurfacePage"]),
        new(OmniHeaderSurface.Accent, Text["PageShellSurfaceAccent"])
    ];

    private string WidthLabel(OmniLayoutWidth width) => width switch
    {
        OmniLayoutWidth.Wide => Text["PageShellWidthWide"],
        OmniLayoutWidth.Content => Text["PageShellWidthContent"],
        _ => Text["PageShellWidthFull"]
    };

    private string WidthCaption(OmniLayoutWidth width) =>
        string.Format(CultureInfo.CurrentCulture, Text["PageShellWidthCaption"].Value, WidthLabel(width), PageShellWidths.Cap(width));
}
