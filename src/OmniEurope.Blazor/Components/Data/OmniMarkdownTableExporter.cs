using System.Text;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Writes rows as a Markdown document meant to be read by a person or an AI: a title, the header
/// lines the consumer gives (what the rows answer to), the row limit, the generation time, the number
/// of rows exported against the number announced, then the table.
/// <para>
/// <see cref="ExportAsync{TItem}"/> reads every announced row through the export's page provider, not
/// the rows a grid happens to hold, so a paginated or virtualized grid exports all of its rows. When
/// fewer rows than announced end up in the table, the document says so and why (the row limit, or
/// data that changed while it was read): a partial export never reads as a complete one.
/// </para>
/// </summary>
public sealed class OmniMarkdownTableExporter
{
    private readonly IStringLocalizer<AppStrings> _text;
    private readonly TimeProvider _time;

    /// <summary>
    /// Creates the exporter. <c>AddOmniEuropeBlazor</c> registers one per scope; a host rarely builds it
    /// by hand.
    /// </summary>
    /// <param name="text">The package texts, read in the current UI culture when a document is built.</param>
    /// <param name="time">The clock that stamps each document's generation time.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> or <paramref name="time"/> is null.</exception>
    public OmniMarkdownTableExporter(IStringLocalizer<AppStrings> text, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(time);
        _text = text;
        _time = time;
    }

    /// <summary>
    /// Reads the rows page by page up to the export's row limit, then builds the document. A page that
    /// fails fails the export: the exception propagates and no document is produced. Reading stops at
    /// a page shorter than the page size, or once the rows announced by the first page (capped by the
    /// row limit) are read. A source that returns more rows than it announced (a count of 0 when it
    /// does not know one) is counted instead: reading goes on to a short page or the row limit, and the
    /// document's total is the distinct rows read. When that reading ends on a full page, more rows may
    /// exist: the total is then only a lower bound (<see cref="OmniMarkdownTableDocument.TotalIsLowerBound"/>)
    /// and the document says "at least" that many rows, never exactly the limit.
    /// </summary>
    /// <typeparam name="TItem">The type of the exported rows.</typeparam>
    /// <param name="export">What to export and where its rows come from.</param>
    /// <param name="cancellationToken">Checked before each page and passed to the page provider.</param>
    /// <returns>The document with its row counts and generation time.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="export"/>, its columns or its page provider is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="export"/> has no column.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The row limit or the page size of <paramref name="export"/> is below 1.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<OmniMarkdownTableDocument> ExportAsync<TItem>(OmniMarkdownTableExport<TItem> export, CancellationToken cancellationToken = default)
    {
        Validate(export);
        var pageSize = Math.Min(export.PageSize, export.RowLimit);
        var maxPages = (export.RowLimit + pageSize - 1) / pageSize;
        var rows = new List<TItem>();
        var seen = new HashSet<object>();
        var totalCount = 0;
        // A source whose pages hold more rows than it announced has announced no usable count (0 when
        // it does not know): the exporter then counts the rows itself, reading on to a short page or
        // the row limit.
        var counting = false;
        var lastPageFull = false;
        for (var page = 1; page <= maxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await export.LoadPage(new OmniMarkdownTablePageRequest(page, pageSize, cancellationToken));
            if (page == 1)
            {
                totalCount = result.TotalCount;
            }

            foreach (var item in result.Items)
            {
                if (export.RowKey is null || seen.Add(export.RowKey(item)))
                {
                    rows.Add(item);
                }
            }

            counting |= rows.Count > totalCount;
            lastPageFull = result.Items.Count >= pageSize;
            if (!lastPageFull || rows.Count >= (counting ? export.RowLimit : Math.Min(totalCount, export.RowLimit)))
            {
                break;
            }
        }

        // A counted source: its total is the distinct rows read, including those past the limit. Reading
        // that stopped on a full page (the limit, not the end of the data) leaves rows unread, so that
        // total is only a lower bound.
        var totalIsLowerBound = counting && lastPageFull;
        if (counting)
        {
            totalCount = rows.Count;
        }

        if (rows.Count > export.RowLimit)
        {
            rows.RemoveRange(export.RowLimit, rows.Count - export.RowLimit);
        }

        var generatedAt = _time.GetUtcNow();
        return new OmniMarkdownTableDocument(Build(export, rows, totalCount, generatedAt, totalIsLowerBound), rows.Count, totalCount,
            totalCount > export.RowLimit, generatedAt)
        {
            TotalIsLowerBound = totalIsLowerBound
        };
    }

