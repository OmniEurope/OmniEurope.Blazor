using System.Globalization;
using System.Net;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class EditorDemo
{
    private const string ViewerCode = "{\n  \"mode\": \"lecture\",\n  \"actif\": true\n}";
    private static readonly IReadOnlyList<int> ViewerHighlights = [2];
    private const string ExcerptCode = "var total = items.Count;\nvar average = total / count;\nreturn average;";
    private static readonly IReadOnlyList<int> ExcerptHighlights = [42];
    private const string ExampleDiff = "--- a/config.json\n+++ b/config.json\n@@ -1 +1 @@\n-ancien\n+nouveau\n";

    private const string OriginalConfiguration = "{\n  \"mode\": \"ancien\"\n}";
    private const string ChangedConfiguration = "{\n  \"mode\": \"nouveau\"\n}";

    private static readonly IReadOnlyList<string> Accepted = ["image/png", "image/jpeg", "application/pdf"];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    /// <summary>
    /// The default toolbar with commands of the host at the end: a command receives a context through
    /// which it inserts sanitised HTML at the caret, or rewrites the whole value, in either face of the
    /// editor. A command without an icon shows its label as text.
    /// </summary>
    private IReadOnlyList<OmniHtmlEditorCommand> Commands { get; set; } = [];

    /// <summary>
    /// The host's own markup kept by the editor: a note carried by an aside, its marker and
    /// identifier in data attributes, and a class the host styles. Scripts, handlers and styles
    /// stay out whatever the policy says.
    /// </summary>
    private static readonly OmniHtmlSanitizerPolicy AnnotationPolicy = new()
    {
        AdditionalTags = ["aside"],
        AdditionalAttributes = ["contenteditable"],
        AdditionalCssClasses = ["demo-note"],
        AllowDataAttributes = true
    };

    /// <summary>The policy handed to the editor: an extension that only widens the allow-list.</summary>
    private static readonly AnnotationPolicyExtension AnnotationExtension = new();

    /// <summary>
    /// Inline formatting, the clipboard, a paragraph break and the table commands, which act on
    /// the cell at the caret and stay disabled elsewhere.
    /// </summary>
    private IReadOnlyList<OmniHtmlEditorCommand> AnnotatedCommands { get; set; } = [];

    private string Annotated { get; set; } = string.Empty;

    /// <summary>Where the caret is; <see langword="null"/> while it is outside the text.</summary>
    private string? SelectionPath { get; set; }

    /// <summary>The built-in commands that applications used to write themselves, then the extension's.</summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> ExtendedCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic, OmniHtmlEditorCommands.ChangeCase, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.InsertSpecialCharacter, OmniHtmlEditorCommands.ImportTable, OmniHtmlEditorCommands.ShowBlocks, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
    ];

    private DemoNoteExtension NoteExtension { get; set; } = default!;

    private string Extended { get; set; } = string.Empty;

    private string Report { get; set; } = string.Empty;

    private string Settings { get; set; } = "{\n  \"langue\": \"fr\"\n}";

    private string Body { get; set; } = string.Empty;

    private List<string> Uploaded { get; } = [];

    /// <summary>
    /// The attachments the letter already has, listed under the drop zone with a remove button; a
    /// deposited file joins them once the transfer succeeds.
    /// </summary>
    private IReadOnlyList<OmniUploadFile> Attachments { get; set; } =
    [
        new("accuse-de-reception.pdf", 184_320, "application/pdf"),
        new("plan-acces.png", 96_256, "image/png"),
        new("releve-de-compteur.pdf", 212_992, "application/pdf"),
        new("photo-facade.png", 348_160, "image/png")
    ];

    private IReadOnlyList<OmniUploadFile> Manifest { get; set; } = [];

    /// <summary>Builds the commands and the sample documents in the reader's language.</summary>
    protected override void OnInitialized()
    {
        var signature = Encoded("DemoEditorSignatureText");
        var reference = Escape(Text["DemoEditorReferenceText", "D-2401"]);
        Commands =
        [
            .. OmniHtmlEditorCommands.Default,
            OmniHtmlEditorCommands.Separator,
            OmniHtmlEditorCommand.Create("signature-block", Text["DemoEditorInsertSignatureBlock"], context => context.InsertHtmlAsync($"<p><strong>{signature}</strong></p>"), OmniIconName.Edit),
            OmniHtmlEditorCommand.Create("signature", Text["DemoEditorAddSignature"], context => context.SetHtmlAsync(context.Html + $"<p>{signature}</p>")),
            OmniHtmlEditorCommand.Create("reference", Text["DemoEditorInsertReference"], context => context.SetHtmlAsync(context.Html + $"<p>{reference}</p>"))
        ];

        var newNote = Encoded("DemoEditorNewNoteSentence");
        AnnotatedCommands =
        [
            OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic,
            OmniHtmlEditorCommand.Create(
                "demo-note",
                Text["DemoEditorAddNote"],
                context => context.InsertHtmlAsync($"<aside class=\"demo-note\" data-marker=\"2\">{newNote}</aside>"),
                OmniIconName.Chat) with { Pressed = selection => selection?.ClosestWithClass("demo-note") is not null, Description = Text["DemoEditorAddNoteDescription"] },
            OmniHtmlEditorCommands.Separator,
            .. OmniHtmlEditorCommands.Clipboard, OmniHtmlEditorCommands.InsertParagraph, OmniHtmlEditorCommands.Separator,
            .. OmniHtmlEditorCommands.Table, OmniHtmlEditorCommands.Separator,
            OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
        ];

        Annotated =
            $"<p>{Encoded("DemoEditorArticleText")}</p>" +
            $"<table><tbody><tr><th>{Encoded("DemoEditorArticleDelay")}</th><th>{Encoded("DemoEditorArticleDocument")}</th></tr>" +
            $"<tr><td>{Escape(Text["DemoEditorArticleDays", 30])}</td><td>{Encoded("DemoEditorArticleForm")}</td></tr></tbody></table>" +
            $"<aside class=\"demo-note\" data-marker=\"1\" contenteditable=\"false\">{Encoded("DemoEditorArticleNote")}</aside>";

        NoteExtension = new DemoNoteExtension(Text);
        Extended = "<p>" + string.Format(
            CultureInfo.CurrentCulture,
            Encoded("DemoEditorOpinion"),
            $"<span class=\"demo-note\" data-state=\"new\" contenteditable=\"false\">{Encoded("DemoEditorOpinionNote")}</span>") + "</p>";

        Report =
            $"<h1>{Encoded("DemoEditorReportLabel")}</h1>" +
            "<p>" + string.Format(CultureInfo.CurrentCulture, Encoded("DemoEditorReportParagraph"), $"<strong>{Encoded("DemoEditorReportImportant")}</strong>") + "</p>" +
            $"<ul><li>{Encoded("DemoEditorReportFirstPoint")}</li><li>{Encoded("DemoEditorReportSecondPoint")}</li></ul>";

        Body = $"<p>{Encoded("DemoEditorBodyGreeting")}</p><p>{Encoded("DemoEditorBodyText")}</p>";
    }

    /// <summary>A text of the resources, encoded to sit inside the sample markup.</summary>
    private string Encoded(string key) => Escape(Text[key]);

    /// <summary>Escapes the characters that would open markup; accented letters stay as they are.</summary>
    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>An extension whose only member is a policy: the markup of the annotated article.</summary>
    private sealed class AnnotationPolicyExtension : OmniHtmlEditorExtension
    {
        public override OmniHtmlSanitizerPolicy SanitizerPolicy => AnnotationPolicy;
    }

    /// <summary>
    /// A note extension: a command and its shortcut insert a note, a click on a note marks it as read,
    /// and the context menu removes the note at the caret. It never touches the surface itself.
    /// </summary>
    private sealed class DemoNoteExtension : OmniHtmlEditorExtension
    {
        private readonly OmniHtmlEditorCommand _addNote;

        private readonly OmniHtmlEditorCommand _removeNote;

        private readonly string _suggestTrigger;

        private readonly string _suggestCompletion;

        public DemoNoteExtension(IStringLocalizer<ShowcaseStrings> text)
        {
            var newNote = Escape(text["DemoEditorNewNote"]);
            _addNote = OmniHtmlEditorCommand.Create(
                "demo-add-note", text["DemoEditorAddNote"],
                context => context.InsertHtmlAsync($"<span class=\"demo-note\" data-state=\"new\" contenteditable=\"false\">{newNote}</span>"),
                OmniIconName.Chat);
            _removeNote = OmniHtmlEditorCommand.Create(
                "demo-remove-note", text["DemoEditorRemoveNote"],
                context => context.ReplaceClosestAsync(".demo-note", string.Empty),
                OmniIconName.Delete) with { Enabled = selection => selection?.ClosestWithClass("demo-note") is not null };
            _suggestTrigger = text["DemoEditorSuggestTrigger"];
            _suggestCompletion = text["DemoEditorSuggestCompletion"];
        }

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

        public override bool SuggestsText => true;

        /// <summary>Proposes the end of a stock phrase, as a writing assistant would; Tab types it.</summary>
        public override Task<string?> SuggestAsync(string textBeforeCaret) =>
            Task.FromResult<string?>(textBeforeCaret.EndsWith(_suggestTrigger, StringComparison.OrdinalIgnoreCase) ? _suggestCompletion : null);
    }

    /// <summary>
    /// Shows where the caret is, outermost element first, as the host of a structured editor
    /// would to enable the commands that fit there.
    /// </summary>
    private void ShowSelection(OmniHtmlEditorSelection selection) =>
        SelectionPath = selection.Ancestors.Count == 0
            ? Text["DemoEditorRoot"]
            : string.Join(" › ", selection.Ancestors.Reverse().Select(node =>
                node.TagName + string.Concat(node.CssClasses.Select(name => "." + name)) +
                string.Concat(node.DataAttributes.Select(entry => $"[{entry.Key}={entry.Value}]"))));

    /// <summary>
    /// Stands in for a transfer: nothing leaves the browser, the demonstration only records the name.
    /// It first refuses what it will not keep: a rejected selection is neither stored nor listed.
    /// </summary>
    private Task UploadAsync(OmniUploadRequest request)
    {
        if (request.Files.Any(file => file.Size == 0))
        {
            request.Reject(Text["DemoEditorEmptyFile"]);
            return Task.CompletedTask;
        }

        Uploaded.AddRange(request.Files.Select(file => file.Name));
        return Task.CompletedTask;
    }
}
