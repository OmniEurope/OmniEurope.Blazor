using System.Text.Json.Serialization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// One line of an export's header: the application, the period, a filter, anything that says which
/// rows the table answers to.
/// </summary>
/// <param name="Label">What the line describes, e.g. "Period".</param>
/// <param name="Value">Its value, e.g. "last 24 hours".</param>
public sealed record OmniTableExportField(string Label, string Value);

/// <summary>One column of an exported table.</summary>
/// <param name="Title">The column heading.</param>
/// <param name="Kind">What its cells hold.</param>
public sealed record OmniTableExportColumn(string Title, OmniTableExportValueKind Kind);

/// <summary>
/// One cell of an exported table: the text the reader saw, and the value behind it when it is a
/// number, a date or a boolean, so a spreadsheet can store the value and a document the text.
/// </summary>
/// <param name="Text">The cell as the grid formats it; empty for no value.</param>
public sealed record OmniTableExportCell(string Text)
{
    /// <summary>The number behind the text, for a <see cref="OmniTableExportValueKind.Number"/> cell.</summary>
    public decimal? Number { get; init; }

    /// <summary>The date behind the text, for a <see cref="OmniTableExportValueKind.Date"/> cell.</summary>
    public DateTimeOffset? Date { get; init; }

    /// <summary>The boolean behind the text, for a <see cref="OmniTableExportValueKind.Boolean"/> cell.</summary>
    public bool? Boolean { get; init; }
}

/// <summary>
/// The rows of a table as any format writes them: a title, the header lines, the columns and the
/// cells, with what the reading found (how many rows the source announced, whether the row limit cut
/// it). It holds no delegate and no component type, so it serializes to JSON as it is: a browser host
/// sends it to its server, which writes the spreadsheet or the PDF there.
/// </summary>
public sealed record OmniTableExportDocument
{
    /// <summary>The title of the document.</summary>
    public required string Title { get; init; }

    /// <summary>The header lines that describe the rows.</summary>
    public IReadOnlyList<OmniTableExportField> Fields { get; init; } = [];

    /// <summary>The columns, in order.</summary>
    public required IReadOnlyList<OmniTableExportColumn> Columns { get; init; }

    /// <summary>The rows, each with one cell per column.</summary>
    public required IReadOnlyList<IReadOnlyList<OmniTableExportCell>> Rows { get; init; }

    /// <summary>When the rows were read.</summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Rows the source announced; the rows counted when it announced none, then possibly a lower bound
    /// only (<see cref="TotalIsLowerBound"/>).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>Most rows the export read.</summary>
    public int RowLimit { get; init; }

    /// <summary>
    /// True when the source announced no usable total and the reading stopped on a full page at the row
    /// limit: more rows than <see cref="TotalCount"/> may exist.
    /// </summary>
    public bool TotalIsLowerBound { get; init; }

    /// <summary>
    /// Name of the culture the texts were formatted in (<c>fr-FR</c>); it decides the separators a CSV
    /// file is written with. Empty reads as the invariant culture.
    /// </summary>
    public string Culture { get; init; } = string.Empty;

    /// <summary>Rows in the table.</summary>
    [JsonIgnore]
    public int RowCount => Rows.Count;

    /// <summary>True when the total is known to exceed the row limit.</summary>
    [JsonIgnore]
    public bool Truncated => TotalCount > RowLimit;

    /// <summary>True when every row of the source is in the table; never while the total is only a lower bound.</summary>
    [JsonIgnore]
    public bool IsComplete => RowCount >= TotalCount && !TotalIsLowerBound;
}

/// <summary>An exported file, ready to be handed to the browser.</summary>
/// <param name="Content">The bytes of the file.</param>
/// <param name="ContentType">Its media type, e.g. <c>application/pdf</c>.</param>
/// <param name="Extension">Its file name extension, without the dot, e.g. <c>pdf</c>.</param>
public sealed record OmniTableExportFile(ReadOnlyMemory<byte> Content, string ContentType, string Extension);
