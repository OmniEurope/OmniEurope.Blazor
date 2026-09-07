namespace OmniEurope.Blazor.Components;

public partial class OmniPassword
{
    private bool _revealed;

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Autocomplete { get; set; } = "current-password";

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Revealable { get; set; } = true;

    [Parameter]
    public string RevealLabel { get; set; } = string.Empty;

    [Parameter]
    public string HideLabel { get; set; } = string.Empty;

    [Parameter]
    public string RevealText { get; set; } = string.Empty;

    [Parameter]
    public string HideText { get; set; } = string.Empty;

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string EffectiveRevealLabel => string.IsNullOrWhiteSpace(RevealLabel) ? Localize("PasswordRevealLabel") : RevealLabel;
    private string EffectiveHideLabel => string.IsNullOrWhiteSpace(HideLabel) ? Localize("PasswordHideLabel") : HideLabel;
    private string EffectiveRevealText => string.IsNullOrWhiteSpace(RevealText) ? Localize("PasswordRevealText") : RevealText;
    private string EffectiveHideText => string.IsNullOrWhiteSpace(HideText) ? Localize("PasswordHideText") : HideText;

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();
    private void ToggleReveal() => _revealed = !_revealed;

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
