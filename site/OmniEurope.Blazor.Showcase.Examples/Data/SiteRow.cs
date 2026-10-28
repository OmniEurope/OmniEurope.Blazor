namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>One row of the grids whose columns are sized to their content.</summary>
/// <param name="Name">The site name.</param>
/// <param name="Online">Whether the site answers.</param>
/// <param name="Visitors">The visitors on the site now.</param>
/// <param name="Project">The project the site belongs to.</param>
/// <param name="Since">When the site went live.</param>
public sealed record SiteRow(string Name, bool Online, int Visitors, string Project, DateOnly Since);

/// <summary>One row of the grid that fits its columns to long names and cuts a long description.</summary>
/// <param name="Name">The package name, a long value without a break that the column must show whole.</param>
/// <param name="Description">A long sentence that its column cuts with an ellipsis (Truncate).</param>
public sealed record PackageRow(string Name, string Description);

/// <summary>The sample sites and packages of the column sizing examples.</summary>
public static class SiteData
{
    /// <summary>The sentence far longer than its column.</summary>
    public const string PackageDescription =
        "A long description that does not fit its column and ends with an ellipsis, while the whole text stays readable in the tooltip.";

    /// <summary>Names and projects of very different lengths, a status for a badge, a number and a date.</summary>
    public static IReadOnlyList<SiteRow> Sites { get; } =
    [
        new("Online shop", true, 128, "Retail showcase", new DateOnly(2024, 3, 12)),
        new("Technical documentation of the platform", true, 37, "Continuous integration", new DateOnly(2023, 9, 4)),
        new("Intranet", false, 0, "Human resources", new DateOnly(2022, 1, 17)),
        new("Partner extranet", true, 9, "Partner relations", new DateOnly(2025, 6, 30)),
        new("Blog", true, 54, "Communication", new DateOnly(2021, 11, 8))
    ];

    /// <summary>Packages with long names and a description far longer than its column.</summary>
    public static IReadOnlyList<PackageRow> Packages { get; } =
    [
        new("@example-org/web-analytics", PackageDescription),
        new("@example-org/design-tokens", PackageDescription),
        new("@example-org/ui", PackageDescription)
    ];
}
