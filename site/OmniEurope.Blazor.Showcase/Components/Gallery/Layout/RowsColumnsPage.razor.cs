using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Layout;

/// <summary>The Rows and columns page: a row of twelve units driven by its settings, then the common splits, the scales and the breakpoints.</summary>
public partial class RowsColumnsPage
{
    [Inject]
    private IStringLocalizer<LayoutStrings> Text { get; set; } = default!;

    private OmniSpacing Gap { get; set; } = OmniSpacing.Medium;

    private OmniAlignment Align { get; set; } = OmniAlignment.Stretch;

    private OmniJustification Justify { get; set; } = OmniJustification.Start;

    private bool Wrap { get; set; } = true;

    private double ColumnCount { get; set; } = 3;

    private double Span { get; set; } = 4;

    private bool Responsive { get; set; }

    private double SmallSpan { get; set; } = 6;

    private double MediumSpan { get; set; } = 4;

    private double LargeSpan { get; set; } = 3;

    private IReadOnlyList<OmniOption<OmniSpacing>> GapOptions =>
        [.. Enum.GetValues<OmniSpacing>().Select(value => new OmniOption<OmniSpacing>(value, Text[$"RowsColumnsGap{value}"]))];

    private IReadOnlyList<OmniOption<OmniAlignment>> AlignOptions =>
        [.. Enum.GetValues<OmniAlignment>().Select(value => new OmniOption<OmniAlignment>(value, Text[$"RowsColumnsAlign{value}"]))];

    private IReadOnlyList<OmniOption<OmniJustification>> JustifyOptions =>
        [.. Enum.GetValues<OmniJustification>().Select(value => new OmniOption<OmniJustification>(value, Text[$"RowsColumnsJustify{value}"]))];
}
