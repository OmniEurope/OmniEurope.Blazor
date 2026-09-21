using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The stylesheet has three breakpoints, the ones its columns are sized at: 40rem (sm), 64rem (md)
/// and 80rem (lg), each written as min-width N or max-width N-0.01rem. A host follows the same
/// three (Aetheus recette R-007); a component that needs another threshold uses one of these.
/// </summary>
public sealed class BreakpointAuditTests
{
    private static readonly HashSet<string> Allowed =
    [
        "min-width: 40rem", "max-width: 39.99rem",
        "min-width: 64rem", "max-width: 63.99rem",
        "min-width: 80rem", "max-width: 79.99rem"
    ];

    [Fact]
    public void WidthQueries_UseOnlyTheThreeBreakpoints()
    {
        var css = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));
        var violations = Regex.Matches(css, @"@media[^{]*")
            .SelectMany(media => Regex.Matches(media.Value, @"(?:min|max)-width\s*:\s*[0-9.]+(?:px|rem|em)"))
            .Select(match => Regex.Replace(match.Value, @"\s*:\s*", ": "))
            .Where(query => !Allowed.Contains(query))
            .ToList();

        Assert.True(violations.Count == 0, "Width queries outside 40, 64 and 80rem: " + string.Join(", ", violations));
    }
}
