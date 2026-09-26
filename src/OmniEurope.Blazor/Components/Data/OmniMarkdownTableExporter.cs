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

    public OmniMarkdownTableExporter(IStringLocalizer<AppStrings> text, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(time);
        _text = text;
        _time = time;
    }

    /// <summary>
    /// Reads the rows page by page up to the export's row limit, then builds the document. A page that
    /// fails fails the export: the exception propagates and no document is produced.
    /// </summary>
    public async Task<OmniMarkdownTableDocument> ExportAsync<TItem>(OmniMarkdownTableExport<TItem> export, CancellationToken cancellationToken = default)
    {
        Validate(export);
        var pageSize = Math.Min(export.PageSize, export.RowLimit);
        var maxPages = (export.RowLimit + pageSize - 1) / pageSize;
        var rows = new List<TItem>();
        var seen = new HashSet<object>();
        var totalCount = 0;
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

            if (result.Items.Count < pageSize || rows.Count >= Math.Min(totalCount, export.RowLimit))
            {
                break;
            }
        }

        if (rows.Count > export.RowLimit)
        {
            rows.RemoveRange(export.RowLimit, rows.Count - export.RowLimit);
        }

        var generatedAt = _time.GetUtcNow();
        return new OmniMarkdownTableDocument(Build(export, rows, totalCount, generatedAt), rows.Count, totalCount,
            totalCount > export.RowLimit, generatedAt);
    }

    /// <summary>
    /// Builds the document from rows already read. <paramref name="totalCount"/> is the number of rows
    /// the source announced; the texts come from the package resources in the current UI culture and
    /// the numbers are written in the current culture.
    /// </summary>
    public string Build<TItem>(OmniMarkdownTableExport<TItem> export, IReadOnlyList<TItem> rows, int totalCount, DateTimeOffset generatedAt)
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
        markdown.Append("- ").AppendLine(Format("MarkdownExportRowCount", Count(rows.Count), Count(totalCount)));
        if (rows.Count < totalCount)
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
    public static string EscapeCell(string? value) => value is null
        ? string.Empty
        : value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", "<br>", StringComparison.Ordinal)
            .Replace("\n", "<br>", StringComparison.Ordinal)
            .Replace("\r", "<br>", StringComparison.Ordinal);

    /// <summary>A heading or header value on one line: line breaks become spaces. Null is empty.</summary>
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
