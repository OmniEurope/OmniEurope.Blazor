namespace OmniEurope.Blazor.Components;

/// <summary>
/// Reads a table file format that <see cref="OmniHtmlEditorAction.ImportTable"/> does not read itself (it
/// reads CSV and TSV): a spreadsheet read by a server, for instance.
/// </summary>
/// <param name="Extensions">The file extensions it reads, with their dot: <c>.xlsx</c>.</param>
/// <param name="Read">
/// Reads the file (its name and its content) into rows of cell texts, first row first. Rows may differ
/// in length; the editor pads them. An <see cref="IOException"/>, <see cref="HttpRequestException"/>,
/// <see cref="InvalidOperationException"/>, <see cref="FormatException"/> or <see cref="NotSupportedException"/>
/// means the file could not be read, and the editor says so.
/// </param>
public sealed record OmniHtmlEditorTableReader(
    IReadOnlyList<string> Extensions,
    Func<string, Stream, Task<IReadOnlyList<IReadOnlyList<string>>>> Read);
