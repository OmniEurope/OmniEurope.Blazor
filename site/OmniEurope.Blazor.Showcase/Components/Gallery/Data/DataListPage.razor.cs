using System.Globalization;
using OmniEurope.Blazor.Showcase.Examples.Data;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The Data list page: the settings of a catalogue list and its pager, then the list in its other states and forms.</summary>
public partial class DataListPage
{
    private static readonly int[] PageSizes = [3, 6, 12, 24];
    private static readonly int[] NumericPageCounts = [0, 3, 5];

    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;

    private DataListSource Source { get; set; } = DataListSource.Items;

    private double LatencyValue { get; set; } = 750;

    private bool FailLoad { get; set; }

    private bool EmptyData { get; set; }

    private bool Virtualize { get; set; }

    private int PageSize { get; set; } = 6;

    private int NumericPageCount { get; set; } = 5;

    private OmniJustification PagerAlign { get; set; } = OmniJustification.Center;

    private bool ShowFirstLast { get; set; } = true;

    private bool ShowPageSizeOptions { get; set; } = true;

    private bool PagerDisabled { get; set; }

    private IReadOnlyList<OmniOption<DataListSource>> SourceOptions =>
    [
        new(DataListSource.Items, Text["DataListSourceItems"]),
        new(DataListSource.Load, Text["DataListSourceLoad"])
    ];

    private IReadOnlyList<OmniOption<int>> PageSizeChoices =>
        [.. PageSizes.Select(size => new OmniOption<int>(size, size.ToString(CultureInfo.CurrentCulture)))];

    private IReadOnlyList<OmniOption<int>> NumericPageChoices =>
    [
        .. NumericPageCounts.Select(count => new OmniOption<int>(
            count,
            count == 0 ? Text["DataListNumericPagesNone"] : count.ToString(CultureInfo.CurrentCulture)))
    ];

    private IReadOnlyList<OmniOption<OmniJustification>> AlignOptions =>
    [
        new(OmniJustification.Start, Text["DataListAlignStart"]),
        new(OmniJustification.Center, Text["DataListAlignCenter"]),
        new(OmniJustification.End, Text["DataListAlignEnd"]),
        new(OmniJustification.Between, Text["DataListAlignBetween"])
    ];

    private string FormatMilliseconds(double milliseconds) =>
        string.Format(CultureInfo.CurrentCulture, Text["DataListMilliseconds"], milliseconds.ToString("0", CultureInfo.CurrentCulture));
}
