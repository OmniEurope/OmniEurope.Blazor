using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A multiple selection of countries whose search field, wording, closing and state are parameters.</summary>
public partial class MultiSelectExample
{
    private static readonly IReadOnlyList<(string Code, string Name)> CountryData =
    [
        ("de", "Germany"),
        ("at", "Austria"),
        ("be", "Belgium"),
        ("dk", "Denmark"),
        ("es", "Spain"),
        ("fr", "France"),
        ("ie", "Ireland"),
        ("it", "Italy"),
        ("lu", "Luxembourg"),
        ("nl", "Netherlands"),
        ("pt", "Portugal"),
        ("ch", "Switzerland")
    ];

    /// <summary>Whether a search field narrows the options to those containing the typed text.</summary>
    [Parameter]
    public bool Filterable { get; set; } = true;

    /// <summary>The summary of the closed control while nothing is selected.</summary>
    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>The text of the search field; left blank, the control uses its own.</summary>
    [Parameter]
    public string FilterPlaceholder { get; set; } = string.Empty;

    /// <summary>Whether a click elsewhere on the page closes the panel.</summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>Whether Luxembourg stays listed but cannot be ticked.</summary>
    [Parameter]
    public bool DisableOption { get; set; }

    /// <summary>Whether the whole control is greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private IReadOnlyList<string> Countries { get; set; } = ["be", "fr"];

    private string? CountrySearch { get; set; }

    private string? EffectiveFilterPlaceholder =>
        string.IsNullOrWhiteSpace(FilterPlaceholder) ? null : FilterPlaceholder;

    private IReadOnlyList<OmniOption<string>> CountryOptions =>
    [
        .. CountryData.Select(country => new OmniOption<string>(
            country.Code,
            country.Name,
            Disabled: DisableOption && country.Code == "lu"))
    ];
}