    /// <summary>
    /// Builds the document from rows already read. <paramref name="totalCount"/> is the number of rows
    /// the source announced, or the least number of rows it holds when
    /// <paramref name="totalIsLowerBound"/> is true; the texts come from the package resources in the
    /// current UI culture and the numbers are written in the current culture.
    /// </summary>
    /// <typeparam name="TItem">The type of the exported rows.</typeparam>
    /// <param name="export">The title, columns, header lines and row limit of the document.</param>
    /// <param name="rows">The rows written in the table, in order.</param>
    /// <param name="totalCount">The number of rows the source announced; fewer <paramref name="rows"/> adds a note saying the table is partial.</param>
    /// <param name="generatedAt">The generation time, written in UTC.</param>
    /// <param name="totalIsLowerBound">
    /// True when the source announced no usable total and more rows than <paramref name="totalCount"/>
    /// may exist: the document writes "at least" that total and a note saying the row limit stopped the
    /// reading. False, the default, writes <paramref name="totalCount"/> as the announced total.
    /// </param>
    /// <returns>The Markdown document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="export"/>, its columns, its page provider or <paramref name="rows"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="export"/> has no column.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The row limit or the page size of <paramref name="export"/> is below 1.</exception>
    public string Build<TItem>(OmniMarkdownTableExport<TItem> export, IReadOnlyList<TItem> rows, int totalCount, DateTimeOffset generatedAt,
        bool totalIsLowerBound = false)
    {
        Validate(export);
        ArgumentNullException.ThrowIfNull(rows);
        var culture = CultureInfo.CurrentCulture;
        string Count(int value) => value.ToString("N0", culture);
        string Format(string key, params object[] args) => string.Format(culture, _text[key].Value, args);

        var markdown = new StringBuilder();
        markdown.Append("# ").AppendLine(EscapeInline(export.Title));
        markdown.AppendLine();
        foreach (var field in export.Fields)
        {
            markdown.Append("- ").AppendLine(Format("MarkdownExportField", EscapeInline(field.Label), EscapeInline(field.Value)));
        }

        markdown.Append("- ").AppendLine(Format("MarkdownExportRowLimit", Count(export.RowLimit)));
        markdown.Append("- ").AppendLine(Format("MarkdownExportGeneratedAt",
            generatedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)));
        if (totalIsLowerBound)
        {
            markdown.Append("- ").AppendLine(Format("MarkdownExportRowCountAtLeast", Count(rows.Count), Count(totalCount)));
            markdown.AppendLine();
            markdown.Append("> ").AppendLine(Format("MarkdownExportLimitReachedWithoutTotal", Count(export.RowLimit)));
        }
        else
        {
            markdown.Append("- ").AppendLine(Format("MarkdownExportRowCount", Count(rows.Count), Count(totalCount)));
        }

        if (!totalIsLowerBound && rows.Count < totalCount)
        {
            markdown.AppendLine();
            markdown.Append("> ").AppendLine(totalCount > export.RowLimit
                ? Format("MarkdownExportTruncated", Count(rows.Count), Count(totalCount), Count(export.RowLimit))
                : Format("MarkdownExportIncomplete", Count(rows.Count), Count(totalCount)));
        }

        markdown.AppendLine();
        if (rows.Count == 0)
        {
            markdown.AppendLine(EscapeInline(export.EmptyText ?? _text["MarkdownExportEmpty"].Value));
            return markdown.ToString();
        }

        markdown.Append('|');
        foreach (var column in export.Columns)
        {
            markdown.Append(' ').Append(EscapeCell(column.Title)).Append(" |");
        }

        markdown.AppendLine();
        markdown.Append('|').Insert(markdown.Length, " --- |", export.Columns.Count).AppendLine();
        foreach (var row in rows)
        {
            markdown.Append('|');
            foreach (var column in export.Columns)
            {
                markdown.Append(' ').Append(EscapeCell(column.Value(row))).Append(" |");
            }

            markdown.AppendLine();
        }

        return markdown.ToString();
    }

    /// <summary>
    /// A table cell's text: a pipe would end the cell and a line break the row, so pipes are escaped
    /// and line breaks become <c>&lt;br&gt;</c>. Null is an empty cell.
    /// </summary>
    /// <param name="value">The cell text as the reader should see it.</param>
    /// <returns>The escaped cell text.</returns>
    public static string EscapeCell(string? value) => value is null
        ? string.Empty
        : value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);

    /// <summary>A heading or header value on one line: line breaks become spaces. Null is empty.</summary>
    /// <param name="value">The text to write on one line.</param>
    /// <returns>The text with its line breaks replaced.</returns>
    public static string EscapeInline(string? value) => value is null
        ? string.Empty
        : value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');

    private static void Validate<TItem>(OmniMarkdownTableExport<TItem> export)
    {
        ArgumentNullException.ThrowIfNull(export);
        ArgumentNullException.ThrowIfNull(export.Columns);
        ArgumentNullException.ThrowIfNull(export.LoadPage);
        if (export.Columns.Count == 0)
        {
            throw new ArgumentException("A Markdown table export needs at least one column.", nameof(export));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(export.RowLimit, 1, nameof(export));
        ArgumentOutOfRangeException.ThrowIfLessThan(export.PageSize, 1, nameof(export));
    }
}
