namespace OmniEurope.Blazor.Components;

/// <summary>
/// Where notifications pile up on the screen. Named by block then inline edge, so the meaning holds
/// in a right-to-left reading direction as well: Start is the side text begins on.
/// </summary>
public enum OmniNotificationPosition
{
    TopStart,
    TopCenter,
    TopEnd,
    BottomStart,
    BottomCenter,
    BottomEnd
}
