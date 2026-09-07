namespace OmniEurope.Blazor.Components;

/// <summary>
/// How much of the severity colour an alert carries.
/// </summary>
public enum OmniAlertVariant
{
    /// <summary>Coloured text and border over the page surface.</summary>
    Outline,

    /// <summary>Solid severity background, for a message that must not be scrolled past.</summary>
    Filled
}
