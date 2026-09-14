namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class EditorDemo
{
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

    private string Body { get; set; } = "<p>Madame, Monsieur,</p><p>Votre dossier a bien été reçu.</p>";

    private List<string> Uploaded { get; } = [];

    /// <summary>
    /// The attachments the letter already has, listed under the drop zone with a remove button; a
    /// deposited file joins them once the transfer succeeds.
    /// </summary>
    private IReadOnlyList<OmniUploadFile> Attachments { get; set; } =
    [
        new("accuse-de-reception.pdf", 184_320, "application/pdf"),
        new("plan-acces.png", 96_256, "image/png")
    ];

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
