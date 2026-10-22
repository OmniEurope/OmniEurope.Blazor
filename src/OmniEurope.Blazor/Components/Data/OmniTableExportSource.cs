namespace OmniEurope.Blazor.Components;

/// <summary>
/// One column of an <see cref="OmniTableExport{TItem}"/>: its heading and the text a row writes in it. A
/// format escapes the text itself (pipes and line breaks in Markdown, quotes in CSV), so the value is given
/// as the reader should see it.
/// </summary>
/// <typeparam name="TItem">The type of the exported rows.</typeparam>
/// <param name="Title">The column heading.</param>
/// <param name="Value">The cell text of a row; null writes an empty cell.</param>
public sealed record OmniTableExportColumn<TItem>(string Title, Func<TItem, string?> Value);

/// <summary>
/// One page the exporter asks the row provider for. Pages are numbered from 1 and all have
/// <see cref="PageSize"/> rows, so a paginated API takes them as they are.
/// </summary>
/// <param name="Page">One-based number of the page.</param>
/// <param name="PageSize">Rows per page: the export's page size, capped by its row limit.</param>
/// <param name="CancellationToken">The token passed to the export; cancelling it stops the export.</param>
public sealed record OmniTableExportPageRequest(int Page, int PageSize, CancellationToken CancellationToken)
{
    /// <summary>Rows to skip before this page.</summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// What a table export holds and where its rows come from, for a host that has no grid to export (see
/// <see cref="OmniMarkdownExportButton{TItem}"/>). The consumer chooses the title, the columns and the
/// header lines; <see cref="LoadPage"/> reads every announced row, page by page, whatever a grid on screen
/// shows (a page, a virtualized window). <see cref="OmniTableExporter.ReadAsync{TItem}"/> turns it into an
/// <see cref="OmniTableExportDocument"/>, which any format then writes.
/// </summary>
/// <typeparam name="TItem">The type of the exported rows.</typeparam>
public sealed record OmniTableExport<TItem>
{
    /// <summary>The title of the document.</summary>
    public required string Title { get; init; }

    /// <summary>The table's columns, in order.</summary>
    public required IReadOnlyList<OmniTableExportColumn<TItem>> Columns { get; init; }

    /// <summary>
    /// Reads one page of the rows to export and the number of rows the source announces. The
    /// announced count of the first page is the one the header compares the export to.
    /// </summary>
    public required Func<OmniTableExportPageRequest, Task<OmniDataGridResult<TItem>>> LoadPage { get; init; }

    /// <summary>The header lines that describe the rows (application, period, filters, sort...).</summary>
    public IReadOnlyList<OmniTableExportField> Fields { get; init; } = [];

    /// <summary>Most rows the export reads and writes; beyond it the header says the export is truncated.</summary>
    public int RowLimit { get; init; } = 5000;

    /// <summary>Rows asked for per page.</summary>
    public int PageSize { get; init; } = 200;

    /// <summary>
    /// The identity of a row. When set, a row read twice (offset pages over data that grows while it is
    /// read) is written once.
    /// </summary>
    public Func<TItem, object>? RowKey { get; init; }

    /// <summary>The text written instead of the table when no row is exported; a generic one when null.</summary>
    public string? EmptyText { get; init; }
}
