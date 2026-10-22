namespace OmniEurope.Blazor.Components;

/// <summary>
/// The direction of the element itself, the ARIA meaning of <c>aria-orientation</c>: an
/// <see cref="OmniStack"/> is <see cref="Horizontal"/> when it lays its children out in a row, an
/// <see cref="OmniDivider"/> is <see cref="Horizontal"/> when its rule runs across the width.
/// </summary>
public enum OmniOrientation
{
    /// <summary>Runs along the reading direction: a row of children, a rule across the width.</summary>
    Horizontal,

    /// <summary>Runs from top to bottom: children one under the other, a rule down the height.</summary>
    Vertical
}
