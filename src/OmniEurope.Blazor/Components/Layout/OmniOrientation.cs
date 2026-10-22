namespace OmniEurope.Blazor.Components;

/// <summary>
/// The direction an element runs in: the way an <see cref="OmniStack"/> lays its children out, the way an
/// <see cref="OmniDivider"/> draws its rule, the way an <see cref="OmniAxisTitle"/> writes its text.
/// </summary>
public enum OmniOrientation
{
    /// <summary>Side by side, in the reading direction (<c>flex-direction: row</c>).</summary>
    Horizontal,

    /// <summary>One under the other (<c>flex-direction: column</c>). The default.</summary>
    Vertical
}
