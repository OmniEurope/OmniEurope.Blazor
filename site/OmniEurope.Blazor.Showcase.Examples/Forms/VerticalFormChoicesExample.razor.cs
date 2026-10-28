using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A vertical form of selection, date, choice and file fields, with a strategy error held by the edit context.</summary>
public partial class VerticalFormChoicesExample : IDisposable
{
    private static readonly IReadOnlyList<OmniOption<string>> Environments =
    [
        new("production", "Production"),
        new("acceptance", "Acceptance"),
        new("development", "Development")
    ];

    private static readonly IReadOnlyList<OmniOption<string>> Strategies =
    [
        new("rolling", "Rolling update"),
        new("blue-green", "Blue-green"),
        new("canary", "Canary", Disabled: true)
    ];

    private static readonly IReadOnlyList<OmniOption<string>> Agents = [new("shared", "Shared agent")];

    private VerticalFormChoicesModel Model { get; } = new();

    private EditContext Context { get; set; } = default!;

    private ValidationMessageStore Messages { get; set; } = default!;

    private IReadOnlyList<OmniUploadFile> Manifest { get; set; } = [];

    private IReadOnlyList<OmniUploadFile> Archive { get; set; } = [];

    /// <summary>Four attachments already there, one more than the reduced list shows.</summary>
    private IReadOnlyList<OmniUploadFile> Attachments { get; set; } =
    [
        new("dashboard-screenshot.png", 348_160, "image/png"),
        new("deployment-log.txt", 12_288, "text/plain"),
        new("coverage-report.pdf", 212_992, "application/pdf"),
        new("network-diagram.png", 96_256, "image/png")
    ];

    private string? Status { get; set; }

    /// <summary>The messages of the summary.</summary>
    private IReadOnlyList<string> Errors =>
    [
        .. Context.GetValidationMessages(Context.Field(nameof(VerticalFormChoicesModel.Strategy)))
    ];

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Context = new EditContext(Model);
        Messages = new ValidationMessageStore(Context);
        Context.OnFieldChanged += OnFieldChanged;
        Validate();
    }

    /// <inheritdoc />
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
    /// choice list reads for its error line and its aria-invalid.
    /// </summary>
    private void Validate()
    {
        Messages.Clear();
        if (string.IsNullOrEmpty(Model.Strategy))
        {
            Messages.Add(Context.Field(nameof(VerticalFormChoicesModel.Strategy)), "Choose a deployment strategy.");
        }

        Context.NotifyValidationStateChanged();
    }

    private void Save() => Status = Errors.Count == 0 ? "Saved." : "Not saved: correct the errors first.";

    private void Reset()
    {
        Model.Strategy = null;
        Status = null;
        Validate();
    }
}
