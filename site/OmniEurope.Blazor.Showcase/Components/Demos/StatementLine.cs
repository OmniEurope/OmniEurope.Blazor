namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// One line of the income statement shown as a tree grid: a category, a sub-total or an account.
/// </summary>
/// <param name="Id">The stable key of the line.</param>
/// <param name="Label">The displayed label.</param>
/// <param name="Level">Its depth, 0 for a category.</param>
/// <param name="Previous">The amount of the previous year.</param>
/// <param name="Current">The amount of the current year.</param>
/// <param name="Children">The lines under it, none for an account.</param>
public sealed record StatementLine(string Id, string Label, int Level, decimal Previous, decimal Current, IReadOnlyList<StatementLine>? Children = null)
{
    /// <summary>The change from the previous year, as a fraction of it.</summary>
    public decimal Variation => Previous == 0 ? 0 : (Current - Previous) / Previous;
}
