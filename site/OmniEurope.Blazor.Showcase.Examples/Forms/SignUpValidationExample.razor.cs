using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A sign-up form validated by validators placed next to its fields.</summary>
public partial class SignUpValidationExample : IDisposable
{
    /// <summary>The time of a simulated submission: long enough to see the busy button before the verdict.</summary>
    private static readonly TimeSpan SendDuration = TimeSpan.FromMilliseconds(800);

    private static readonly TimeSpan NotificationDuration = TimeSpan.FromSeconds(6);

    private readonly CancellationTokenSource _disposal = new();

    /// <summary>Where the messages appear.</summary>
    [Parameter]
    public ValidationMessagePlacement Placement { get; set; } = ValidationMessagePlacement.Validator;

    /// <summary>How long a validator waits after the last keystroke before it checks.</summary>
    [Parameter]
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(300);

    /// <summary>Whether a refused submission moves the focus to the first invalid field.</summary>
    [Parameter]
    public bool FocusFirstInvalid { get; set; } = true;

    /// <summary>How a valid submission is answered.</summary>
    [Parameter]
    public ValidationSuccessFeedback Feedback { get; set; } = ValidationSuccessFeedback.Notification;

    [Inject]
    private OmniOverlayService Overlay { get; set; } = null!;

    private SignUpModel Model { get; set; } = new();

    /// <summary>
    /// Kept here rather than created by the form: the messages carried by the field or by the summary are
    /// read in this component, which must therefore render again on every validation change.
    /// </summary>
    private EditContext FormContext { get; set; } = null!;

    private bool ShowValidatorMessages => Placement == ValidationMessagePlacement.Validator;

    private bool Sending { get; set; }

    private SubmitOutcome Outcome { get; set; }

    private string AcceptedName { get; set; } = string.Empty;

    private int RefusedErrorCount { get; set; }

    private int AcceptedCount { get; set; }

    private int RefusedCount { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() => Attach(new SignUpModel());

    private void Attach(SignUpModel model)
    {
        if (FormContext is not null)
        {
            FormContext.OnValidationStateChanged -= OnValidationStateChanged;
        }

        Model = model;
        FormContext = new EditContext(model);
        FormContext.OnValidationStateChanged += OnValidationStateChanged;
    }

    /// <summary>The first message of the field, when it is the form field that shows it.</summary>
    private string? FieldError(string fieldName) => Placement == ValidationMessagePlacement.Field
        ? FormContext.GetValidationMessages(new FieldIdentifier(Model, fieldName)).FirstOrDefault()
        : null;

    /// <summary>The control links to the help of its field, and to its error when the field shows it.</summary>
    private string DescribedBy(string fieldId) => Placement == ValidationMessagePlacement.Field
        ? $"{fieldId}-description {fieldId}-error"
        : $"{fieldId}-description";

    private async Task HandleValidSubmitAsync()
    {
        Sending = true;
        try
        {
            await Task.Delay(SendDuration, _disposal.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Sending = false;
        AcceptedCount++;
        AcceptedName = Model.Name.Trim();
        Outcome = SubmitOutcome.Accepted;
        if (Feedback == ValidationSuccessFeedback.Notification)
        {
            Overlay.Notify(
                $"Welcome, {AcceptedName}. The account was created (simulation, nothing was sent).",
                OmniSeverity.Success,
                "Account created",
                NotificationDuration);
        }
    }

    private void HandleInvalidSubmit()
    {
        RefusedCount++;
        RefusedErrorCount = FormContext.GetValidationMessages().Count();
        Outcome = SubmitOutcome.Refused;
    }

    private void ResetForm()
    {
        Attach(new SignUpModel());
        Outcome = SubmitOutcome.None;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs args) => StateHasChanged();

    /// <inheritdoc />
    public void Dispose()
    {
        FormContext.OnValidationStateChanged -= OnValidationStateChanged;
        _disposal.Cancel();
        _disposal.Dispose();
    }

    private enum SubmitOutcome
    {
        None,
        Accepted,
        Refused
    }
}
