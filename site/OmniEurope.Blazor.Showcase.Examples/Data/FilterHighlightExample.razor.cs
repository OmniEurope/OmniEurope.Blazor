namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A list narrowed by a filter box, the typed text marked in every name that stays.</summary>
public partial class FilterHighlightExample
{
    private static readonly string[] Cities = ["Brussels", "Antwerp", "Bruges", "Ghent", "Liège", "Namur", "Luxembourg", "Mechelen"];

    private string Filter { get; set; } = "br";

    private bool MatchCase { get; set; }

    private IEnumerable<string> Visible => Cities.Where(city => string.IsNullOrWhiteSpace(Filter)
        || city.Contains(Filter.Trim(), MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase));
}
