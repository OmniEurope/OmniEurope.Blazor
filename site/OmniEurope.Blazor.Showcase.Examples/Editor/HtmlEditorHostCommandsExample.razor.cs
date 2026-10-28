using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>An HTML editor whose toolbar ends with commands of the host, bound to the same value as an editor in source mode.</summary>
public partial class HtmlEditorHostCommandsExample
{
    /// <summary>
    /// The default toolbar with commands of the host at the end. A command without an icon shows its label as text.
    /// </summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> Commands =
    [
        .. OmniHtmlEditorCommands.Default,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommand.Create("signature-block", "Insert the signature block", context => context.InsertHtmlAsync("<p><strong>The case files department</strong></p>"), OmniIconName.Edit),
        OmniHtmlEditorCommand.Create("signature", "Add the signature", context => context.SetHtmlAsync(context.Html + "<p>The case files department</p>")),
        // At the caret: the content before and after it, a paragraph split in two, around the new one.
        OmniHtmlEditorCommand.Create("reference", "Insert the reference", async context =>
        {
            var around = await context.GetHtmlAroundCaretAsync();
            await context.SetHtmlAsync(around.Before + "<p>Reference: D-2401</p>" + around.After);
        })
    ];

    private string Body { get; set; } = "<p>Dear Sir or Madam,</p><p>Your case file has been received.</p>";
}
