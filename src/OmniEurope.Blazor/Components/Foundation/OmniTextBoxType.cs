namespace OmniEurope.Blazor.Components;

/// <summary>
/// The <c>type</c> a single-line text box renders. A closed set rather than a free string: these
/// are the types that keep the same value semantics as <c>text</c> (a string the component parses
/// as-is), and they are what changes the on-screen keyboard and the browser's own autofill.
/// </summary>
/// <remarks>
/// <c>password</c> is deliberately absent: it is <see cref="OmniPassword"/>, which owns a reveal
/// button and its labels. So are <c>number</c>, <c>date</c> and the other typed inputs, which are
/// their own components because their value is not a string.
/// </remarks>
public enum OmniTextBoxType
{
    Text,
    Email,
    Tel,
    Url,
    Search
}
