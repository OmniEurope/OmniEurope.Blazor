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

    /// <summary>
    /// A word to show on the button instead of the eye while the password is hidden. Set it (with
    /// <see cref="HideText"/>) to keep a text button; left empty, the button is an eye icon.
    /// </summary>
    [Parameter]
    public string RevealText { get; set; } = string.Empty;

    /// <summary>The word shown while the password is revealed, when the button carries text.</summary>
    [Parameter]
    public string HideText { get; set; } = string.Empty;

    private bool ShowsText => !string.IsNullOrWhiteSpace(RevealText) && !string.IsNullOrWhiteSpace(HideText);

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string EffectiveRevealLabel => string.IsNullOrWhiteSpace(RevealLabel) ? Localize("PasswordRevealLabel") : RevealLabel;
    private string EffectiveHideLabel => string.IsNullOrWhiteSpace(HideLabel) ? Localize("PasswordHideLabel") : HideLabel;

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();
    private void ToggleReveal() => _revealed = !_revealed;

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
