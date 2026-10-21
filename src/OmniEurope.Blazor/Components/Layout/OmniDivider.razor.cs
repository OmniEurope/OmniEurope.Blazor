namespace OmniEurope.Blazor.Components;

/// <summary>
/// A thin rule that separates two blocks of content, typically between the children of an
/// <see cref="OmniStack"/>. It renders a <c>div</c> with <c>role="separator"</c> for both directions
/// (an <c>hr</c> brings its own margins and border box that a vertical rule would have to undo, and one
/// element for both keeps the markup, the ARIA state and the styling identical). The divider adds no
/// margin: the gap of the surrounding stack spaces it, and it stretches across the stack on its cross
/// axis while keeping its one pixel on the main axis.
/// </summary>
public partial class OmniDivider
{
    /// <summary>
    /// The direction of the rule itself, which is also what <c>aria-orientation</c> reports: a
    /// <see cref="OmniStackOrientation.Horizontal"/> rule runs across the width and separates blocks laid
    /// one under the other (the default), a <see cref="OmniStackOrientation.Vertical"/> rule runs down
    /// the height and separates blocks laid side by side in a row stack. This is the direction of the
    /// line, not the direction of the stack that holds it. A value outside the enumeration is drawn
    /// as the default, horizontal.
    /// </summary>
    [Parameter]
    public OmniStackOrientation Orientation { get; set; } = OmniStackOrientation.Horizontal;

    private bool Vertical => Orientation == OmniStackOrientation.Vertical;
}
