using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A tree grid with expand and collapse buttons and an Excel export of the whole tree.</summary>
public partial class DataGridTreeExample
{
    private OmniDataGrid<StatementLine>? _grid;

    [Inject]
    private OmniTableExportDownloader Downloader { get; set; } = default!;

    private Task ExpandAsync() => _grid?.ExpandAllTreeRowsAsync() ?? Task.CompletedTask;

    private Task CollapseAsync() => _grid?.CollapseAllTreeRowsAsync() ?? Task.CompletedTask;

    // A table the example builds itself, every line at its depth: the label indented by two spaces a level.
    private Task ExportAsync()
    {
        static IEnumerable<StatementLine> Flatten(IEnumerable<StatementLine> lines) =>
            lines.SelectMany(line => Flatten(line.Children ?? []).Prepend(line));
        static OmniTableExportCell Amount(decimal value) => new(value.ToString("C0", CultureInfo.CurrentCulture)) { Number = value };
        var document = new OmniTableExportDocument
        {
            Title = "Income statement",
            Columns =
            [
                new("Item", OmniTableExportValueKind.Text),
                new("2025", OmniTableExportValueKind.Number),
                new("2026", OmniTableExportValueKind.Number)
            ],
            Rows = Flatten(StatementLine.Sample)
                .Select(line => (IReadOnlyList<OmniTableExportCell>)[new(new string(' ', line.Level * 2) + line.Label), Amount(line.Previous), Amount(line.Current)])
                .ToArray()
        };
        return Downloader.DownloadAsync(document, OmniTableExportFormat.Excel, "Income statement");
    }
}
