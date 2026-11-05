using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>An editor with the special characters command, inside a dialog.</summary>
public partial class HtmlEditorDialogExample
{
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> DialogCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.InsertSpecialCharacter
    ];

    private bool Open { get; set; }

    private string Body { get; set; } = "<p>Some text.</p>";
}
