namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>
/// The state the sign-up form edits. It carries no validation attribute on purpose: the required rule is
/// placed in the markup, next to each field it guards.
/// </summary>
public sealed class SignUpModel
{
    /// <summary>The name of the account.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The address that confirms the account.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The password.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>The password typed a second time.</summary>
    public string Confirmation { get; set; } = string.Empty;
}
