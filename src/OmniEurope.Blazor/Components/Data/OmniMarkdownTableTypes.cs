namespace OmniEurope.Blazor.Components;

/// <summary>
/// One column of a Markdown table export: its heading and the text a row writes in it. The text is
/// escaped by the exporter (pipes, line breaks), so the value is given as the reader should see it.
/// </summary>
/// <typeparam name="TItem">The type of the exported rows.</typeparam>
/// <param name="Title">The column heading.</param>
/// <param name="Value">The cell text of a row; null writes an empty cell.</param>
public sealed record OmniMarkdownTableColumn<TItem>(string Title, Func<TItem, string?> Value);

/// <summary>
/// One line of the export header, supplied by the consumer: the application, the period, a filter, a
/// sort, anything that says which rows the table answers to.
/// </summary>
/// <param name="Label">What the line describes, e.g. "Period".</param>
/// <param name="Value">Its value, e.g. "last 24 hours".</param>
public sealed record OmniMarkdownTableField(string Label, string Value);

/// <summary>
/// One page the exporter asks the row provider for. Pages are numbered from 1 and all have
/// <see cref="PageSize"/> rows, so a paginated API takes them as they are.
/// </summary>
/// <param name="Page">One-based number of the page.</param>
/// <param name="PageSize">Rows per page: the export's page size, capped by its row limit.</param>
/// <param name="CancellationToken">The token passed to the export; cancelling it stops the export.</param>
public sealed record OmniMarkdownTablePageRequest(int Page, int PageSize, CancellationToken CancellationToken)
{
    /// <summary>Rows to skip before this page.</summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// What a Markdown table export holds and where its rows come from. The consumer chooses the title,
/// the columns and the header lines; <see cref="LoadPage"/> reads every announced row, page by page,
/// whatever the grid on screen shows (a page, a virtualized window).
/// </summary>
/// <typeparam name="TItem">The type of the exported rows.</typeparam>
public sealed record OmniMarkdownTableExport<TItem>
{
    /// <summary>The level-one heading of the document.</summary>
    public required string Title { get; init; }

    /// <summary>The table's columns, in order.</summary>
    public required IReadOnlyList<OmniMarkdownTableColumn<TItem>> Columns { get; init; }

    /// <summary>
    /// Reads one page of the rows to export and the number of rows the source announces. The
    /// announced count of the first page is the one the header compares the export to.
    /// </summary>
    public required Func<OmniMarkdownTablePageRequest, Task<OmniDataGridResult<TItem>>> LoadPage { get; init; }

    /// <summary>The header lines that describe the rows (application, period, filters, sort...).</summary>
    public IReadOnlyList<OmniMarkdownTableField> Fields { get; init; } = [];

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

/// <summary>
/// A finished Markdown table export.
/// </summary>
/// <param name="Markdown">The document.</param>
/// <param name="RowCount">Rows written in the table.</param>
/// <param name="TotalCount">
/// Rows the source announced; the distinct rows read when it returned more than it announced, then
/// possibly a lower bound only (<see cref="TotalIsLowerBound"/>, see
/// <see cref="OmniMarkdownTableExporter.ExportAsync{TItem}"/>).
/// </param>
/// <param name="Truncated">
/// True when the total is known to exceed the limit: announced by the source, or counted past the
/// limit. A lower-bound total equal to the limit leaves it false, since the rows beyond it are unknown.
/// </param>
/// <param name="GeneratedAt">When the document was generated.</param>
public sealed record OmniMarkdownTableDocument(string Markdown, int RowCount, int TotalCount, bool Truncated, DateTimeOffset GeneratedAt)
{
    /// <summary>
    /// True when the source announced no usable total and the reading stopped on a full page at the row
    /// limit: <see cref="TotalCount"/> is then the least number of rows the source holds, more may exist,
    /// and the document says "at least". False for an announced total, or a counted one that ended on a
    /// short page.
    /// </summary>
    public bool TotalIsLowerBound { get; init; }

    /// <summary>True when every row of the source is in the table; never while the total is only a lower bound.</summary>
    public bool IsComplete => RowCount >= TotalCount && !TotalIsLowerBound;
}
