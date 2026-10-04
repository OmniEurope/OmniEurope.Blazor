using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A validator that requires a text field to hold an email address, when it holds anything: a null,
/// empty or blank value passes, so a required address pairs it with <see cref="OmniRequiredValidator{TValue}"/>.
/// </summary>
/// <remarks>
/// The value, without its surrounding spaces, must read <c>local@domain</c>: one <c>@</c>; a local part
/// of 1 to 64 letters, digits or <c>!#$%&amp;'*+/=?^_`{|}~-.</c>, without a dot at either end or two
/// dots in a row; a domain of at least two labels of letters, digits or hyphens (63 characters each,
/// 253 in all), none starting or ending with a hyphen, the last one not all digits. Letters of any
/// script are accepted. Quoted local parts, comments and IP literals, valid under RFC 5322 but almost
/// never typed, are refused; the check says nothing about whether the mailbox exists.
/// </remarks>
public partial class OmniEmailValidator
{
    /// <summary>The message of an invalid address; the localized "This field does not contain a valid email address" when null or blank.</summary>
    [Parameter]
    public string? Message { get; set; }

    private string EffectiveMessage => string.IsNullOrWhiteSpace(Message)
        ? Localize("EmailValidatorMessage")
        : Message;

    /// <inheritdoc />
    protected override string? GetValidationError(string? value) =>
        string.IsNullOrWhiteSpace(value) || EmailAddressRule.IsValid(value.Trim()) ? null : EffectiveMessage;
}
