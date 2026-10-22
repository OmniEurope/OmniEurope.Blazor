using System.Text;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Turns an <see cref="OmniTableExportDocument"/> into a file. The package writes Markdown, CSV and Excel
/// (<c>.xlsx</c>) itself, with no dependency; PDF, and any of those three when the host prefers its own, is
/// written by the <see cref="IOmniTableExportRenderer"/> services of the host. A format nobody writes is not
/// supported, and a grid then offers no button for it.
/// <para>
/// The Markdown document is meant to be read by a person or an AI: a title, the header lines the consumer
/// gives (what the rows answer to), the row limit, the generation time, the number of rows exported
/// against the number announced, then the table. <see cref="ReadAsync{TItem}"/> reads every announced row
/// of a source that is not a grid through its page provider, so a paginated or virtualized source exports
/// all of its rows. When fewer rows than announced end up in the table, the document says so and why (the
/// row limit, or data that changed while it was read): a partial export never reads as a complete one.
/// </para>
/// </summary>
public sealed class OmniTableExporter
{
    private static readonly char[] FormulaTriggers = ['=', '+', '-', '@', '\t', '\r'];

    private readonly IStringLocalizer<AppStrings> _text;
    private readonly TimeProvider _time;
    private readonly IReadOnlyList<IOmniTableExportRenderer> _renderers;

