namespace OmniEurope.Blazor.Components;

/// <summary>
/// A placeholder drawn in the shape of content that is still loading. Without a <see cref="Label"/> it is
/// hidden from assistive technologies; with one it becomes a <c>role="status"</c> that announces it.
/// </summary>
public partial class OmniSkeleton
{
    /// <summary>The outline of the placeholder; <see cref="OmniSkeletonShape.Text"/> by default.</summary>
    [Parameter]
    public OmniSkeletonShape Shape { get; set; }

    /// <summary>
    /// The number of text lines drawn, 1 by default, clamped to 1 to 10. Only read for
    /// <see cref="OmniSkeletonShape.Text"/>; the other shapes always draw one block.
    /// </summary>
    [Parameter]
    public int LineCount { get; set; } = 1;

    /// <summary>
    /// Text announced to assistive technologies, visually hidden. Null or blank, the default, makes the
    /// placeholder decorative (<c>aria-hidden</c>). Not localized: the caller passes the text.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    private int NormalizedLineCount => Shape == OmniSkeletonShape.Text
        ? Math.Clamp(LineCount, 1, 10)
        : 1;
}
