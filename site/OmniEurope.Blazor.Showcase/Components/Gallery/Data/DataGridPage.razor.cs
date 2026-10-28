using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The Data grid page: the settings of a flights grid, which drive its example, then a date-range filter on its own.</summary>
public partial class DataGridPage
{
    private static readonly int[] PageSizes = [5, 10, 20, 50];
    private static readonly int[] NumericPageCounts = [0, 3, 5, 7];

    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;

    // ---- data ---------------------------------------------------------------------------------

    private bool Virtualize { get; set; } = true;

    private bool EmptyData { get; set; }

    private bool IsLoading { get; set; }

    private bool ShowLoadingBar { get; set; } = true;

    private OmniLoadingBarMode LoadingBarMode { get; set; } = OmniLoadingBarMode.Sweep;

    // ---- paging -------------------------------------------------------------------------------

    private bool AllowPaging { get; set; } = true;

    private int PageSize { get; set; } = 10;

    private OmniDataGridPlacement PagerPlacement { get; set; } = OmniDataGridPlacement.Bottom;

    private OmniJustification PagerJustify { get; set; } = OmniJustification.Between;

    private int NumericPageCount { get; set; } = 5;

    private bool ShowPageSizeOptions { get; set; } = true;

    private bool ShowPagingSummary { get; set; } = true;

    private bool AlwaysShowPager { get; set; } = true;

    // ---- sorting and filters ------------------------------------------------------------------

    private bool AllowSorting { get; set; } = true;

    private bool AllowFiltering { get; set; } = true;

    private OmniDataGridFilterMode FilterMode { get; set; } = OmniDataGridFilterMode.Simple;

    private bool ShowHeaderFilterMenu { get; set; }

    private bool HideFilterMenuOnSelect { get; set; } = true;

    private bool CaseSensitiveFilters { get; set; }

    private bool IgnoreDiacritics { get; set; } = true;

    // ---- rows ---------------------------------------------------------------------------------

    private OmniDataGridSelectionMode SelectionMode { get; set; } = OmniDataGridSelectionMode.Multiple;

    private bool SelectOnRowClick { get; set; }

    private bool LockCancelled { get; set; } = true;

    private bool ShowEditColumn { get; set; } = true;

    private OmniDataGridRowMode EditMode { get; set; } = OmniDataGridRowMode.Single;

    private bool ShowDetails { get; set; } = true;

    private OmniDataGridRowMode ExpandMode { get; set; } = OmniDataGridRowMode.Multiple;

    private bool ShowExpandAll { get; set; } = true;

    private bool AllowGrouping { get; set; } = true;

    private bool ShowGroupPanel { get; set; } = true;

    private bool AllGroupsExpanded { get; set; } = true;

    // ---- columns and look ---------------------------------------------------------------------

    private bool AllowColumnResize { get; set; } = true;

    private bool AllowColumnReorder { get; set; } = true;

    private bool AllowColumnHide { get; set; } = true;

    private bool ShowResetLayout { get; set; }

    private bool ShowExportButtonText { get; set; } = true;

    private bool FreezeNumber { get; set; } = true;

    private OmniDataGridHeaderWrap HeaderWrap { get; set; } = OmniDataGridHeaderWrap.Wrap;

    private bool HighlightActiveColumn { get; set; } = true;

    private OmniDensity Density { get; set; } = OmniDensity.Compact;

    private OmniDataGridLines GridLines { get; set; } = OmniDataGridLines.Horizontal;

    private bool AlternatingRows { get; set; } = true;

    private bool HighlightRowOnHover { get; set; } = true;

    private bool FixedRowHeight { get; set; }

    private OmniDataGridPlacement FooterPlacement { get; set; } = OmniDataGridPlacement.Bottom;

    private bool Responsive { get; set; }

    // ---- derived state ------------------------------------------------------------------------

    private bool PagingDisabled => Virtualize || !AllowPaging;

    private bool DetailsDisabled => Virtualize || !ShowDetails;

    private bool GroupingDisabled => Virtualize || !AllowGrouping;

    // ---- options of the settings --------------------------------------------------------------

