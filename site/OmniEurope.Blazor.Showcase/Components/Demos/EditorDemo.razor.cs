namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class EditorDemo
{
    private const string ViewerCode = "{\n  \"mode\": \"lecture\",\n  \"actif\": true\n}";
    private static readonly IReadOnlyCollection<int> ViewerHighlights = [2];
    private const string ExampleDiff = "--- a/config.json\n+++ b/config.json\n@@ -1 +1 @@\n-ancien\n+nouveau\n";

    private const string OriginalConfiguration = "{\n  \"mode\": \"ancien\"\n}";
    private const string ChangedConfiguration = "{\n  \"mode\": \"nouveau\"\n}";

    private static readonly IReadOnlyList<string> Accepted = ["image/png", "image/jpeg", "application/pdf"];

    /// <summary>
    /// The default toolbar with commands of the host at the end: a command receives a context through
    /// which it inserts sanitised HTML at the caret, or rewrites the whole value, in either face of the
    /// editor. A command without an icon shows its label as text.
    /// </summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> Commands =
    [
        .. OmniHtmlEditorCommands.Default,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommand.Create("signature-block", "Insérer le bloc de signature", context => context.InsertHtmlAsync("<p><strong>Le service des dossiers</strong></p>"), OmniIconName.Edit),
        OmniHtmlEditorCommand.Create("signature", "Ajouter la signature", context => context.SetHtmlAsync(context.Html + "<p>Le service des dossiers</p>")),
        OmniHtmlEditorCommand.Create("reference", "Insérer la référence", context => context.SetHtmlAsync(context.Html + "<p>Référence : D-2401</p>"))
    ];

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
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> AnnotatedCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic,
        OmniHtmlEditorCommand.Create(
            "demo-note",
            "Ajouter une note",
            context => context.InsertHtmlAsync("<aside class=\"demo-note\" data-marker=\"2\">Nouvelle note.</aside>"),
            OmniIconName.Chat) with { Pressed = selection => selection?.ClosestWithClass("demo-note") is not null },
        OmniHtmlEditorCommands.Separator,
        .. OmniHtmlEditorCommands.Clipboard, OmniHtmlEditorCommands.InsertParagraph, OmniHtmlEditorCommands.Separator,
        .. OmniHtmlEditorCommands.Table, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
    ];

    private string Annotated { get; set; } =
        "<p>Article 1. Le présent règlement s'applique aux dossiers reçus.</p>" +
        "<table><tbody><tr><th>Délai</th><th>Pièce</th></tr><tr><td>30 jours</td><td>Formulaire</td></tr></tbody></table>" +
        "<aside class=\"demo-note\" data-marker=\"1\" contenteditable=\"false\">Note : texte consolidé au 1er janvier.</aside>";

    private string SelectionPath { get; set; } = "hors du texte";

    /// <summary>The built-in commands that applications used to write themselves, then the extension's.</summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> ExtendedCommands =
    [
        OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.Italic, OmniHtmlEditorCommands.ChangeCase, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.InsertSpecialCharacter, OmniHtmlEditorCommands.ImportTable, OmniHtmlEditorCommands.ShowBlocks, OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo, OmniHtmlEditorCommands.Redo
    ];

    private DemoNoteExtension NoteExtension { get; } = new();

    private string Extended { get; set; } =
        "<p>Avis favorable sous réserve<span class=\"demo-note\" data-state=\"new\" contenteditable=\"false\">vérifier le délai</span>.</p>";

    /// <summary>
    /// A note extension: a command and its shortcut insert a note, a click on a note marks it as read,
    /// and the context menu removes the note at the caret. It never touches the surface itself.
    /// </summary>
    /// <summary>An extension whose only member is a policy: the markup of the annotated article.</summary>
    private sealed class AnnotationPolicyExtension : OmniHtmlEditorExtension
    {
        public override OmniHtmlSanitizerPolicy SanitizerPolicy => AnnotationPolicy;
    }

    private sealed class DemoNoteExtension : OmniHtmlEditorExtension
    {
        private static readonly OmniHtmlEditorCommand AddNote = OmniHtmlEditorCommand.Create(
            "demo-add-note", "Ajouter une note",
            context => context.InsertHtmlAsync("<span class=\"demo-note\" data-state=\"new\" contenteditable=\"false\">Nouvelle note</span>"),
            OmniIconName.Chat);

        private static readonly OmniHtmlEditorCommand RemoveNote = OmniHtmlEditorCommand.Create(
            "demo-remove-note", "Retirer la note",
            context => context.ReplaceClosestAsync(".demo-note", string.Empty),
            OmniIconName.Delete) with { Enabled = selection => selection?.ClosestWithClass("demo-note") is not null };

        public override IReadOnlyList<OmniHtmlEditorCommand> Commands => [AddNote];

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
                $"<span class=\"demo-note\" data-state=\"read\" contenteditable=\"false\">{System.Net.WebUtility.HtmlEncode(context.Text)}</span>"))
        ];

        public override IReadOnlyList<OmniHtmlEditorCommand> ContextMenu => [AddNote, RemoveNote];

        public override bool SuggestsText => true;

        /// <summary>Proposes the end of a stock phrase, as a writing assistant would; Tab types it.</summary>
        public override Task<string?> SuggestAsync(string textBeforeCaret) =>
            Task.FromResult<string?>(textBeforeCaret.EndsWith("sous réserve", StringComparison.OrdinalIgnoreCase) ? " de vérification" : null);
    }

    /// <summary>
    /// Shows where the caret is, outermost element first, as the host of a structured editor
    /// would to enable the commands that fit there.
    /// </summary>
    private void ShowSelection(OmniHtmlEditorSelection selection) =>
        SelectionPath = selection.Ancestors.Count == 0
            ? "racine"
            : string.Join(" › ", selection.Ancestors.Reverse().Select(node =>
                node.TagName + string.Concat(node.CssClasses.Select(name => "." + name)) +
                string.Concat(node.DataAttributes.Select(entry => $"[{entry.Key}={entry.Value}]"))));

    private string Report { get; set; } = "<h1>Rapport</h1><p>Un paragraphe <strong>important</strong>.</p><ul><li>Premier point</li><li>Second point</li></ul>";

    private string Settings { get; set; } = "{\n  \"langue\": \"fr\"\n}";

    private string Body { get; set; } = "<p>Madame, Monsieur,</p><p>Votre dossier a bien été reçu.</p>";

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

    /// <summary>
    /// Stands in for a transfer: nothing leaves the browser, the demonstration only records the name.
    /// It first refuses what it will not keep: a rejected selection is neither stored nor listed.
    /// </summary>
    private Task UploadAsync(OmniUploadRequest request)
    {
        if (request.Files.Any(file => file.Size == 0))
        {
            request.Reject("Un fichier est vide.");
            return Task.CompletedTask;
        }

        Uploaded.AddRange(request.Files.Select(file => file.Name));
        return Task.CompletedTask;
    }
}
