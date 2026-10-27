using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A country drop-down whose empty entry, groups, disabled option, search field and state are parameters.</summary>
public partial class DropDownExample
{
    private static readonly IReadOnlyList<SampleCountry> Countries =
    [
        new("be", "Belgium", "Benelux"),
        new("nl", "Netherlands", "Benelux"),
        new("lu", "Luxembourg", "Benelux"),
        new("fr", "France", "Western Europe"),
        new("es", "Spain", "Western Europe"),
        new("pt", "Portugal", "Western Europe"),
        new("de", "Germany", "Central Europe"),
        new("at", "Austria", "Central Europe"),
        new("ch", "Switzerland", "Central Europe")
    ];

    /// <summary>Whether an empty entry at the top sets the value back to null.</summary>
    [Parameter]
    public bool AllowEmpty { get; set; }

    /// <summary>The text of the empty entry; left blank, the control uses its own.</summary>
    [Parameter]
    public string EmptyText { get; set; } = string.Empty;

    /// <summary>Whether the options are filed under the heading of their region.</summary>
    [Parameter]
    public bool Grouped { get; set; }

    /// <summary>Whether Luxembourg stays listed but cannot be chosen.</summary>
    [Parameter]
    public bool DisableOption { get; set; } = true;

    /// <summary>Whether a search field comes before the list and narrows it as you type.</summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>Whether the whole control is greyed out and ignores input.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? Country { get; set; } = "be";

    private string CountryName => Countries.FirstOrDefault(country => country.Code == Country)?.Name ?? "(none)";

    private IReadOnlyList<OmniOption<string>> CountryOptions =>
    [
        .. Countries.Select(country => new OmniOption<string>(
            country.Code,
            country.Name,
            Disabled: DisableOption && country.Code == "lu",
            Group: Grouped ? country.Region : null))
    ];

    private sealed record SampleCountry(string Code, string Name, string Region);
}
