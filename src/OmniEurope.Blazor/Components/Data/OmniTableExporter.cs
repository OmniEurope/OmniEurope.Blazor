using System.Text;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Turns an <see cref="OmniTableExportDocument"/> into a file. The package writes Markdown and CSV
/// itself, with no dependency; any other format, and either of those two when the host prefers its
/// own, is written by the <see cref="IOmniTableExportRenderer"/> services of the host. A format nobody
/// writes is not supported, and a grid then offers no button for it.
/// </summary>
public sealed class OmniTableExporter
{
    private static readonly char[] FormulaTriggers = ['=', '+', '-', '@', '\t', '\r'];

    private readonly OmniMarkdownTableExporter _markdown;
    private readonly IReadOnlyList<IOmniTableExportRenderer> _renderers;

    /// <summary>
    /// Creates the exporter. <c>AddOmniEuropeBlazor</c> registers one per scope; a host rarely builds it
    /// by hand.
    /// </summary>
    /// <param name="markdown">The writer of the Markdown document.</param>
    /// <param name="renderers">The host's renderers, asked in order; the first that supports a format writes it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="markdown"/> or <paramref name="renderers"/> is null.</exception>
    public OmniTableExporter(OmniMarkdownTableExporter markdown, IEnumerable<IOmniTableExportRenderer> renderers)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(renderers);
        _markdown = markdown;
        _renderers = renderers.ToArray();
    }

    /// <summary>Whether a file can be written in <paramref name="format"/>, by the package or by a host renderer.</summary>
    /// <param name="format">The format asked for.</param>
    /// <returns>True for Markdown and CSV, and for any format a registered renderer supports.</returns>
    public bool Supports(OmniTableExportFormat format) =>
        format is OmniTableExportFormat.Markdown or OmniTableExportFormat.Csv || RendererOf(format) is not null;

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
            _ => throw new NotSupportedException($"No IOmniTableExportRenderer is registered for the {format} format.")
        };
    }

    /// <summary>
    /// The Markdown document of the rows: the title, the header lines, the row limit, the generation
    /// time, the rows written against the rows announced, then the table, as
    /// <see cref="OmniMarkdownTableExporter"/> writes it.
    /// </summary>
    /// <param name="document">The rows to write.</param>
    /// <returns>The Markdown document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is null.</exception>
    public string ToMarkdown(OmniTableExportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var columns = document.Columns
            .Select((column, index) => new OmniMarkdownTableColumn<IReadOnlyList<OmniTableExportCell>>(column.Title, row => row[index].Text))
            .ToArray();
        var export = new OmniMarkdownTableExport<IReadOnlyList<OmniTableExportCell>>
        {
            Title = document.Title,
            Columns = columns,
            Fields = [.. document.Fields.Select(field => new OmniMarkdownTableField(field.Label, field.Value))],
            RowLimit = Math.Max(1, document.RowLimit),
            // The rows are already read: the document is built from them, this provider is never asked.
            LoadPage = _ => Task.FromResult(new OmniDataGridResult<IReadOnlyList<OmniTableExportCell>>([], 0))
        };
        return _markdown.Build(export, document.Rows, document.TotalCount, document.GeneratedAt, document.TotalIsLowerBound);
    }

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
