namespace OmniEurope.Blazor.Components;

/// <summary>
/// The size of a control, shared by <see cref="OmniButton"/>, <see cref="OmniToggleButton"/> and the
/// other controls that expose a <c>Size</c> parameter. It sets the height, padding and icon size
/// through a CSS modifier class.
/// </summary>
public enum OmniControlSize
{
    /// <summary>Shorter than the standard control height, with smaller text and tighter padding.</summary>
    Small,

    /// <summary>The standard control height, the same as a neighbouring input field. The default.</summary>
    Medium,

    /// <summary>Taller than the standard control height, with larger text and wider padding.</summary>
    Large
}
