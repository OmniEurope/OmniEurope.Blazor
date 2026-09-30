namespace OmniEurope.Blazor.Components;

/// <summary>
/// A file format a table is exported to (<see cref="OmniDataGrid{TItem}.ExportFormats"/>). The package
/// writes <see cref="Markdown"/> and <see cref="Csv"/> itself; the others are written by the host,
/// through an <see cref="IOmniTableExportRenderer"/>.
/// </summary>
public enum OmniTableExportFormat
{
    /// <summary>A Markdown document: what the rows answer to, then the table. Written by the package.</summary>
    Markdown,

    /// <summary>Comma-separated values (RFC 4180), the table alone. Written by the package.</summary>
    Csv,

    /// <summary>A spreadsheet workbook (<c>.xlsx</c>). Written by the host's renderer.</summary>
    Excel,

    /// <summary>A PDF document. Written by the host's renderer.</summary>
    Pdf
}
