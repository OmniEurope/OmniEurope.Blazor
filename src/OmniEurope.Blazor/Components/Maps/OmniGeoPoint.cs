namespace OmniEurope.Blazor.Components;

/// <summary>
/// A place on the Earth in decimal degrees (WGS 84, the coordinates of a GPS or of OpenStreetMap): the latitude, north
/// positive, and the longitude, east positive. An <see cref="OmniMap"/> draws it in Web Mercator, which cannot show the
/// poles: a latitude beyond <see cref="MaxLatitude"/> north or south is drawn on that limit.
/// </summary>
/// <param name="Latitude">Degrees north of the equator, -90 to 90 (south is negative).</param>
/// <param name="Longitude">Degrees east of Greenwich, -180 to 180 (west is negative).</param>
public readonly record struct OmniGeoPoint(double Latitude, double Longitude)
{
    /// <summary>The northernmost latitude Web Mercator draws (and, negative, the southernmost): about 85.0511 degrees, where the square world of the tiles ends.</summary>
    public const double MaxLatitude = 85.0511287798066;

    // The point clamped into the square world of Web Mercator, its longitude brought back between -180 and 180.
    internal OmniGeoPoint ToMercatorRange()
    {
        var longitude = Longitude;
        if (longitude is < -180 or > 180)
        {
            longitude = ((longitude + 180) % 360 + 360) % 360 - 180;
        }

        return new OmniGeoPoint(Math.Clamp(Latitude, -MaxLatitude, MaxLatitude), longitude);
    }

    // Whether both coordinates are numbers (not NaN, not infinite): the only points a map can place.
    internal bool IsFinite => double.IsFinite(Latitude) && double.IsFinite(Longitude);
}
