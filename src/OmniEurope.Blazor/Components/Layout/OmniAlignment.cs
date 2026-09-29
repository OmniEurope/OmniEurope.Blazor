namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where the children of an <see cref="OmniRow"/> or an <see cref="OmniStack"/> sit on its cross axis
/// (<c>align-items</c>).
/// </summary>
public enum OmniAlignment
{
    /// <summary>Against the start edge of the cross axis (<c>flex-start</c>).</summary>
    Start,

    /// <summary>Centred on the cross axis.</summary>
    Center,

    /// <summary>Against the end edge of the cross axis (<c>flex-end</c>).</summary>
    End,

    /// <summary>Stretched to fill the cross axis, the default of both containers.</summary>
    Stretch
}
