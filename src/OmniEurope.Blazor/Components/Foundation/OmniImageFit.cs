namespace OmniEurope.Blazor.Components;

/// <summary>How an <see cref="OmniImage"/> fills the box its width and height give it.</summary>
public enum OmniImageFit
{
    /// <summary>No <c>object-fit</c> rule: the browser's default rendering. The default.</summary>
    Natural,

    /// <summary>The whole image stays visible, letterboxed inside the box (<c>object-fit: contain</c>).</summary>
    Contain,

    /// <summary>The image covers the whole box and is cropped to it (<c>object-fit: cover</c>).</summary>
    Cover
}
