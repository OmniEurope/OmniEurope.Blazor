using System.Text;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniHtmlEditor"/> shows under its toolbar: the link field, the special characters
/// and the table import, one at a time. Errors are kept as resource keys, localized when drawn.
/// </summary>
internal sealed class HtmlEditorPanels
{
    /// <summary>The panel shown besides the link field.</summary>
    internal HtmlEditorPanel Current { get; private set; }

    /// <summary>Whether the link field is shown.</summary>
    internal bool LinkOpen { get; private set; }

    /// <summary>Whether the link field takes the focus after the next render.</summary>
    internal bool FocusLink { get; set; }

    /// <summary>The address typed in the link field.</summary>
    internal string LinkUrl { get; set; } = string.Empty;

    /// <summary>The resource key of the error of the link field, or null.</summary>
    internal string? LinkErrorKey { get; private set; }

    /// <summary>The category of special characters shown while nothing is searched.</summary>
    internal string CharacterCategory { get; private set; } = HtmlEditorCharacters.DefaultCategory;

    /// <summary>What is searched among the special characters.</summary>
    internal string CharacterSearch { get; set; } = string.Empty;

    /// <summary>The rows of the table file read, or null.</summary>
    internal IReadOnlyList<IReadOnlyList<string>>? TableRows { get; private set; }

    /// <summary>Whether the first row of the table is inserted as its header.</summary>
    internal bool TableHeader { get; set; }

    /// <summary>The resource key of the error of the table file, or null.</summary>
    internal string? TableErrorKey { get; private set; }

    /// <summary>Whether a table file is being read.</summary>
    internal bool TableBusy { get; private set; }

    /// <summary>Shows a panel in place of the link field, emptied.</summary>
    internal void Open(HtmlEditorPanel panel)
    {
        LinkOpen = false;
        Current = panel;
        CharacterSearch = string.Empty;
        TableRows = null;
        TableErrorKey = null;
    }

    /// <summary>Hides the panel.</summary>
    internal void Close() => Current = HtmlEditorPanel.None;

    /// <summary>Shows the special characters of a category, the search emptied.</summary>
    internal void ChooseCategory(string category)
    {
        CharacterCategory = category;
        CharacterSearch = string.Empty;
    }

    /// <summary>Shows the link field, empty and about to take the focus, in place of the panel.</summary>
    internal void OpenLink()
    {
        Current = HtmlEditorPanel.None;
        LinkOpen = true;
        FocusLink = true;
        LinkErrorKey = null;
        LinkUrl = string.Empty;
    }

    /// <summary>Hides the link field and its error.</summary>
    internal void CloseLink()
    {
        LinkOpen = false;
        LinkErrorKey = null;
    }

    /// <summary>Hides the link field, keeping its error.</summary>
    internal void HideLink() => LinkOpen = false;

    /// <summary>
    /// The typed address once checked by <see cref="OmniUriPolicy"/>, or null with the link error set
    /// when it is refused.
    /// </summary>
    internal string? SafeLinkUrl(string url)
    {
        try
        {
            return OmniUriPolicy.EnsureSafe(url, nameof(OmniHtmlEditorAction.Link))!;
        }
        catch (InvalidOperationException)
        {
            LinkErrorKey = "HtmlEditorLinkInvalid";
            return null;
        }
    }

    /// <summary>
    /// The code points shown: those of the category, or, while something is searched, every character
    /// whose text or localized name holds it.
    /// </summary>
    internal IEnumerable<int> VisibleCharacters(Func<string, string> localize)
    {
        var search = CharacterSearch.Trim();
        if (search.Length == 0)
        {
            return HtmlEditorCharacters.All.Where(entry => entry.Category == CharacterCategory).Select(entry => entry.CodePoint);
        }

        return HtmlEditorCharacters.All.Select(entry => entry.CodePoint).Distinct().Where(codePoint =>
            HtmlEditorCharacters.Text(codePoint).Contains(search, StringComparison.Ordinal)
            || localize(HtmlEditorCharacters.NameKey(codePoint)).Contains(search, StringComparison.CurrentCultureIgnoreCase));
    }

    /// <summary>The file types the table import accepts: its own and those of the extensions' readers.</summary>
    internal static string TableAccept(IReadOnlyList<OmniHtmlEditorTableReader> readers) => string.Join(',', HtmlEditorTableFile.OwnExtensions
        .Concat(readers.SelectMany(reader => reader.Extensions))
        .Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Reads the first file chosen as a table: CSV and TSV here, other types through the first reader
    /// that takes them. The rows are kept for the preview, or an error when the file cannot be read.
    /// </summary>
    internal async Task ReadTableFileAsync(IReadOnlyList<IBrowserFile> files, IReadOnlyList<OmniHtmlEditorTableReader> readers)
    {
        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        TableRows = null;
        TableErrorKey = null;
        TableBusy = true;
        try
        {
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            await using var upload = file.OpenReadStream(HtmlEditorTableFile.MaxBytes);
            using var content = new MemoryStream();
            await upload.CopyToAsync(content);
            content.Position = 0;
            IReadOnlyList<IReadOnlyList<string>> rows;
            if (extension is ".csv" or ".tsv")
            {
                using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                rows = HtmlEditorTableFile.ReadDelimited(await reader.ReadToEndAsync(), extension == ".tsv" ? '\t' : ',');
            }
            else if (readers.FirstOrDefault(candidate => candidate.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) is { } tableReader)
            {
                rows = HtmlEditorTableFile.Shape(await tableReader.Read(file.Name, content));
            }
            else
            {
                TableErrorKey = "HtmlEditorImportTableUnsupported";
                return;
            }

            if (rows.Count == 0)
            {
                TableErrorKey = "HtmlEditorImportTableEmpty";
                return;
            }

            TableRows = rows;
            // A first row with no number in it reads as a header, as a spreadsheet's usually does.
            TableHeader = rows.Count > 1 && rows[0].All(cell => !double.TryParse(cell, NumberStyles.Any, CultureInfo.InvariantCulture, out _));
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or HttpRequestException or FormatException or NotSupportedException)
        {
            TableErrorKey = "HtmlEditorImportTableFailed";
        }
        finally
        {
            TableBusy = false;
        }
    }

    /// <summary>The table read, as HTML, closing the panel; null when no table was read.</summary>
    internal string? TakeTable()
    {
        if (TableRows is null)
        {
            return null;
        }

        var html = HtmlEditorTableFile.ToHtml(TableRows, TableHeader);
        Current = HtmlEditorPanel.None;
        TableRows = null;
        return html;
    }
}
