using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A vertical form with its errors, their summary and its disabled and read-only fields.</summary>
public partial class VerticalFormExample : IDisposable
{
    private VerticalFormModel Model { get; } = new();

    private EditContext Context { get; set; } = default!;

    private ValidationMessageStore Messages { get; set; } = default!;

    private string? Status { get; set; }

    /// <summary>The messages of the summary, in field order.</summary>
    private IReadOnlyList<string> Errors =>
    [
        .. new[] { nameof(VerticalFormModel.Name), nameof(VerticalFormModel.Email) }
            .SelectMany(name => Context.GetValidationMessages(Context.Field(name)))
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
    public void Dispose() => Context.OnFieldChanged -= OnFieldChanged;

    private string? ErrorOf(string field) => Context.GetValidationMessages(Context.Field(field)).FirstOrDefault();

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        Status = null;
        Validate();
    }

    /// <summary>
    /// The two rules of the form: a name, and an address with a full domain. Each failure is a message of the
    /// edit context, which the field reads for its error line and its aria-invalid.
    /// </summary>
    private void Validate()
    {
        Messages.Clear();
        if (string.IsNullOrWhiteSpace(Model.Name))
        {
            Messages.Add(Context.Field(nameof(VerticalFormModel.Name)), "The pipeline name is required.");
        }

        var at = Model.Email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || !Model.Email[(at + 1)..].Contains('.', StringComparison.Ordinal))
        {
            Messages.Add(Context.Field(nameof(VerticalFormModel.Email)), "The address must have a full domain, such as team@example.com.");
        }

        Context.NotifyValidationStateChanged();
    }

    private void Save() => Status = Errors.Count == 0 ? "Saved." : "Not saved: correct the errors first.";

    private void Reset()
    {
        Model.Email = "team@example";
        Status = null;
        Validate();
    }
}
