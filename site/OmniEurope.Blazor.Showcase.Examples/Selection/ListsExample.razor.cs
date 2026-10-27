using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A list box and a check box list whose height, legend, disabled options and state are parameters.</summary>
public partial class ListsExample
{
    private static readonly IReadOnlyList<(string Code, string Name)> Cities =
    [
        ("ams", "Amsterdam"),
        ("anv", "Antwerp"),
        ("bru", "Brussels"),
        ("gand", "Ghent"),
        ("liege", "Liège"),
        ("lil", "Lille"),
        ("lux", "Luxembourg"),
        ("nam", "Namur"),
        ("par", "Paris"),
        ("rot", "Rotterdam")
    ];

    /// <summary>The height of the list box, in rows.</summary>
    [Parameter]
    public int VisibleRows { get; set; } = 5;

    /// <summary>Whether the check box list titles its group; hidden, it keeps an accessible name.</summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>Whether Lille and the certified copy stay visible but inert.</summary>
    [Parameter]
    public bool DisableOption { get; set; } = true;

    /// <summary>Whether both controls are greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? City { get; set; } = "bru";

    private IReadOnlyList<string> Extras { get; set; } = ["tracking"];

    private IReadOnlyList<OmniOption<string>> CityOptions =>
    [
        .. Cities.Select(city => new OmniOption<string>(city.Code, city.Name, Disabled: DisableOption && city.Code == "lil"))
    ];

    private IReadOnlyList<OmniOption<string>> ExtraOptions =>
    [
        new("receipt", "Acknowledgement of receipt"),
        new("copy", "Certified copy", Disabled: DisableOption),
        new("tracking", "Email tracking"),
        new("insurance", "Insurance")
    ];

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "(none)" : string.Join(", ", values);
}
