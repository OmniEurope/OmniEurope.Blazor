namespace OmniEurope.Blazor.Components;

/// <summary>
/// How one value of a status is drawn by <see cref="OmniStatusBadge{TValue}"/>: the badge colour, its
/// label, an optional icon and an optional explanation shown on hover.
/// </summary>
/// <param name="Tone">The colour intention of the badge.</param>
/// <param name="Text">
/// The label. When the <see cref="OmniStatusMap{TValue}"/> carries a
/// <see cref="OmniStatusMap{TValue}.Localizer"/>, it is a resource key of the host, resolved at render.
/// </param>
public sealed record OmniStatus(OmniTone Tone, string Text)
{
    /// <summary>An icon drawn before the label.</summary>
    public OmniIconName? Icon { get; init; }

    /// <summary>
    /// What the status means, for the values a reader cannot guess. Resolved through the map's
    /// localizer like <see cref="Text"/>.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Painted or drawn: <see cref="OmniFill.Tonal"/> by default; an outlined badge reads as a different
    /// kind of state from the tinted ones.
    /// </summary>
    public OmniFill Fill { get; init; } = OmniFill.Tonal;
}
