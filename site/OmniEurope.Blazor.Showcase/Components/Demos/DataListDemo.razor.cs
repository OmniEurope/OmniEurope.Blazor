namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DataListDemo
{
    private static readonly IReadOnlyList<int> Sizes = [2, 4, 8];

    private static readonly IReadOnlyList<GridRow> Rows =
    [
        new("D-2401", "Camille Durand", "Belgique", 12400),
        new("D-2402", "Jonas Meyer", "Luxembourg", 8600),
        new("D-2403", "Sofia Rossi", "France", 21500),
        new("D-2404", "Lars Jansen", "Pays-Bas", 4300),
        new("D-2405", "Ana Silva", "France", 17800),
        new("D-2406", "Piet de Vries", "Pays-Bas", 9100)
    ];

    private int PageIndex { get; set; }

    private int PageSize { get; set; } = 2;

    private int PageCount => (int)Math.Ceiling(Rows.Count / (double)PageSize);

    private IReadOnlyList<GridRow> Page => [.. Rows.Skip(PageIndex * PageSize).Take(PageSize)];
}
