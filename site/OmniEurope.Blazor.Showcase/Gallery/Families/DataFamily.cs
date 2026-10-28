using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Data family of the gallery: its pages, in menu order.</summary>
internal static class DataFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyData", OmniIconName.Database, typeof(DataStrings),
    [
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
