namespace OmniEurope.Blazor.Components;

/// <summary>
/// The HTML <c>type</c> of an <see cref="OmniButton"/>, rendered in lower case on the
/// <c>&lt;button&gt;</c> element.
/// </summary>
public enum OmniButtonType
{
    /// <summary>A plain button that does nothing in a form, the default of <see cref="OmniButton"/>.</summary>
    Button,

    /// <summary>Submits the form that contains the button.</summary>
    Submit,

    /// <summary>Resets the form that contains the button to its initial values.</summary>
    Reset
}
