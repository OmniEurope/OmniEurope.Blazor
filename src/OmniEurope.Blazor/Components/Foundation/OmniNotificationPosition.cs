namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where notifications pile up on the screen. Named by block then inline edge, so the meaning holds
/// in a right-to-left reading direction as well: Start is the side text begins on.
/// </summary>
public enum OmniNotificationPosition
{
    /// <summary>The top corner on the side text begins on.</summary>
    TopStart,

    /// <summary>Centred along the top edge.</summary>
    TopCenter,

    /// <summary>The top corner on the side text ends on. The default of <see cref="OmniNotificationOptions"/>.</summary>
    TopEnd,

    /// <summary>The bottom corner on the side text begins on.</summary>
    BottomStart,

    /// <summary>Centred along the bottom edge.</summary>
    BottomCenter,

    /// <summary>The bottom corner on the side text ends on.</summary>
    BottomEnd
}
