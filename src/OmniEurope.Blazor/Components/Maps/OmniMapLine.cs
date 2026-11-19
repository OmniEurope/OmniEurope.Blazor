namespace OmniEurope.Blazor.Components;

/// <summary>
/// A line drawn on an <see cref="OmniMap"/> through its <see cref="Points"/>, in order: a route, or one stretch of it. Its
/// colour comes from <see cref="Tone"/>, or from <see cref="Color"/> when that is a valid CSS colour; a light casing under it
/// keeps it readable on any tile. Several lines with different tones draw a route in stretches (one per weather, per
/// mode of transport...).
/// </summary>
public sealed record OmniMapLine
{
    /// <summary>The points of the line, in order; two at least.</summary>
    public required IReadOnlyList<OmniGeoPoint> Points { get; init; }

    /// <summary>The colour of the line from the theme: <see cref="OmniTone.Accent"/> by default; <see cref="OmniTone.Neutral"/> is the text colour.</summary>
    public OmniTone Tone { get; init; } = OmniTone.Accent;

    /// <summary>
    /// A colour that replaces the one of <see cref="Tone"/>: a CSS hex colour (<c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c>,
    /// <c>#rrggbbaa</c>) or an <c>rgb()</c>, <c>rgba()</c>, <c>hsl()</c> or <c>hsla()</c> of numbers. It is written as the
    /// <c>stroke</c> presentation attribute of the line, never as a style. Anything else is ignored and the tone applies.
    /// Null by default.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>The width of the line in CSS pixels, whatever the zoom: 5 by default, a number above zero and at most 40.</summary>
    public double Width { get; init; } = 5;

    /// <summary>
    /// A text shown by the package tooltip while the pointer is on the line, and part of the accessible description of the
    /// map (the lines are not focusable, so their texts are read with the map). Null by default: no tooltip.
    /// </summary>
    public string? Tooltip { get; init; }

    /// <summary>Extra CSS classes on the line's SVG group. Null by default.</summary>
    public string? Class { get; init; }

    /// <summary>A stable identity for the line's element across renders (its <c>@key</c>). Null, the default, uses its position in the list.</summary>
    public object? Key { get; init; }
}
