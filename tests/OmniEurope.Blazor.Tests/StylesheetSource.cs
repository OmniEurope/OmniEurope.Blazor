namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The library stylesheet is written as ordered parts under <c>src/OmniEurope.Blazor/Styles</c>. The
/// build joins them in ordinal file-name order, which is the cascade order, into the one
/// <c>omnieurope.blazor.css</c> a host downloads (<c>eng/OmniEurope.Stylesheet.targets</c>). Tests
/// that read the source read the same joined text here.
/// </summary>
internal static class StylesheetSource
{
    /// <summary>The folder that holds the parts.</summary>
    internal static string PartsDirectory =>
        Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "Styles");

    /// <summary>The parts, in the order the build joins them.</summary>
    internal static IReadOnlyList<string> Parts()
    {
        var parts = Directory.GetFiles(PartsDirectory, "*.css")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(parts);
        return parts;
    }

    /// <summary>The joined source, as the build writes it before minifying.</summary>
    internal static string Read() => string.Concat(Parts().Select(File.ReadAllText));

    /// <summary>The single part whose text contains <paramref name="marker"/>.</summary>
    internal static string PartContaining(string marker) =>
        Assert.Single(Parts(), part => File.ReadAllText(part).Contains(marker, StringComparison.Ordinal));
}
