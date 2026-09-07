namespace OmniEurope.Blazor.Components;

public partial class OmniRequiredValidator<TValue>
{
    [Parameter]
    public string Message { get; set; } = string.Empty;

    private string EffectiveMessage => string.IsNullOrWhiteSpace(Message)
        ? Localize("RequiredValidatorMessage")
        : Message;

    protected override string? GetValidationError(TValue value) =>
        value is null || value is string text && string.IsNullOrWhiteSpace(text) ? EffectiveMessage : null;
}
