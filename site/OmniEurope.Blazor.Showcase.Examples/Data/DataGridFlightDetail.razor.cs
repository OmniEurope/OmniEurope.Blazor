using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>The detail panel of a flight: its aircraft, route, seats taken and arrival time.</summary>
public partial class DataGridFlightDetail
{
    /// <summary>The flight the panel describes.</summary>
    [Parameter, EditorRequired]
    public Flight Flight { get; set; } = default!;

    private string AircraftText => $"Aircraft: {Flight.Aircraft}";

    private string RouteText => $"Route: {Flight.Origin} to {Flight.Destination}";

    private string SeatsText => $"Seats taken: {Flight.SeatsTaken} of {Flight.Seats}";

    private string ArrivalText => string.Create(
        CultureInfo.CurrentCulture,
        $"Arrival at {Flight.Departure.AddMinutes(Flight.DurationMinutes).ToString("HH:mm", CultureInfo.CurrentCulture)}");
}
