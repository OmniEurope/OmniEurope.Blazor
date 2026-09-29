namespace OmniEurope.Blazor.Components;

/// <summary>The direction an <see cref="OmniStack"/> lays its children out in.</summary>
public enum OmniStackOrientation
{
    /// <summary>Side by side, in the reading direction (<c>flex-direction: row</c>).</summary>
    Horizontal,

    /// <summary>One under the other (<c>flex-direction: column</c>). The default.</summary>
    Vertical
}
