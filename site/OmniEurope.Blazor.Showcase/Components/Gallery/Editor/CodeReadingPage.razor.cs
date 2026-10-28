using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Editor;

/// <summary>The code reading page: a command to copy, a numbered viewer, a unified diff and a side by side comparison.</summary>
public partial class CodeReadingPage
{
    [Inject]
    private IStringLocalizer<EditorStrings> Text { get; set; } = default!;

    private bool Changed { get; set; } = true;

    private bool Excerpt { get; set; }

    private bool DiffExpanded { get; set; } = true;
}
