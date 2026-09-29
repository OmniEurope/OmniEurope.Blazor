namespace OmniEurope.Blazor.Components;

/// <summary>
/// A password field with an eye that reveals what was typed. <see cref="OmniInputBase{TValue}.Class"/>
/// goes on the wrapper that holds the field and the eye; <see cref="OmniInputBase{TValue}.Id"/> and the
/// additional attributes on the input.
/// </summary>
public partial class OmniPassword
{
    private bool _revealed;

    /// <summary>A hint shown while the field is empty.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The <c>autocomplete</c> token; <c>current-password</c> by default, <c>new-password</c> for a new one.</summary>
    [Parameter]
    public string? Autocomplete { get; set; } = "current-password";

    /// <summary>Disables the field and its eye.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the value without letting it change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Draws the eye that reveals the value; on by default.</summary>
    [Parameter]
    public bool Revealable { get; set; } = true;

    /// <summary>
    /// For a secret that is not the user's own password (a vault value, an API key): the field is a text
    /// input masked by the stylesheet and marked for password managers to leave alone, so the browser
    /// neither offers to save it nor fills the login name into the fields around it. The eye still
    /// reveals it. False, the default, keeps a real password field.
    /// </summary>
    [Parameter]
    public bool IgnorePasswordManagers { get; set; }

    private string InputType => _revealed || IgnorePasswordManagers ? "text" : "password";

    private bool MaskedByStyle => IgnorePasswordManagers && !_revealed;

    /// <summary>Accessible name of the eye while the value is hidden; the localized "Show the password" when null or blank.</summary>
    [Parameter]
    public string? RevealLabel { get; set; }

    /// <summary>Accessible name of the eye while the value shows; the localized "Hide the password" when null or blank.</summary>
    [Parameter]
    public string? HideLabel { get; set; }

    /// <summary>The id of the element that describes the field (<c>aria-describedby</c>).</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string EffectiveRevealLabel => LocalizeOr(RevealLabel, "PasswordRevealLabel");
    private string EffectiveHideLabel => LocalizeOr(HideLabel, "PasswordHideLabel");

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();
    private void ToggleReveal() => _revealed = !_revealed;

    /// <summary>Takes the text as it is, null as an empty string; never fails.</summary>
    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
