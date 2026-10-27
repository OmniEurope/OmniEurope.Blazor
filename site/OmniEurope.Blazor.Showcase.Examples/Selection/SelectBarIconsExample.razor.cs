using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A view bar whose options carry an icon, the map being the option that can be disabled.</summary>
public partial class SelectBarIconsExample
{
    /// <summary>Whether the map view stays visible but cannot be picked.</summary>
    [Parameter]
    public bool DisableOption { get; set; } = true;

    /// <summary>Whether every button of the bar is greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string View { get; set; } = "list";

    private IReadOnlyList<OmniOption<string>> ViewOptions =>
    [
        new("list", "List"),
        new("grid", "Grid"),
        new("map", "Map", Disabled: DisableOption)
    ];

    /// <summary>A value sitting on a disabled option goes back to the list: a disabled option is never the chosen one.</summary>
    protected override void OnParametersSet()
    {
        if (DisableOption && View == "map")
        {
            View = "list";
        }
    }

    private static OmniIconName? IconOf(string view) => view switch
    {
        "list" => OmniIconName.Rows,
        "grid" => OmniIconName.Pattern,
        "map" => OmniIconName.MapPin,
        _ => null
    };
}
