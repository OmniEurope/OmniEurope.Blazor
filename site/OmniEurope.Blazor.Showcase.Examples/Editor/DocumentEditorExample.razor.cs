using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A light word processor: an HTML editor on a sheet, with a status bar, and the HTML and text it exports.</summary>
public partial class DocumentEditorExample
{
    private const string SampleContent =
        "<h1>Summary note</h1>" +
        "<p>This note sums up the trips planned for the quarter and the points to settle.</p>" +
        "<h2>Open points</h2>" +
        "<ul><li>Pick the dates of the stay in Lisbon.</li><li>Compare the direct flights.</li></ul>" +
        "<p class=\"omni-align-right\"><em>The travel desk</em></p>";

    private OmniHtmlEditor? _document;

    /// <summary>The name given to the downloaded files, without the extension.</summary>
    [Parameter]
    public string FileName { get; set; } = "summary-note";

    /// <summary>Shows the bar under the page: the words and characters counted while typing, and the downloads.</summary>
    [Parameter]
    public bool ShowStatusBar { get; set; } = true;

    /// <summary>Locks the page and the toolbar; the exports stay available.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    private string Content { get; set; } = SampleContent;

    private string Exported { get; set; } = string.Empty;

    private string ExportDescription { get; set; } = "Choose Show the HTML or Show the text.";

    private async Task ShowHtmlAsync()
    {
        if (_document is not null)
        {
            Exported = await _document.ExportHtmlAsync();
            ExportDescription = "The complete HTML file, alignments and sizes written on the elements.";
        }
    }

    private async Task ShowTextAsync()
    {
        if (_document is not null)
        {
            Exported = await _document.ExportTextAsync();
            ExportDescription = "The plain text: blocks separated by a blank line, bullets and cells kept.";
        }
    }

    private void ResetContent() => Content = SampleContent;
}
