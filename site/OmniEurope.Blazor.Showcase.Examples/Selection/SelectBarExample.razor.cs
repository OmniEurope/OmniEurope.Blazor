using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A period bar whose number of options, disabled option and state are parameters.</summary>
public partial class SelectBarExample
{
    /// <summary>How many periods the bar offers, from two to five.</summary>
    [Parameter]
    public int OptionCount { get; set; } = 5;

    /// <summary>Whether the quarter stays visible but cannot be picked.</summary>
    [Parameter]
    public bool DisableOption { get; set; } = true;

    /// <summary>Whether every button of the bar is greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? Period { get; set; } = "month";

    private IReadOnlyList<OmniOption<string>> AllPeriodOptions =>
    [
        new("day", "Day"),
        new("week", "Week"),
        new("month", "Month"),
        new("quarter", "Quarter", Disabled: DisableOption),
        new("year", "Year")
    ];

    private IReadOnlyList<OmniOption<string>> PeriodOptions => [.. AllPeriodOptions.Take(OptionCount)];

    /// <summary>
    /// A value that left the bar, or sits on a disabled option, goes back to the month, else to the first
    /// pickable option: otherwise no button would be checked.
    /// </summary>
    protected override void OnParametersSet()
    {
        var pickable = PeriodOptions.Where(option => !option.Disabled).ToList();
        if (pickable.Count > 0 && pickable.All(option => option.Value != Period))
        {
            Period = pickable.Any(option => option.Value == "month") ? "month" : pickable[0].Value;
        }
    }
}
