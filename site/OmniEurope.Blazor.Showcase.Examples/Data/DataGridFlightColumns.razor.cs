using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>The columns of the flights grid, one per filter shape, with a status badge, a duration and a total.</summary>
public partial class DataGridFlightColumns
{
    /// <summary>The rows the grid shows, for the total in the footer of the price column.</summary>
    [Parameter]
    public IReadOnlyList<Flight> Flights { get; set; } = [];

    /// <summary>Whether the flight number stays in place while the table scrolls sideways.</summary>
    [Parameter]
    public bool FreezeNumber { get; set; }

    private string TotalText => string.Create(
        CultureInfo.CurrentCulture,
        $"Total: {Flights.Sum(flight => flight.Price).ToString("C0", CultureInfo.CurrentCulture)}");

    private IReadOnlyList<OmniOption<FlightStatus>> StatusOptions =>
    [
        .. Enum.GetValues<FlightStatus>().Select(status => new OmniOption<FlightStatus>(status, StatusLabel(status)))
    ];

    private object? StatusValue(Flight flight) => StatusLabel(flight.Status);

    private static string StatusLabel(FlightStatus status) => status switch
    {
        FlightStatus.OnTime => "On time",
        FlightStatus.Boarding => "Boarding",
        FlightStatus.Delayed => "Delayed",
        _ => "Cancelled"
    };

    private static OmniTone StatusTone(FlightStatus status) => status switch
    {
        FlightStatus.OnTime => OmniTone.Success,
        FlightStatus.Boarding => OmniTone.Accent,
        FlightStatus.Delayed => OmniTone.Warning,
        _ => OmniTone.Danger
    };

    private static string FormatDuration(int minutes) =>
        string.Create(CultureInfo.CurrentCulture, $"{minutes / 60} h {(minutes % 60).ToString("00", CultureInfo.CurrentCulture)}");

    /// <summary>An almost full flight is noticed without reading the figure.</summary>
    private static OmniTextTone LoadTone(double load) => load switch
    {
        >= 0.95 => OmniTextTone.Danger,
        >= 0.8 => OmniTextTone.Warning,
        _ => OmniTextTone.Neutral
    };
}
