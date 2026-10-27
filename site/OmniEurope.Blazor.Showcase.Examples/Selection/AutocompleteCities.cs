using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>
/// The in-memory source of the autocomplete examples: a list of European cities searched like a real
/// service would be, with an adjustable delay, cancelled by the next keystroke, and compared without
/// accents so that "liege" finds Liège.
/// </summary>
public static class AutocompleteCities
{
    /// <summary>Beyond this, the list of results overflows the screen without teaching anything more.</summary>
    private const int MaximumResults = 8;

    private static readonly IReadOnlyList<SampleCity> Cities =
    [
        new("ams", "Amsterdam", "Netherlands", false),
        new("anv", "Antwerp", "Belgium", false),
        new("ath", "Athens", "Greece", false),
        new("bcn", "Barcelona", "Spain", false),
        new("ber", "Berlin", "Germany", false),
        new("bor", "Bordeaux", "France", true),
        new("bru", "Brussels", "Belgium", false),
        new("bud", "Budapest", "Hungary", false),
        new("cop", "Copenhagen", "Denmark", false),
        new("dub", "Dublin", "Ireland", false),
        new("gand", "Ghent", "Belgium", false),
        new("gen", "Geneva", "Switzerland", false),
        new("ham", "Hamburg", "Germany", false),
        new("hel", "Helsinki", "Finland", true),
        new("lis", "Lisbon", "Portugal", false),
        new("liege", "Liège", "Belgium", false),
        new("lil", "Lille", "France", false),
        new("lon", "London", "United Kingdom", false),
        new("lux", "Luxembourg", "Luxembourg", false),
        new("lyo", "Lyon", "France", false),
        new("mad", "Madrid", "Spain", false),
        new("mar", "Marseille", "France", false),
        new("mil", "Milan", "Italy", false),
        new("mun", "Munich", "Germany", false),
        new("nam", "Namur", "Belgium", false),
        new("nic", "Nice", "France", true),
        new("osl", "Oslo", "Norway", false),
        new("par", "Paris", "France", false),
        new("pra", "Prague", "Czechia", false),
        new("rom", "Rome", "Italy", false),
        new("rot", "Rotterdam", "Netherlands", false),
        new("sto", "Stockholm", "Sweden", false),
        new("str", "Strasbourg", "France", false),
        new("vie", "Vienna", "Austria", false),
        new("var", "Warsaw", "Poland", false),
        new("zur", "Zurich", "Switzerland", false)
    ];

    /// <summary>The name of the city with that code, or the code itself when there is none.</summary>
    public static string NameOf(string code) => Cities.FirstOrDefault(city => city.Code == code)?.Name ?? code;

    /// <summary>
    /// Searches the cities whose name contains <paramref name="text"/>, ignoring case and accents.
    /// </summary>
    /// <param name="text">What the user typed.</param>
    /// <param name="latencyMilliseconds">How long the fake service takes to answer; the next keystroke cancels the wait.</param>
    /// <param name="markClosedCities">Whether the cities flagged closed are listed but cannot be picked.</param>
    /// <param name="fail">Whether the search throws, so the field shows its error message.</param>
    /// <param name="cancellationToken">Cancelled when the user types again.</param>
    /// <returns>At most eight options whose value is the code of the city.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="fail"/> is set.</exception>
    public static async Task<IReadOnlyList<OmniOption<string>>> SearchAsync(
        string text,
        int latencyMilliseconds,
        bool markClosedCities,
        bool fail,
        CancellationToken cancellationToken)
    {
        if (latencyMilliseconds > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(latencyMilliseconds), cancellationToken);
        }

        if (fail)
        {
            throw new InvalidOperationException("Simulated search failure.");
        }

        var compare = CultureInfo.CurrentCulture.CompareInfo;
        return
        [
            .. Cities
                .Where(city => compare.IndexOf(city.Name, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)
                .Take(MaximumResults)
                .Select(city => new OmniOption<string>(
                    city.Code,
                    city.Closed && markClosedCities ? $"{city.Name}, {city.Country} (closed)" : $"{city.Name}, {city.Country}",
                    Disabled: city.Closed && markClosedCities))
        ];
    }

    private sealed record SampleCity(string Code, string Name, string Country, bool Closed);
}
