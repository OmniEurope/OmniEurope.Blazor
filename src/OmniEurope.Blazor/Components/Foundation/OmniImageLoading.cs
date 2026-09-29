namespace OmniEurope.Blazor.Components;

/// <summary>When the browser loads an <see cref="OmniImage"/>: its <c>loading</c> attribute.</summary>
public enum OmniImageLoading
{
    /// <summary>Loads at once, wherever the image is on the page (<c>loading="eager"</c>).</summary>
    Eager,

    /// <summary>Waits until the image nears the viewport (<c>loading="lazy"</c>).</summary>
    Lazy
}
