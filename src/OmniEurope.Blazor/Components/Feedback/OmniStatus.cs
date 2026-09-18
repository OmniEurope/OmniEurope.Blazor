namespace OmniEurope.Blazor.Components;

/// <summary>
/// How one value of a status is drawn by <see cref="OmniStatusBadge{TValue}"/>: the badge colour, its
/// label, an optional icon and an optional explanation shown on hover.
/// </summary>
/// <param name="Variant">The colour of the badge.</param>
/// <param name="Text">
/// The label. When the <see cref="OmniStatusMap{TValue}"/> carries a
/// <see cref="OmniStatusMap{TValue}.Localizer"/>, it is a resource key of the host, resolved at render.
/// </param>
public sealed record OmniStatus(OmniBadgeVariant Variant, string Text)
{
    /// <summary>An icon drawn before the label.</summary>
    public OmniIconName? Icon { get; init; }

    /// <summary>
    /// What the status means, for the values a reader cannot guess. Resolved through the map's
    /// localizer like <see cref="Text"/>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>Painted or drawn: an outlined badge reads as a different kind of state from the filled ones.</summary>
    public OmniBadgeFill Fill { get; init; } = OmniBadgeFill.Filled;
}
