using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class VerticalFormDemo : IDisposable
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private VerticalFormDemoModel Model { get; } = new();

    private EditContext Context { get; set; } = default!;

    private ValidationMessageStore Messages { get; set; } = default!;

    private IReadOnlyList<OmniOption<string>> Environments { get; set; } = [];

    private IReadOnlyList<OmniOption<string>> Strategies { get; set; } = [];

    private IReadOnlyList<OmniOption<string>> Agents { get; set; } = [];

    private IReadOnlyList<OmniUploadFile> Manifest { get; set; } = [];

    private IReadOnlyList<OmniUploadFile> Archive { get; set; } = [];

    /// <summary>Four attachments already there, one more than the reduced list shows.</summary>
    private IReadOnlyList<OmniUploadFile> Attachments { get; set; } =
    [
        new("capture-tableau-de-bord.png", 348_160, "image/png"),
        new("journal-deploiement.txt", 12_288, "text/plain"),
        new("rapport-couverture.pdf", 212_992, "application/pdf"),
        new("schema-reseau.png", 96_256, "image/png")
    ];

    private string? Status { get; set; }

    /// <summary>The messages of the summary.</summary>
    private IReadOnlyList<string> Errors =>
    [
        .. Context.GetValidationMessages(Context.Field(nameof(VerticalFormDemoModel.Strategy)))
    ];

    protected override void OnInitialized()
    {
        Environments =
        [
            new("production", Text["VFormEnvironmentProduction"]),
            new("recette", Text["VFormEnvironmentAcceptance"]),
            new("developpement", Text["VFormEnvironmentDevelopment"])
        ];
        Strategies =
        [
            new("rolling", Text["VFormStrategyRolling"]),
            new("blue-green", Text["VFormStrategyBlueGreen"]),
            new("canary", Text["VFormStrategyCanary"], Disabled: true)
        ];
        Agents = [new("shared", Text["VFormAgentShared"])];

        Context = new EditContext(Model);
        Messages = new ValidationMessageStore(Context);
        Context.OnFieldChanged += OnFieldChanged;
        Validate();
    }

    public void Dispose()
    {
        Context.OnFieldChanged -= OnFieldChanged;
        GC.SuppressFinalize(this);
    }

    private string? ErrorOf(string field) => Context.GetValidationMessages(Context.Field(field)).FirstOrDefault();

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        Status = null;
        Validate();
    }

    /// <summary>
    /// The one rule of the form: a strategy. The failure is a message of the edit context, which the
    /// choice list reads for its error line and aria-invalid.
    /// </summary>
    private void Validate()
    {
        Messages.Clear();
        if (string.IsNullOrEmpty(Model.Strategy))
        {
            Messages.Add(Context.Field(nameof(VerticalFormDemoModel.Strategy)), Text["VFormStrategyError"]);
        }

        Context.NotifyValidationStateChanged();
    }

    private void Save() => Status = Errors.Count == 0 ? Text["VFormSaved"] : Text["VFormNotSaved"];

    private void Reset()
    {
        Model.Strategy = null;
        Status = null;
        Validate();
    }

    /// <summary>Stands in for a transfer: nothing leaves the browser, the file is only listed.</summary>
    private static Task KeepAsync(OmniUploadRequest request) => Task.CompletedTask;
}
