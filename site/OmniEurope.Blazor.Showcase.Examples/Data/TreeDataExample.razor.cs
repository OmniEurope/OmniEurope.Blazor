using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A tree generated from data, with a branch loaded on opening whose delay and failure are parameters.</summary>
public partial class TreeDataExample
{
    // Regions, countries and cities: the tree is built from this single list. The cities marked closed
    // are the ones the DisableItems parameter turns into disabled nodes.
    private static readonly IReadOnlyList<TreeRegion> Regions =
    [
        new("benelux", "Benelux",
        [
            new("be", "Belgium", [new("bru", "Brussels", false), new("anv", "Antwerp", false), new("liege", "Liège", true)]),
            new("nl", "Netherlands", [new("ams", "Amsterdam", false), new("rot", "Rotterdam", false)]),
            new("lu", "Luxembourg", [new("lux", "Luxembourg", false)])
        ]),
        new("south", "Southern Europe",
        [
            new("fr", "France", [new("par", "Paris", false), new("lyo", "Lyon", false), new("nic", "Nice", true)]),
            new("es", "Spain", [new("mad", "Madrid", false), new("bcn", "Barcelona", false)]),
            new("it", "Italy", [new("rom", "Rome", false), new("mil", "Milan", false)])
        ]),
        new("central", "Central Europe",
        [
            new("de", "Germany", [new("ber", "Berlin", false), new("mun", "Munich", false)]),
            new("ch", "Switzerland", [new("zur", "Zurich", false), new("gen", "Geneva", true)])
        ])
    ];

    private static readonly IReadOnlyList<TreeCity> NordicCities =
        [new("osl", "Oslo", false), new("hel", "Helsinki", false), new("rey", "Reykjavik", false)];

    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal) { "benelux", "be" };
    private readonly List<TreeCity> _lazyCities = [];

    /// <summary>Whether several items can be selected; off, selecting an item releases the previous one.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Whether the cities marked closed stay visible but can no longer be selected.</summary>
    [Parameter]
    public bool DisableItems { get; set; } = true;

    /// <summary>The time the branch loaded on opening takes, in milliseconds.</summary>
    [Parameter]
    public double LatencyMilliseconds { get; set; } = 800;

    /// <summary>Whether the branch fails to load; closing and opening it again retries.</summary>
    [Parameter]
    public bool FailLoad { get; set; }

    private IReadOnlyList<string> Selection { get; set; } = ["bru"];

    private string? LastActivity { get; set; }

    private int FailureCount { get; set; }

    private IReadOnlyList<TreeCity> LazyCities => _lazyCities;

    private string SelectionText => Selection.Count == 0 ? "(none)" : string.Join(", ", Selection.Select(NameOf));

    private string ActivityEcho => LastActivity is null
        ? $"Failed loads: {FailureCount}."
        : $"{LastActivity} Failed loads: {FailureCount}.";

    private static string Join(IReadOnlyList<string> values) =>
        values.Count == 0 ? "(none)" : string.Join(", ", values);

    private static string NameOf(string value)
    {
        if (value == "more")
        {
            return "Nordic destinations, loaded on opening";
        }

        var region = Regions.FirstOrDefault(candidate => candidate.Id == value);
        if (region is not null)
        {
            return region.Name;
        }

        var countries = Regions.SelectMany(candidate => candidate.Countries).ToArray();
        var country = countries.FirstOrDefault(candidate => candidate.Id == value);
        if (country is not null)
        {
            return country.Name;
        }

        return countries.SelectMany(candidate => candidate.Cities).Concat(NordicCities)
            .FirstOrDefault(city => city.Id == value)?.Name ?? value;
    }

    private bool IsExpanded(string id) => _expanded.Contains(id);

    private void SetExpanded(string id, bool expanded)
    {
        if (expanded)
        {
            _expanded.Add(id);
        }
        else
        {
            _expanded.Remove(id);
        }

        LastActivity = $"Branch {(expanded ? "opened" : "closed")}: {NameOf(id)}.";
    }

    private void ExpandAll()
    {
        foreach (var region in Regions)
        {
            _expanded.Add(region.Id);
            foreach (var country in region.Countries)
            {
                _expanded.Add(country.Id);
            }
        }

        LastActivity = "Every branch opened.";
    }

    private void CollapseAll()
    {
        _expanded.Clear();
        LastActivity = "Every branch closed.";
    }

    // The branch loaded on opening: the tree waits on the task before it draws the level. A failure
    // leaves the branch empty, and opening it again retries the load.
    private async Task LoadMoreAsync(CancellationToken cancellationToken)
    {
        if (LatencyMilliseconds > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(LatencyMilliseconds), cancellationToken);
        }

        if (FailLoad)
        {
            throw new InvalidOperationException("Simulated branch load failure.");
        }

        if (_lazyCities.Count == 0)
        {
            _lazyCities.AddRange(NordicCities);
        }
    }

    private void OnLoadFailed(Exception exception) => FailureCount++;

    private sealed record TreeCity(string Id, string Name, bool Closed);

    private sealed record TreeCountry(string Id, string Name, IReadOnlyList<TreeCity> Cities);

    private sealed record TreeRegion(string Id, string Name, IReadOnlyList<TreeCountry> Countries);
}
