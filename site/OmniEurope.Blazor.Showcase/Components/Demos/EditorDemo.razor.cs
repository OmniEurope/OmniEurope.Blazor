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
    /// Extra buttons the host adds to the toolbar. A tool is a pure transform over the markup, so
    /// the editor never has to know what the host wanted to insert.
    /// </summary>
    private static readonly IReadOnlyList<OmniHtmlEditorTool> Tools =
    [
        new("signature", "Ajouter la signature", html => html + "<p>Le service des dossiers</p>"),
        new("reference", "Insérer la référence", html => html + "<p>Référence : D-2401</p>")
    ];

    /// <summary>
    /// The default toolbar with one command of the host at the end: a command receives a context
    /// through which it inserts sanitised HTML at the caret, in either face of the editor.
    /// </summary>
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> Commands =
    [
        .. OmniHtmlEditorCommands.Default,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommand.Create("signature-block", "Insérer le bloc de signature", context => context.InsertHtmlAsync("<p><strong>Le service des dossiers</strong></p>"), OmniIconName.Edit)
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

    private string Annotated { get; set; } =
        "<p>Article 1. Le présent règlement s'applique aux dossiers reçus.</p>" +
        "<aside class=\"demo-note\" data-marker=\"1\" contenteditable=\"false\">Note : texte consolidé au 1er janvier.</aside>";

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
    /// Refuses what the demonstration will not keep, before any transfer starts. Returning a message
    /// rejects the file, returning null accepts it.
    /// </summary>
    private static Task<string?> ValidateAsync(OmniUploadRequest request) =>
        Task.FromResult<string?>(request.Files.Any(file => file.Size == 0) ? "Un fichier est vide." : null);

    /// <summary>
    /// Stands in for a transfer: nothing leaves the browser, the demonstration only records the name.
    /// </summary>
    private Task UploadAsync(OmniUploadRequest request)
    {
        Uploaded.AddRange(request.Files.Select(file => file.Name));
        return Task.CompletedTask;
    }
}
