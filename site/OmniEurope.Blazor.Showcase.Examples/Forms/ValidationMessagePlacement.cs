namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>Where the validation messages of the sign-up form appear.</summary>
public enum ValidationMessagePlacement
{
    /// <summary>Under each validator, which shows its own message.</summary>
    Validator,

    /// <summary>Inside the form field, which shows the first message of its value.</summary>
    Field,

    /// <summary>Gathered in a summary above the form.</summary>
    Summary
}
