using System.Net;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>Editors extended by a host extension (notes, proofreading, suggestions) and the same proofreader on a locked text.</summary>
public partial class HtmlEditorExtensionExample
{
    private const string Wrong = "speling";

    private const string Right = "spelling";

    private const string Repeated = "Repeated word";

    private const string ProofreadingSample = "The checker underlines a doubtful speling and a repeated repeated word.";

    /// <summary>The built-in commands that applications used to write themselves, then the extension's.</summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> ExtendedCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic, OmniHtmlEditorCommands.Highlight, OmniHtmlEditorCommands.ChangeCase, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.InsertSpecialCharacter, OmniHtmlEditorCommands.ImportTable, OmniHtmlEditorCommands.ShowBlocks, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
    ];

    /// <summary>A short toolbar for the locked editor.</summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> PaletteCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
    ];

    private NotesExtension NoteExtension { get; } = new();

    /// <summary>The proofreader alone, on the text that starts locked.</summary>
    private ProofreadingOnlyExtension ProofreadingExtension { get; } = new(new SampleProofreader(Wrong, Right, Repeated));

    private string Extended { get; set; } =
        "<p>Approved subject to <span class=\"demo-note\" data-state=\"new\" contenteditable=\"false\">check the deadline</span>.</p>" +
        $"<p>{ProofreadingSample}</p>";

    private string Locked { get; set; } = $"<p>{ProofreadingSample}</p>";

    private bool LockedReadOnly { get; set; } = true;

    /// <summary>An extension that brings a proofreader and nothing else.</summary>
    private sealed class ProofreadingOnlyExtension(OmniHtmlEditorProofreader proofreader) : OmniHtmlEditorExtension
    {
        public override OmniHtmlEditorProofreader? Proofreader { get; } = proofreader;
    }

    /// <summary>
    /// A note extension: a command and its shortcut insert a note, a click on a note marks it as read,
    /// and the context menu removes the note at the caret. It also underlines a misspelling and proposes the end of a
    /// stock phrase. It never touches the surface itself.
    /// </summary>
    private sealed class NotesExtension : OmniHtmlEditorExtension
    {
        private readonly OmniHtmlEditorCommand _addNote = OmniHtmlEditorCommand.Create(
            "demo-add-note",
            "Add a note",
            context => context.InsertHtmlAsync("<span class=\"demo-note\" data-state=\"new\" contenteditable=\"false\">New note</span>"),
            OmniIconName.Chat);

        private readonly OmniHtmlEditorCommand _removeNote = OmniHtmlEditorCommand.Create(
            "demo-remove-note",
            "Remove the note",
            context => context.ReplaceClosestAsync(".demo-note", string.Empty),
            OmniIconName.Delete) with { Enabled = selection => selection?.ClosestWithClass("demo-note") is not null };

        public override IReadOnlyList<OmniHtmlEditorCommand> Commands => [_addNote];

        public override OmniHtmlSanitizerPolicy SanitizerPolicy { get; } = new()
        {
            AdditionalAttributes = ["contenteditable"],
            AdditionalCssClasses = ["demo-note"],
            AllowDataAttributes = true
        };

        public override IReadOnlyList<OmniHtmlEditorShortcut> Shortcuts { get; } = [new("Ctrl+Shift+N", "demo-add-note")];

        public override IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements { get; } =
        [
            new(".demo-note", context => context.ReplaceAsync(
                $"<span class=\"demo-note\" data-state=\"read\" contenteditable=\"false\">{WebUtility.HtmlEncode(context.Text)}</span>"))
        ];

        public override IReadOnlyList<OmniHtmlEditorCommand> ContextMenu => [_addNote, _removeNote];

        /// <summary>Underlines a misspelled word and a repeated one; a right-click offers the correction.</summary>
        public override OmniHtmlEditorProofreader? Proofreader { get; } = new SampleProofreader(Wrong, Right, Repeated);

        public override bool SuggestsText => true;

        /// <summary>Proposes the end of a stock phrase, as a writing assistant would; Tab types it.</summary>
        public override Task<string?> SuggestAsync(string textBeforeCaret) =>
            Task.FromResult<string?>(textBeforeCaret.EndsWith("subject to", StringComparison.OrdinalIgnoreCase) ? " verification" : null);
    }
}
