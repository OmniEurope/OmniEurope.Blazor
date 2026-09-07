namespace OmniEurope.Blazor.Components;

public partial class OmniLengthValidator
{
    [Parameter]
    public int? MinimumLength { get; set; }

    [Parameter]
    public int? MaximumLength { get; set; }

    [Parameter]
    public string Message { get; set; } = string.Empty;

    private string EffectiveMessage => string.IsNullOrWhiteSpace(Message)
        ? Localize("LengthValidatorMessage")
        : Message;

    protected override void OnParametersSet()
    {
        if (MinimumLength is < 0 || MaximumLength is < 0 ||
            MinimumLength is not null && MaximumLength is not null && MinimumLength > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumLength), "Length bounds must be positive and ordered.");
        }

        base.OnParametersSet();
    }

    protected override string? GetValidationError(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var tooShort = MinimumLength is not null && value.Length < MinimumLength.Value;
        var tooLong = MaximumLength is not null && value.Length > MaximumLength.Value;
        return tooShort || tooLong ? EffectiveMessage : null;
    }
}
