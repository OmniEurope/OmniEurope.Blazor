namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>One line of the income statement shown as a tree grid: a category, a sub-total or an account.</summary>
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

    /// <summary>The sample income statement.</summary>
    public static IReadOnlyList<StatementLine> Sample { get; } =
    [
        new("P", "Income", 0, 412000, 455500,
        [
            new("P70", "Sales", 1, 398000, 441000,
            [
                new("706", "706 Services", 2, 310000, 352000),
                new("707", "707 Goods", 2, 88000, 89000)
            ]),
            new("758", "758 Income", 1, 14000, 14500)
        ]),
        new("C", "Expenses", 0, 351000, 372800,
        [
            new("C60", "Purchases", 1, 121000, 118300,
            [
                new("607", "607 Goods", 2, 121000, 118300)
            ]),
            new("C64", "Staff", 1, 230000, 254500,
            [
                new("641", "641 Staff", 2, 230000, 254500)
            ])
        ])
    ];
}
