using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Data family of the gallery: its pages, in menu order.</summary>
internal static class DataFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyData", OmniIconName.Database, typeof(DataStrings),
    [
        new("data-grid", "DataGridTitle", "DataGridDescription", OmniIconName.Table,
            ["OmniDataGrid", "OmniDataGridColumn", "OmniDataGridFilterCombo", "OmniDataGridFilterMultiSelect", "OmniDataGridFilterDateRange", "OmniMarkdownExportButton"],
            [
                "Data/DataGridExample.razor", "Data/DataGridExample.razor.cs",
                "Data/DataGridFlightColumns.razor", "Data/DataGridFlightColumns.razor.cs",
                "Data/DataGridFlightDetail.razor", "Data/DataGridFlightDetail.razor.cs",
                "Data/DataGridDateRangeExample.razor", "Data/DataGridDateRangeExample.razor.cs",
                "Data/Flight.cs"
            ]),
        new("data-grid-variants", "DataGridVariantsTitle", "DataGridVariantsDescription", OmniIconName.Rows,
            ["OmniDataGrid", "OmniDataGridFilterDate", "OmniDataGridFilterDateRange", "OmniImage", "OmniLink", "OmniTableExportDownloader"],
            [
                "Data/DataGridCasesExample.razor",
                "Data/DataGridContentSizingExample.razor", "Data/DataGridContentSizingExample.razor.cs",
                "Data/DataGridTemplatedSizingExample.razor", "Data/DataGridTemplatedSizingExample.razor.cs",
                "Data/DataGridTruncationExample.razor",
                "Data/DataGridCappedExample.razor",
                "Data/DataGridRunStatusExample.razor", "Data/DataGridRunStatusExample.razor.cs",
                "Data/DataGridTreeExample.razor", "Data/DataGridTreeExample.razor.cs",
                "Data/DataGridEditExample.razor", "Data/DataGridEditExample.razor.cs",
                "Data/DataGridPicturesExample.razor", "Data/DataGridPicturesExample.razor.cs",
                "Data/DataGridFillExample.razor", "Data/DataGridFillExample.razor.cs",
                "Data/DataGridFillBoxExample.razor", "Data/DataGridFillBoxExample.razor.cs",
                "Data/CaseRow.cs", "Data/SiteRow.cs", "Data/StatementLine.cs", "Data/PipelineRun.cs"
            ]),
        new("data-grid-runs", "DataGridRunsTitle", "DataGridRunsDescription", OmniIconName.Gauge,
            ["OmniDataGrid", "OmniDataGridColumn"],
            [
                "Data/DataGridRunsExample.razor", "Data/DataGridRunsExample.razor.cs",
                "Data/PipelineRun.cs"
            ]),
        new("data-grid-advanced", "DataGridAdvancedTitle", "DataGridAdvancedDescription", OmniIconName.Sliders,
            ["OmniDataGrid", "OmniDataGridFilterCombo", "OmniDataGridFilterMultiSelect"],
            [
                "Data/DataGridAdvancedExample.razor", "Data/DataGridAdvancedExample.razor.cs",
                "Data/CaseRow.cs"
            ]),
        new("data-list", "DataListTitle", "DataListDescription", OmniIconName.ListBullets,
            ["OmniDataList", "OmniPager", "OmniLogViewer", "OmniAnsiText", "OmniKanban", "OmniMarkdownExportButton"],
            [
                "Data/DataListExample.razor", "Data/DataListExample.razor.cs",
                "Data/DataListGalleryExample.razor", "Data/DataListGalleryExample.razor.cs",
                "Data/DataListCardsExample.razor", "Data/DataListCardsExample.razor.cs",
                "Data/DataListVirtualExample.razor", "Data/DataListVirtualExample.razor.cs",
                "Data/DataListLogExample.razor", "Data/DataListLogExample.razor.cs",
                "Data/DataListAnsiExample.razor", "Data/DataListAnsiExample.razor.cs",
                "Data/DataListKanbanExample.razor", "Data/DataListKanbanExample.razor.cs",
                "Data/Product.cs"
            ]),
        new("tree", "TreeTitle", "TreeDescription", OmniIconName.Graph,
            ["OmniTree", "OmniTreeItem", "OmniDataGrid"],
            [
                "Data/TreeStaticExample.razor", "Data/TreeStaticExample.razor.cs",
                "Data/TreeDataExample.razor", "Data/TreeDataExample.razor.cs",
                "Data/TreeCustomContentExample.razor", "Data/TreeCustomContentExample.razor.cs",
                "Data/TreeMoveExample.razor", "Data/TreeMoveExample.razor.cs",
                "Data/TreeAccountsExample.razor", "Data/TreeAccountsExample.razor.cs"
            ]),
        new("spreadsheet", "SpreadsheetTitle", "SpreadsheetDescription", OmniIconName.Calculator,
            ["OmniSpreadsheet"],
            [
                "Data/SpreadsheetExample.razor", "Data/SpreadsheetExample.razor.cs"
            ]),
        new("collections", "CollectionsTitle", "CollectionsDescription", OmniIconName.Columns,
            ["OmniKanban", "OmniLogViewer"],
            [
                "Data/KanbanExample.razor", "Data/KanbanExample.razor.cs",
                "Data/LogViewerExample.razor", "Data/LogViewerExample.razor.cs"
            ])
    ]);
}
