namespace OmniEurope.Blazor.Components;

/// <summary>A validator that requires a value: not null, and for a text, not blank.</summary>
/// <typeparam name="TValue">The type of the validated field.</typeparam>
public partial class OmniRequiredValidator<TValue>
{
    /// <summary>The message of a missing value; the localized "This field is required" when null or blank.</summary>
    [Parameter]
    public string? Message { get; set; }

    private string EffectiveMessage => string.IsNullOrWhiteSpace(Message)
        ? Localize("RequiredValidatorMessage")
        : Message;

    /// <inheritdoc />
    protected override string? GetValidationError(TValue value) =>
        value is null || value is string text && string.IsNullOrWhiteSpace(text) ? EffectiveMessage : null;
}
