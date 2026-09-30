namespace OmniEurope.Blazor.Components;

/// <summary>
/// Writes a table export in a format the package does not write itself (a spreadsheet, a PDF). The
/// host registers one in its container; the grid then offers the formats it supports. A renderer that
/// supports <see cref="OmniTableExportFormat.Markdown"/> or <see cref="OmniTableExportFormat.Csv"/>
/// replaces the package's own writer for that format.
/// </summary>
public interface IOmniTableExportRenderer
{
    /// <summary>Whether this renderer writes <paramref name="format"/>.</summary>
    /// <param name="format">The format asked for.</param>
    /// <returns>True when <see cref="RenderAsync"/> can write it.</returns>
    bool Supports(OmniTableExportFormat format);

    /// <summary>Writes the document in a format this renderer supports.</summary>
    /// <param name="document">The rows to write.</param>
    /// <param name="format">The format, one <see cref="Supports"/> accepts.</param>
    /// <param name="cancellationToken">Cancelled when the reader cancels the export or leaves the page.</param>
    /// <returns>The file.</returns>
    Task<OmniTableExportFile> RenderAsync(OmniTableExportDocument document, OmniTableExportFormat format, CancellationToken cancellationToken);
}
