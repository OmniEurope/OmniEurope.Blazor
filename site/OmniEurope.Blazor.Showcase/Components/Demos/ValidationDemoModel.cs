namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The state the validation demonstration edits. It carries no validation attribute on purpose: the
/// rules live in the markup, next to the fields they guard.
/// </summary>
public sealed class ValidationDemoModel
{
    /// <summary>The required, length-checked name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The address checked for shape.</summary>
    public string Mail { get; set; } = string.Empty;

    /// <summary>The address compared against <see cref="Mail"/>.</summary>
    public string MailConfirmation { get; set; } = string.Empty;
}
