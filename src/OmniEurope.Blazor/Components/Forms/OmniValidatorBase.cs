using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The base of a validator placed inside an <see cref="EditForm"/> that checks one field: it validates the
/// field when the form is submitted and when the field changes (after <see cref="ValidationDelay"/>),
/// writes the message in the edit context, and a derived class only says what the error is in
/// <see cref="GetValidationError"/>.
/// </summary>
/// <typeparam name="TValue">The type of the validated field.</typeparam>
public abstract class OmniValidatorBase<TValue> : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messages;
    private FieldIdentifier _field;
    private Func<TValue>? _accessor;
    private CancellationTokenSource? _validationDelay;
    private EditContext? _subscribedEditContext;

    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    /// <summary>The edit context of the enclosing <see cref="EditForm"/>; null outside one, which throws.</summary>
    [CascadingParameter]
    protected EditContext? CurrentEditContext { get; set; }

    /// <summary>The field to validate, as an expression such as <c>() =&gt; Model.Name</c>. Required.</summary>
    [Parameter, EditorRequired]
    public Expression<Func<TValue>> For { get; set; } = default!;

    /// <summary>
    /// Delay between the last change of the field and its validation. <see cref="TimeSpan.Zero"/>, the
    /// default (and any negative value), validates on every change; a new change during the delay
    /// restarts it.
    /// </summary>
    [Parameter]
    public TimeSpan ValidationDelay { get; set; }

    /// <summary>
    /// Whether the validator writes its own message where it is placed, in a polite live region. On by
    /// default; off, the message still reaches the edit context (a validation summary, for instance).
    /// </summary>
    [Parameter]
    public bool ShowMessage { get; set; } = true;

    /// <summary>
    /// Raised with the exception thrown while validating the field after a change. Without a handler,
    /// that exception reaches the renderer (the nearest error boundary) like one thrown by a lifecycle
    /// method. An exception thrown while validating on submit is not caught and reaches the form instead.
    /// </summary>
    [Parameter]
    public EventCallback<Exception> ValidationFailed { get; set; }

    /// <summary>The field identified by <see cref="For"/>.</summary>
    protected FieldIdentifier Field => _field;

    /// <summary>The message of the last validation; null or blank when the field passed.</summary>
    protected string? CurrentError { get; private set; }

    /// <summary>Returns the text of the library resource <paramref name="name"/> in the current UI culture.</summary>
    /// <param name="name">The resource key.</param>
    /// <param name="arguments">Values for the placeholders of the resource, if any.</param>
    /// <returns>The localized text, or <paramref name="name"/> itself when the key does not exist.</returns>
    protected string Localize(string name, params object[] arguments) =>
        PluralMessage.Localize(StringLocalizer, name, arguments);

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> outside an <see cref="EditForm"/>; otherwise follows
    /// the field of <see cref="For"/> in the form's edit context, moving to a new context or field when either changes.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside an EditForm.");
        }

        var field = FieldIdentifier.Create(For);
        _accessor = For.Compile();
        if (ReferenceEquals(_subscribedEditContext, CurrentEditContext) && _field.Equals(field))
        {
            return;
        }

        Detach();
        _field = field;
        _subscribedEditContext = CurrentEditContext;
        _messages = new ValidationMessageStore(_subscribedEditContext);
        _subscribedEditContext.OnValidationRequested += HandleValidationRequested;
        _subscribedEditContext.OnFieldChanged += HandleFieldChanged;
        _subscribedEditContext.OnValidationStateChanged += HandleValidationStateChanged;
    }

    /// <summary>Checks the current value of the field.</summary>
    /// <param name="value">The value read through <see cref="For"/>.</param>
    /// <returns>The message to show, or null or blank when the value is valid.</returns>
    protected abstract string? GetValidationError(TValue value);

    /// <summary>
    /// Validates the field now: replaces its message in the edit context with the result of
    /// <see cref="GetValidationError"/> and notifies the edit context. Only valid once the component has
    /// received its parameters.
    /// </summary>
    protected void Validate()
    {
        var editContext = _subscribedEditContext!;
        _messages!.Clear(_field);
        CurrentError = GetValidationError(_accessor!());

        if (!string.IsNullOrWhiteSpace(CurrentError))
        {
            _messages.Add(_field, CurrentError);
        }

        editContext.NotifyValidationStateChanged();
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args) => Validate();

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs args) =>
        _ = InvokeAsync(StateHasChanged);

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        if (!args.FieldIdentifier.Equals(_field))
        {
            return;
        }

        _validationDelay?.Cancel();
        _validationDelay?.Dispose();
        _validationDelay = new CancellationTokenSource();
        _ = ValidateFieldAsync(_validationDelay);
    }

    private async Task ValidateFieldAsync(CancellationTokenSource delay)
    {
        try
        {
            if (ValidationDelay > TimeSpan.Zero)
            {
                await Task.Delay(ValidationDelay, delay.Token);
            }
            await InvokeAsync(Validate);
        }
        catch (OperationCanceledException) when (delay.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (ValidationFailed.HasDelegate)
        {
            await ValidationFailed.InvokeAsync(exception);
        }
        catch (Exception exception)
        {
            // Nobody handles it: the failure reaches the renderer (an error boundary, or the host's
            // error handling) as it would from a lifecycle method, instead of vanishing.
            await InvokeAsync(() => DispatchExceptionAsync(exception));
        }
    }

    /// <summary>Cancels a pending delayed validation, clears the field's message and leaves the edit context.</summary>
    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }

    private void Detach()
    {
        _validationDelay?.Cancel();
        _validationDelay?.Dispose();
        _validationDelay = null;
        if (_subscribedEditContext is not null)
        {
            // Subscribed and given a message store together, in OnParametersSet.
            _messages!.Clear(_field);
            _subscribedEditContext.OnValidationRequested -= HandleValidationRequested;
            _subscribedEditContext.OnFieldChanged -= HandleFieldChanged;
            _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        }

        _messages = null;
        _subscribedEditContext = null;
    }
}
