using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public abstract class OmniValidatorBase<TValue> : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messages;
    private FieldIdentifier _field;
    private Func<TValue>? _accessor;
    private CancellationTokenSource? _validationDelay;
    private EditContext? _subscribedEditContext;

    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    [CascadingParameter]
    protected EditContext? CurrentEditContext { get; set; }

    [Parameter, EditorRequired]
    public Expression<Func<TValue>> For { get; set; } = default!;

    [Parameter]
    public int ValidationDelayMilliseconds { get; set; }

    [Parameter]
    public bool ShowMessage { get; set; } = true;

    [Parameter]
    public EventCallback<Exception> ValidationFailed { get; set; }

    protected FieldIdentifier Field => _field;
    protected string? CurrentError { get; private set; }

    protected string Localize(string name, params object[] arguments) => StringLocalizer[name, arguments].Value;

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

    protected abstract string? GetValidationError(TValue value);

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
            if (ValidationDelayMilliseconds > 0)
            {
                await Task.Delay(ValidationDelayMilliseconds, delay.Token);
            }
            await InvokeAsync(Validate);
        }
        catch (OperationCanceledException) when (delay.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            await ValidationFailed.InvokeAsync(exception);
        }
    }

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
            _messages?.Clear(_field);
            _subscribedEditContext.OnValidationRequested -= HandleValidationRequested;
            _subscribedEditContext.OnFieldChanged -= HandleFieldChanged;
            _subscribedEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        }

        _messages = null;
        _subscribedEditContext = null;
    }
}
