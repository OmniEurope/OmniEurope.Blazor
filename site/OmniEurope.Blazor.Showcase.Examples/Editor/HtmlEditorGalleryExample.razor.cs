using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>Four editors side by side: a short toolbar, the source face, a disabled editor and a read-only one.</summary>
public partial class HtmlEditorGalleryExample
{
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> MinimalCommands =
    [
        OmniHtmlEditorCommands.Bold,
        OmniHtmlEditorCommands.Italic,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo,
        OmniHtmlEditorCommands.Redo
    ];

    // The short editor and the source editor are bound to the same value: typing in one shows in the other.
    private string MinimalBody { get; set; } = "<p>A short note.</p>";

    private string DisabledBody { get; set; } = "<p>This text cannot be edited.</p>";

    private string ReadOnlyBody { get; set; } = "<p>This content can be selected and copied, but not changed.</p>";
}
