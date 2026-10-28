using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The word processor page: a sheet, a status bar that counts and exports, and the file name of the downloads.</summary>
public partial class DocumentEditorPage
{
    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private bool ShowStatusBar { get; set; } = true;

    private bool ReadOnly { get; set; }

    private string FileName { get; set; } = "summary-note";
}
