using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Components;

public abstract class OmniValidatorBase<TValue> : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messages;
    private FieldIdentifier _field;
    private Func<TValue>? _accessor;
    private CancellationTokenSource? _validationDelay;

    [CascadingParameter]
    protected EditContext? CurrentEditContext { get; set; }

    [Parameter, EditorRequired]
    public Expression<Func<TValue>> For { get; set; } = default!;

    [Parameter]
    public int ValidationDelayMilliseconds { get; set; }

    [Parameter]
    public bool ShowMessage { get; set; } = true;

    protected FieldIdentifier Field => _field;
    protected string? CurrentError { get; private set; }

    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be placed inside an EditForm.");
        }

        _field = FieldIdentifier.Create(For);
        _accessor = For.Compile();
        _messages = new ValidationMessageStore(CurrentEditContext);
        CurrentEditContext.OnValidationRequested += HandleValidationRequested;
        CurrentEditContext.OnFieldChanged += HandleFieldChanged;
        CurrentEditContext.OnValidationStateChanged += HandleValidationStateChanged;
    }

    protected abstract string? GetValidationError(TValue value);

    protected void Validate()
    {
        var editContext = CurrentEditContext!;
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

    private async void HandleFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        if (!args.FieldIdentifier.Equals(_field))
        {
            return;
        }

        _validationDelay?.Cancel();
        _validationDelay?.Dispose();
        _validationDelay = new CancellationTokenSource();
        var delay = _validationDelay;
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
    }

    public void Dispose()
    {
        _validationDelay?.Cancel();
        _validationDelay?.Dispose();
        if (CurrentEditContext is not null)
        {
            CurrentEditContext.OnValidationRequested -= HandleValidationRequested;
            CurrentEditContext.OnFieldChanged -= HandleFieldChanged;
            CurrentEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        }

        GC.SuppressFinalize(this);
    }
}
