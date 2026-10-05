using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>The name of an exported file: a slug of the title, then the moment it was generated.</summary>
public sealed class ExportFileNameTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    [InlineData("Journaux de la boutique : étape 3", "journaux-de-la-boutique-etape-3")]
    [InlineData("  -- Bilan 2026 !", "bilan-2026")]
    [InlineData("Œuvre", "uvre")]
    public void Slug_KeepsLettersAndDigits_JoinedByOneHyphen(string? text, string slug) =>
        Assert.Equal(slug, ExportFileName.Slug(text));

    [Theory]
    [InlineData(null, "export-2026-10-04-0930.csv")]
    [InlineData(" ", "export-2026-10-04-0930.csv")]
    [InlineData(" bilan ", "bilan-2026-10-04-0930.csv")]
    public void Stamp_NamesTheFileAfterItsSlugAndItsUtcMoment(string? name, string file) =>
        Assert.Equal(file, ExportFileName.Stamp(name, new DateTimeOffset(2026, 10, 4, 11, 30, 0, TimeSpan.FromHours(2)), "csv"));
}
