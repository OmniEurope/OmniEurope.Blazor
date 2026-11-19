namespace OmniEurope.Blazor.Components;

/// <summary>What a marker of an <see cref="OmniMap"/> stands for: each kind has its own icon and colour.</summary>
public enum OmniMapMarkerKind
{
    /// <summary>A place: a map pin on the accent colour. The default.</summary>
    Point,

    /// <summary>The start of a route: a play sign on the success colour.</summary>
    Start,

    /// <summary>A stop on the way: a smaller neutral disc with a dot.</summary>
    Waypoint,

    /// <summary>The end of a route: a flag on the danger colour.</summary>
    End
}
