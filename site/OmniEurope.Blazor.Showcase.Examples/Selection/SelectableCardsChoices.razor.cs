using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The three transport cards the selectable card groups of the example are filled with.</summary>
public partial class SelectableCardsChoices
{
    /// <summary>The bound values, data and not labels.</summary>
    public const string Train = "train";
    public const string Plane = "plane";
    public const string Car = "car";

    /// <summary>Whether each card shows its explanation line under its title.</summary>
    [Parameter]
    public bool ShowDescriptions { get; set; } = true;

    private string? Describe(string description) => ShowDescriptions ? description : null;
}