    /// <summary>
    /// Creates the exporter. <c>AddOmniEuropeBlazor</c> registers one per scope; a host rarely builds it
    /// by hand.
    /// </summary>
    /// <param name="text">The package texts, read in the current UI culture when a Markdown document is built.</param>
    /// <param name="time">The clock that stamps the generation time of the documents <see cref="ReadAsync{TItem}"/> reads.</param>
    /// <param name="renderers">The host's renderers, asked in order; the first that supports a format writes it.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public OmniTableExporter(IStringLocalizer<AppStrings> text, TimeProvider time, IEnumerable<IOmniTableExportRenderer> renderers)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(renderers);
        _text = text;
        _time = time;
        _renderers = renderers.ToArray();
    }

    /// <summary>
    /// Reads the rows of <paramref name="export"/> page by page up to its row limit, and returns them as a
    /// document any format can write. A page that fails fails the export: the exception propagates and no
    /// document is produced. Reading stops at a page shorter than the page size, or once the rows announced
    /// by the first page (capped by the row limit) are read. A source that returns more rows than it
    /// announced (a count of 0 when it does not know one) is counted instead: reading goes on to a short
    /// page or the row limit, and the document's total is the distinct rows read. When that reading ends on
    /// a full page, more rows may exist: the total is then only a lower bound
    /// (<see cref="OmniTableExportDocument.TotalIsLowerBound"/>) and the Markdown document says "at least"
    /// that many rows, never exactly the limit. Every cell is the text the column gives.
    /// </summary>
    /// <typeparam name="TItem">The type of the exported rows.</typeparam>
    /// <param name="export">What to export and where its rows come from.</param>
    /// <param name="cancellationToken">Checked before each page and passed to the page provider.</param>
    /// <returns>The document with its row counts and generation time.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="export"/>, its columns or its page provider is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="export"/> has no column.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The row limit or the page size of <paramref name="export"/> is below 1.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public async Task<OmniTableExportDocument> ReadAsync<TItem>(OmniTableExport<TItem> export, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(export);
        ArgumentNullException.ThrowIfNull(export.Columns);
        ArgumentNullException.ThrowIfNull(export.LoadPage);
        if (export.Columns.Count == 0)
        {
            throw new ArgumentException("A table export needs at least one column.", nameof(export));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(export.RowLimit, 1, nameof(export));
        ArgumentOutOfRangeException.ThrowIfLessThan(export.PageSize, 1, nameof(export));
        var read = await TableExportReader.ReadAsync(
            (page, pageSize, token) => export.LoadPage(new OmniTableExportPageRequest(page, pageSize, token)),
            export.RowLimit, export.PageSize, export.RowKey, cancellationToken);
        return new OmniTableExportDocument
        {
            Title = export.Title,
            Fields = export.Fields,
            Columns = [.. export.Columns.Select(column => new OmniTableExportColumn(column.Title, OmniTableExportValueKind.Text))],
            Rows = [.. read.Rows.Select(item => (IReadOnlyList<OmniTableExportCell>)[.. export.Columns.Select(column => new OmniTableExportCell(column.Value(item) ?? string.Empty))])],
            GeneratedAt = _time.GetUtcNow(),
            TotalCount = read.TotalCount,
            RowLimit = export.RowLimit,
            TotalIsLowerBound = read.TotalIsLowerBound,
            Culture = CultureInfo.CurrentCulture.Name,
            EmptyText = export.EmptyText
        };
    }

    /// <summary>Whether a file can be written in <paramref name="format"/>, by the package or by a host renderer.</summary>
    /// <param name="format">The format asked for.</param>
    /// <returns>True for Markdown, CSV and Excel, and for any format a registered renderer supports.</returns>
    public bool Supports(OmniTableExportFormat format) =>
        format is OmniTableExportFormat.Markdown or OmniTableExportFormat.Csv or OmniTableExportFormat.Excel || RendererOf(format) is not null;

    /// <summary>Writes the document in <paramref name="format"/>.</summary>
    /// <param name="document">The rows to write.</param>
    /// <param name="format">The format of the file.</param>
    /// <param name="cancellationToken">Passed to the host's renderer.</param>
    /// <returns>The file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    /// <exception cref="NotSupportedException">Nobody writes <paramref name="format"/> (see <see cref="Supports"/>).</exception>
    public async Task<OmniTableExportFile> RenderAsync(OmniTableExportDocument document, OmniTableExportFormat format, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (RendererOf(format) is { } renderer)
        {
            return await renderer.RenderAsync(document, format, cancellationToken);
        }

        return format switch
        {
            OmniTableExportFormat.Markdown => new OmniTableExportFile(Encoding.UTF8.GetBytes(ToMarkdown(document)), "text/markdown;charset=utf-8", "md"),
            // The byte order mark is what makes a spreadsheet read the file as UTF-8.
            OmniTableExportFormat.Csv => new OmniTableExportFile(
                (byte[])[.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(ToCsv(document))], "text/csv;charset=utf-8", "csv"),
            OmniTableExportFormat.Excel => new OmniTableExportFile(ToXlsx(document), XlsxContentType, "xlsx"),
            _ => throw new NotSupportedException($"No IOmniTableExportRenderer is registered for the {format} format.")
        };
    }

    /// <summary>
    /// The Markdown document of the rows: the title, the header lines, the row limit, the generation
    /// time (UTC), the rows written against the rows announced (with a note saying why when the table is
    /// partial), then the table. The texts come from the package resources in the current UI culture and
    /// the numbers are written in the current culture; a cell is written as its text, with its pipes
    /// escaped and its line breaks replaced.
    /// </summary>
    /// <param name="document">The rows to write.</param>
    /// <returns>The Markdown document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="document"/> has no column.</exception>
    public string ToMarkdown(OmniTableExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Columns.Count == 0)
        {
            throw new ArgumentException("A Markdown table export needs at least one column.", nameof(document));
        }

        var rowLimit = Math.Max(1, document.RowLimit);
        var rows = document.Rows;
        // A count keeps its number beside its grouped text: the wording of a line follows it (1 row, 2 rows).
        PluralCount Count(int value) => new(value, "N0");
        string Format(string key, params object[] args) => PluralMessage.Localize(_text, key, args);

        var markdown = new StringBuilder();
        markdown.Append("# ").AppendLine(EscapeInline(document.Title));
        markdown.AppendLine();
        foreach (var field in document.Fields)
        {
            markdown.Append("- ").AppendLine(Format("MarkdownExportField", EscapeInline(field.Label), EscapeInline(field.Value)));
        }

        markdown.Append("- ").AppendLine(Format("MarkdownExportRowLimit", Count(rowLimit)));
        markdown.Append("- ").AppendLine(Format("MarkdownExportGeneratedAt",
            document.GeneratedAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)));
        if (document.TotalIsLowerBound)
        {
            markdown.Append("- ").AppendLine(Format("MarkdownExportRowCountAtLeast", Count(rows.Count), Count(document.TotalCount)));
            markdown.AppendLine();
            markdown.Append("> ").AppendLine(Format("MarkdownExportLimitReachedWithoutTotal", Count(rowLimit)));
        }
        else
        {
            markdown.Append("- ").AppendLine(Format("MarkdownExportRowCount", Count(rows.Count), Count(document.TotalCount)));
        }

        if (!document.TotalIsLowerBound && rows.Count < document.TotalCount)
        {
            markdown.AppendLine();
            markdown.Append("> ").AppendLine(document.TotalCount > rowLimit
                ? Format("MarkdownExportTruncated", Count(rows.Count), Count(document.TotalCount), Count(rowLimit))
                : Format("MarkdownExportIncomplete", Count(rows.Count), Count(document.TotalCount)));
        }

        markdown.AppendLine();
        if (rows.Count == 0)
        {
            markdown.AppendLine(EscapeInline(document.EmptyText ?? _text["MarkdownExportEmpty"].Value));
            return markdown.ToString();
        }

        markdown.Append('|');
        foreach (var column in document.Columns)
        {
            markdown.Append(' ').Append(EscapeCell(column.Title)).Append(" |");
        }

        markdown.AppendLine();
        markdown.Append('|').Insert(markdown.Length, " --- |", document.Columns.Count).AppendLine();
        foreach (var row in rows)
        {
            markdown.Append('|');
            for (var index = 0; index < document.Columns.Count; index++)
            {
                markdown.Append(' ').Append(EscapeCell(row[index].Text)).Append(" |");
            }

            markdown.AppendLine();
        }

        return markdown.ToString();
    }

    /// <summary>
    /// A Markdown table cell's text: a pipe would end the cell and a line break the row, so pipes are
    /// escaped and line breaks become <c>&lt;br&gt;</c>. Null is an empty cell.
    /// </summary>
    /// <param name="value">The cell text as the reader should see it.</param>
    /// <returns>The escaped cell text.</returns>
    public static string EscapeCell(string? value) => value is null
        ? string.Empty
        : value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);

    /// <summary>A Markdown heading or header value on one line: line breaks become spaces. Null is empty.</summary>
    /// <param name="value">The text to write on one line.</param>
    /// <returns>The text with its line breaks replaced.</returns>
    public static string EscapeInline(string? value) => value is null
        ? string.Empty
        : value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');

    /// <summary>
    /// The rows as comma-separated values (RFC 4180): a heading line, then one line per row, ended by
    /// CRLF. The separator is a semicolon when the document's culture writes decimals with a comma,
    /// as spreadsheets expect there. A number is written as its value in that culture, without
    /// grouping; every other cell as its text. A text that starts with <c>=</c>, <c>+</c>, <c>-</c>,
    /// <c>@</c>, a tab or a carriage return is preceded by an apostrophe, so a spreadsheet does not run
    /// it as a formula.
    /// </summary>
    /// <param name="document">The rows to write.</param>
    /// <returns>The CSV text, without a byte order mark.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public static string ToCsv(OmniTableExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var culture = CultureOf(document);
        var separator = culture.NumberFormat.NumberDecimalSeparator == "," ? ';' : ',';
        var csv = new StringBuilder();
        AppendLine(csv, document.Columns.Select(column => Guarded(column.Title)), separator);
        foreach (var row in document.Rows)
        {
            AppendLine(csv, row.Select(cell => cell.Number is { } number ? number.ToString(culture) : Guarded(cell.Text)), separator);
        }

        return csv.ToString();
    }

    /// <summary>
    /// The rows as an Office Open XML workbook (<c>.xlsx</c>), written with the base library alone, so in
    /// WebAssembly too: one sheet named after the title (31 characters at most, without the characters a sheet
    /// name refuses), a bold heading row kept on screen while scrolling and carrying a filter, then one row per
    /// row of the document. A number is stored as a number, a date as a date (with its time when it has one), a
    /// boolean as a boolean, any other cell as its text; columns are as wide as their longest text, within
    /// bounds. No cell holds a formula, and a text that starts like one (<c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>)
    /// is marked as text, so editing it does not run it.
    /// </summary>
    /// <param name="document">The rows to write.</param>
    /// <returns>The bytes of the workbook.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public static byte[] ToXlsx(OmniTableExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Internal.XlsxWriter.Write(document);
    }

    /// <summary>The media type of an <c>.xlsx</c> workbook.</summary>
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private IOmniTableExportRenderer? RendererOf(OmniTableExportFormat format) =>
        _renderers.FirstOrDefault(renderer => renderer.Supports(format));

    private static CultureInfo CultureOf(OmniTableExportDocument document)
    {
        try
        {
            return CultureInfo.GetCultureInfo(document.Culture);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static string Guarded(string text) =>
        text.Length > 0 && text.IndexOfAny(FormulaTriggers, 0, 1) == 0 ? "'" + text : text;

    private static void AppendLine(StringBuilder csv, IEnumerable<string> values, char separator)
    {
        var first = true;
        foreach (var value in values)
        {
            if (!first)
            {
                csv.Append(separator);
            }

            first = false;
            if (value.AsSpan().IndexOfAny([separator, '"', '\r', '\n']) >= 0)
            {
                csv.Append('"').Append(value.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
            }
            else
            {
                csv.Append(value);
            }
        }

        csv.Append("\r\n");
    }
}