    private IReadOnlyList<OmniOption<OmniLoadingBarMode>> LoadingBarModeOptions =>
    [
        new(OmniLoadingBarMode.Sweep, Text["DataGridLoadingBarSweep"]),
        new(OmniLoadingBarMode.Continuous, Text["DataGridLoadingBarContinuous"])
    ];

    private IReadOnlyList<OmniOption<int>> PageSizeChoices =>
        [.. PageSizes.Select(size => new OmniOption<int>(size, size.ToString(CultureInfo.CurrentCulture)))];

    private IReadOnlyList<OmniOption<int>> NumericPageChoices =>
    [
        .. NumericPageCounts.Select(count => new OmniOption<int>(
            count,
            count == 0 ? Text["DataGridNumericPagesNone"] : count.ToString(CultureInfo.CurrentCulture)))
    ];

    private IReadOnlyList<OmniOption<OmniDataGridPlacement>> PagerPositionOptions =>
    [
        new(OmniDataGridPlacement.Bottom, Text["DataGridPagerBottom"]),
        new(OmniDataGridPlacement.Top, Text["DataGridPagerTop"]),
        new(OmniDataGridPlacement.TopAndBottom, Text["DataGridPagerBoth"])
    ];

    private IReadOnlyList<OmniOption<OmniJustification>> PagerAlignOptions =>
    [
        new(OmniJustification.Start, Text["DataGridAlignStart"]),
        new(OmniJustification.Center, Text["DataGridAlignCenter"]),
        new(OmniJustification.End, Text["DataGridAlignEnd"]),
        new(OmniJustification.Between, Text["DataGridAlignBetween"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridFilterMode>> FilterModeOptions =>
    [
        new(OmniDataGridFilterMode.Simple, Text["DataGridFilterSimple"]),
        new(OmniDataGridFilterMode.SimpleWithMenu, Text["DataGridFilterSimpleWithMenu"]),
        new(OmniDataGridFilterMode.Advanced, Text["DataGridFilterAdvanced"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridSelectionMode>> SelectionModeOptions =>
    [
        new(OmniDataGridSelectionMode.None, Text["DataGridSelectionNone"]),
        new(OmniDataGridSelectionMode.Single, Text["DataGridSelectionSingle"]),
        new(OmniDataGridSelectionMode.Multiple, Text["DataGridSelectionMultiple"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridRowMode>> EditModeOptions =>
    [
        new(OmniDataGridRowMode.Single, Text["DataGridEditSingle"]),
        new(OmniDataGridRowMode.Multiple, Text["DataGridEditMultiple"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridRowMode>> ExpandModeOptions =>
    [
        new(OmniDataGridRowMode.Single, Text["DataGridExpandSingle"]),
        new(OmniDataGridRowMode.Multiple, Text["DataGridExpandMultiple"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridHeaderWrap>> HeaderWrapOptions =>
    [
        new(OmniDataGridHeaderWrap.Wrap, Text["DataGridHeaderWrapWrap"]),
        new(OmniDataGridHeaderWrap.Truncate, Text["DataGridHeaderWrapTruncate"])
    ];

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =>
    [
        new(OmniDensity.Compact, Text["DataGridDensityCompact"]),
        new(OmniDensity.Comfortable, Text["DataGridDensityComfortable"]),
        new(OmniDensity.Spacious, Text["DataGridDensitySpacious"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridLines>> GridLinesOptions =>
    [
        new(OmniDataGridLines.None, Text["DataGridLinesNone"]),
        new(OmniDataGridLines.Horizontal, Text["DataGridLinesHorizontal"]),
        new(OmniDataGridLines.Vertical, Text["DataGridLinesVertical"]),
        new(OmniDataGridLines.Both, Text["DataGridLinesBoth"])
    ];

    private IReadOnlyList<OmniOption<OmniDataGridPlacement>> FooterPositionOptions =>
    [
        new(OmniDataGridPlacement.Bottom, Text["DataGridFooterBottom"]),
        new(OmniDataGridPlacement.Top, Text["DataGridFooterTop"])
    ];
}
