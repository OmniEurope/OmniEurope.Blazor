using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Where a flight stands. The grid shows it under a readable label, never under the name of this enumeration.</summary>
public enum FlightStatus
{
    /// <summary>The flight runs to schedule.</summary>
    OnTime,

    /// <summary>The gate is open.</summary>
    Boarding,

    /// <summary>The flight leaves later than planned.</summary>
    Delayed,

    /// <summary>The flight does not leave.</summary>
    Cancelled
}

/// <summary>A sample flight. Status and gate stay settable: the grid edits them in place.</summary>
public sealed class Flight
{
    /// <summary>The stable key of the flight.</summary>
    public int Id { get; init; }

    /// <summary>The flight number, an airline code and four digits.</summary>
    public string Number { get; init; } = string.Empty;

    /// <summary>The airline.</summary>
    public string Airline { get; init; } = string.Empty;

    /// <summary>The departure city.</summary>
    public string Origin { get; init; } = string.Empty;

    /// <summary>The arrival city.</summary>
    public string Destination { get; init; } = string.Empty;

    /// <summary>When the flight leaves.</summary>
    public DateTime Departure { get; init; }

    /// <summary>The flight time, in minutes.</summary>
    public int DurationMinutes { get; init; }

    /// <summary>Where the flight stands.</summary>
    public FlightStatus Status { get; set; }

    /// <summary>The boarding gate.</summary>
    public string Gate { get; set; } = string.Empty;

    /// <summary>The aircraft model.</summary>
    public string Aircraft { get; init; } = string.Empty;

    /// <summary>The seats of the aircraft.</summary>
    public int Seats { get; init; }

    /// <summary>The seats sold.</summary>
    public int SeatsTaken { get; init; }

    /// <summary>The share of the seats sold, from 0 to 1.</summary>
    public double LoadFactor { get; init; }

    /// <summary>The fare.</summary>
    public decimal Price { get; init; }
}

/// <summary>The sample flights of the data grid examples.</summary>
public static class FlightData
{
    private const int FlightCount = 240;

    // Fictional airlines and accented cities: enough to try the case and the accents of the filters.
    private static readonly string[] Airlines =
        ["Aéro Lumière", "Brise Atlantique", "Céleste Air", "Nordique Express", "Ibérica Vuelos", "Alpes Jet"];

    private static readonly string[] AirlineCodes = ["AL", "BA", "CA", "NX", "IV", "AJ"];

    private static readonly string[] Cities =
        ["Bruxelles", "Liège", "Genève", "Zürich", "Málaga", "Kraków", "Göteborg", "Lisbonne", "Marseille", "Reykjavík", "Toulouse", "Porto"];

    private static readonly string[] AircraftModels = ["A220-300", "A320neo", "A321neo", "B737-800", "E195-E2", "ATR 72-600"];

    private static readonly int[] SeatCapacities = [140, 180, 220, 189, 132, 72];

    // On time is the most common status: a grid where everything is cancelled looks like nothing.
    private static readonly FlightStatus[] StatusDraw =
        [FlightStatus.OnTime, FlightStatus.OnTime, FlightStatus.OnTime, FlightStatus.Boarding, FlightStatus.Delayed, FlightStatus.Cancelled];

    /// <summary>A day of flights drawn from a fixed seed, so the example is the same at every visit.</summary>
    public static IReadOnlyList<Flight> Create()
    {
        var random = new Random(2026);
        var start = DateTime.Today.AddHours(5);
        var flights = new List<Flight>(FlightCount);
        for (var index = 0; index < FlightCount; index++)
        {
            var airline = random.Next(Airlines.Length);
            var origin = random.Next(Cities.Length);
            var destination = (origin + 1 + random.Next(Cities.Length - 1)) % Cities.Length;
            var model = random.Next(AircraftModels.Length);
            var seats = SeatCapacities[model];
            var taken = random.Next(seats / 3, seats + 1);
            flights.Add(new Flight
            {
                Id = index + 1,
                Number = string.Create(CultureInfo.InvariantCulture, $"{AirlineCodes[airline]}{1000 + random.Next(9000)}"),
                Airline = Airlines[airline],
                Origin = Cities[origin],
                Destination = Cities[destination],
                Departure = start.AddMinutes((index * 6) + random.Next(5)),
                DurationMinutes = 55 + (random.Next(40) * 5),
                Status = StatusDraw[random.Next(StatusDraw.Length)],
                Gate = string.Create(CultureInfo.InvariantCulture, $"{(char)('A' + random.Next(5))}{1 + random.Next(40)}"),
                Aircraft = AircraftModels[model],
                Seats = seats,
                SeatsTaken = taken,
                LoadFactor = taken / (double)seats,
                Price = 49 + (random.Next(60) * 5)
            });
        }

        return flights;
    }
}
