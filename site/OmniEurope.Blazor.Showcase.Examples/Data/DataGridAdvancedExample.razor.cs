using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>The grid with every feature on, its display settings driven by a row of selectors.</summary>
public partial class DataGridAdvancedExample
{
    // A text column can narrow its operator menu to the conditions that make sense for it.
    private static readonly IReadOnlyList<OmniDataGridFilterOperator> ReferenceOperators =
    [
        OmniDataGridFilterOperator.Contains, OmniDataGridFilterOperator.StartsWith,
        OmniDataGridFilterOperator.EndsWith, OmniDataGridFilterOperator.DoesNotContain
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridLines>> _lineOptions =
    [
        new(OmniDataGridLines.None, "None"),
        new(OmniDataGridLines.Horizontal, "Horizontal"),
        new(OmniDataGridLines.Vertical, "Vertical"),
        new(OmniDataGridLines.Both, "Both")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDensity>> _densityOptions =
    [
        new(OmniDensity.Compact, "Compact"),
        new(OmniDensity.Comfortable, "Comfortable"),
        new(OmniDensity.Spacious, "Spacious")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridPlacement>> _pagerOptions =
    [
        new(OmniDataGridPlacement.Bottom, "Bottom"),
        new(OmniDataGridPlacement.Top, "Top"),
        new(OmniDataGridPlacement.TopAndBottom, "Top and bottom")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridSelectionMode>> _selectionOptions =
    [
        new(OmniDataGridSelectionMode.None, "None"),
        new(OmniDataGridSelectionMode.Single, "Single"),
        new(OmniDataGridSelectionMode.Multiple, "Multiple")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridFilterMode>> _filterOptions =
    [
        new(OmniDataGridFilterMode.Simple, "Simple"),
        new(OmniDataGridFilterMode.SimpleWithMenu, "Simple with menu"),
        new(OmniDataGridFilterMode.Advanced, "Advanced")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridRowMode>> _editOptions =
    [
        new(OmniDataGridRowMode.Single, "One row at a time"),
        new(OmniDataGridRowMode.Multiple, "Several rows")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridRowMode>> _expandOptions =
    [
        new(OmniDataGridRowMode.Single, "One row at a time"),
        new(OmniDataGridRowMode.Multiple, "Several rows")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridPlacement>> _footerOptions =
    [
        new(OmniDataGridPlacement.Bottom, "Bottom"),
        new(OmniDataGridPlacement.Top, "Top"),
        new(OmniDataGridPlacement.TopAndBottom, "Top and bottom")
    ];

    private readonly IReadOnlyList<OmniOption<OmniDataGridAggregate>> _aggregateOptions =
    [
        new(OmniDataGridAggregate.None, "None"),
        new(OmniDataGridAggregate.Sum, "Sum"),
        new(OmniDataGridAggregate.Average, "Average"),
        new(OmniDataGridAggregate.Min, "Minimum"),
        new(OmniDataGridAggregate.Max, "Maximum"),
        new(OmniDataGridAggregate.Count, "Count")
    ];

    // Grouped by country from the start, so each group closes on its subtotal row.
    private IReadOnlyList<OmniDataGridGroup> _grouping = [new(nameof(CaseRow.Country))];

    private IReadOnlyList<CaseRow> _selection = [];

    private OmniDataGridAggregate _aggregate = OmniDataGridAggregate.Sum;

    private OmniDataGridLines _lines = OmniDataGridLines.Both;

    private OmniDensity _density = OmniDensity.Comfortable;

    private OmniDataGridPlacement _pager = OmniDataGridPlacement.Bottom;

    private OmniDataGridSelectionMode _mode = OmniDataGridSelectionMode.Multiple;

    private OmniDataGridFilterMode _filters = OmniDataGridFilterMode.Advanced;

    private OmniDataGridRowMode _edit = OmniDataGridRowMode.Single;

    private OmniDataGridRowMode _expand = OmniDataGridRowMode.Multiple;

    private OmniDataGridPlacement _footer = OmniDataGridPlacement.Bottom;

    private string? _cellRead;

    // A count is a number of rows, not an amount; a sum reads as the total in the grid footer.
    private string AggregateFormat => _aggregate switch
    {
        OmniDataGridAggregate.Sum => "Total {0:C0}",
        OmniDataGridAggregate.Count => "{0}",
        _ => "{0:C0}"
    };

    private string GroupAggregateFormat => _aggregate == OmniDataGridAggregate.Count ? "{0}" : "{0:C0}";

    // The cell double-clicked, read back as the host of a drill-down would: its column, its row, its value.
    private void ReadCell(OmniDataGridCellMouseEventArgs<CaseRow> cell) =>
        _cellRead = $"Cell {cell.ColumnTitle} of {cell.Item.Reference}: {cell.Value}";
}
