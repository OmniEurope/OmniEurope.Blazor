namespace OmniEurope.Blazor.Components;

/// <summary>
/// A marker of an <see cref="OmniMap"/>: a real button at <see cref="Point"/>, named by <see cref="Label"/>, in the tab order
/// of the map; a click, Enter or Space raises <see cref="OmniMap.OnMarkerClick"/> with this marker.
/// </summary>
public sealed record OmniMapMarker
{
    /// <summary>Where the marker stands.</summary>
    public required OmniGeoPoint Point { get; init; }

    /// <summary>The accessible name of the marker's button, also shown by the package tooltip on hover. Required, never blank.</summary>
    public required string Label { get; init; }

    /// <summary>What the marker stands for, which gives its icon and colour. <see cref="OmniMapMarkerKind.Point"/> by default.</summary>
    public OmniMapMarkerKind Kind { get; init; }

    /// <summary>An icon that replaces the icon of <see cref="Kind"/> (the colour stays the kind's). Null, the default, keeps the kind's icon.</summary>
    public OmniIconName? Icon { get; init; }

    /// <summary>A stable identity for the marker's element across renders (the <c>@key</c> of its button). Null, the default, uses its position in the list.</summary>
    public object? Key { get; init; }
}
