namespace OmniEurope.Blazor.Components;

/// <summary>
/// How children are spread along the main axis (<c>justify-content</c>) of an <see cref="OmniRow"/> or an
/// <see cref="OmniStack"/>, and where an <see cref="OmniPager"/> sits in its row.
/// </summary>
public enum OmniJustification
{
    /// <summary>Packed against the start edge. The default.</summary>
    Start,

    /// <summary>Packed in the centre.</summary>
    Center,

    /// <summary>Packed against the end edge.</summary>
    End,

    /// <summary>The first child at the start, the last at the end, the free space shared between them (<c>space-between</c>).</summary>
    Between
}
