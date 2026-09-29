namespace OmniEurope.Blazor.Components;

/// <summary>
/// A step of the spacing scale, used for the gap of an <see cref="OmniRow"/> or an <see cref="OmniStack"/>.
/// Each step reads a <c>--omni-space-*</c> token, so its size follows the theme and the density.
/// </summary>
public enum OmniSpacing
{
    /// <summary>No space.</summary>
    None,

    /// <summary>The smallest step (<c>--omni-space-xs</c>).</summary>
    XSmall,

    /// <summary>A small step (<c>--omni-space-sm</c>).</summary>
    Small,

    /// <summary>The middle step (<c>--omni-space-md</c>), the default of both containers.</summary>
    Medium,

    /// <summary>A large step (<c>--omni-space-lg</c>).</summary>
    Large,

    /// <summary>The largest step (<c>--omni-space-xl</c>).</summary>
    XLarge
}
