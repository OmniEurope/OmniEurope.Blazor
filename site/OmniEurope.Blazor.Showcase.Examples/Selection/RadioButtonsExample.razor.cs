using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A shipping-method radio list whose legend, disabled option and state are parameters.</summary>
public partial class RadioButtonsExample
{
    /// <summary>Whether the list titles its group; hidden, it keeps an accessible name.</summary>
    [Parameter]
    public bool ShowLegend { get; set; } = true;

    /// <summary>Whether agency pickup stays visible but cannot be chosen.</summary>
    [Parameter]
    public bool DisableOption { get; set; } = true;

    /// <summary>Whether the whole group is greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? Shipping { get; set; } = "standard";

    private IReadOnlyList<OmniOption<string>> ShippingOptions =>
    [
        new("standard", "Standard"),
        new("express", "Express"),
        new("pickup", "Agency pickup", Disabled: DisableOption)
    ];
}
