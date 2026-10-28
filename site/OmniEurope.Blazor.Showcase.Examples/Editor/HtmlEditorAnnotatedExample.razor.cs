using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>An editor that keeps the markup of its host (notes in asides), reports the caret and offers the table commands.</summary>
public partial class HtmlEditorAnnotatedExample
{
    /// <summary>The mark a ticked note starts with.</summary>
    private const string ReadMark = "✓ ";

    /// <summary>
    /// The host's own markup kept by the editor: a note carried by an aside, its marker and identifier in data attributes,
    /// and a class the host styles. Scripts, handlers and styles stay out whatever the policy says.
    /// </summary>
    private static readonly OmniHtmlSanitizerPolicy AnnotationPolicy = new()
    {
        AdditionalTags = ["aside"],
        AdditionalAttributes = ["contenteditable"],
        AdditionalCssClasses = ["demo-note"],
        AllowDataAttributes = true
    };

    /// <summary>The policy handed to the editor, with the click on a note.</summary>
    private static readonly AnnotationPolicyExtension AnnotationExtension = new();

    /// <summary>Inline formatting, the host's note command, the clipboard, a paragraph break and the table commands.</summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> AnnotatedCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic,
        OmniHtmlEditorCommand.Create(
            "demo-note",
            "Add a note",
            // The selected words become the note; without a selection, the stock sentence.
            async context =>
            {
                var selected = (await context.GetSelectedTextAsync()).Trim();
                await context.InsertHtmlAsync($"<aside class=\"demo-note\" data-marker=\"2\">{(selected.Length == 0 ? "New note." : Escape(selected))}</aside>");
            },
            OmniIconName.Chat) with { Pressed = selection => selection?.ClosestWithClass("demo-note") is not null, Description = "Inserts a numbered note in the margin of the text." },
        OmniHtmlEditorCommands.Separator,
        .. OmniHtmlEditorCommands.Clipboard, OmniHtmlEditorCommands.InsertParagraph, OmniHtmlEditorCommands.Separator,
        .. OmniHtmlEditorCommands.Table, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
    ];

    private string Annotated { get; set; } =
        "<p>Article 1. These rules apply to the case files received.</p>" +
        "<table><tbody><tr><th>Deadline</th><th>Document</th></tr>" +
        "<tr><td>30 days</td><td>Form</td></tr></tbody></table>" +
        "<aside class=\"demo-note\" data-marker=\"1\" contenteditable=\"false\">Note: text consolidated as of 1 January.</aside>";

    /// <summary>Where the caret is; <see langword="null"/> while it is outside the text.</summary>
    private string? SelectionPath { get; set; }

    /// <summary>Escapes the characters that would open markup.</summary>
    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// Shows where the caret is, outermost element first, as the host of a structured editor
    /// would to enable the commands that fit there.
    /// </summary>
    private void ShowSelection(OmniHtmlEditorSelection selection) =>
        SelectionPath = selection.Ancestors.Count == 0
            ? "root"
            : string.Join(" › ", selection.Ancestors.Reverse().Select(node =>
                node.TagName + string.Concat(node.CssClasses.Select(name => "." + name)) +
                string.Concat(node.DataAttributes.Select(entry => $"[{entry.Key}={entry.Value}]"))));

    /// <summary>The markup of the annotated article as a policy, and a note ticked or unticked by a click.</summary>
    private sealed class AnnotationPolicyExtension : OmniHtmlEditorExtension
    {
        public override OmniHtmlSanitizerPolicy SanitizerPolicy => AnnotationPolicy;

        /// <summary>A click on a note ticks it, or unticks it: its text changes, the note and its attributes stay.</summary>
        public override IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements { get; } =
        [
            new(".demo-note", context => context.SetTextAsync(context.Text.StartsWith(ReadMark, StringComparison.Ordinal)
                ? context.Text[ReadMark.Length..]
                : ReadMark + context.Text))
        ];
    }
}
