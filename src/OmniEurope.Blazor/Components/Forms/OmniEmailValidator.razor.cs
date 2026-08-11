namespace OmniEurope.Blazor.Components;

public partial class OmniEmailValidator
{
    private static readonly System.ComponentModel.DataAnnotations.EmailAddressAttribute EmailRule = new();

    [Parameter]
    public string Message { get; set; } = string.Empty;

    private string EffectiveMessage => string.IsNullOrWhiteSpace(Message)
        ? Localize("EmailValidatorMessage")
        : Message;

    protected override string? GetValidationError(string value) =>
        string.IsNullOrWhiteSpace(value) || EmailRule.IsValid(value) ? null : EffectiveMessage;
}
