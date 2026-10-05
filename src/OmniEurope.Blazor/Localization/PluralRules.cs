namespace OmniEurope.Blazor.Localization;

/// <summary>
/// The cardinal plural category of a whole number in each of the 24 languages the package ships, as the
/// Unicode CLDR defines them (<c>zero</c>, <c>one</c>, <c>two</c>, <c>few</c>, <c>many</c>, <c>other</c>).
/// </summary>
/// <remarks>
/// Whole numbers only: the package counts files, rows, characters and days. A language the package does
/// not ship takes the French rule, because the text it then shows is the neutral French resource.
/// </remarks>
internal static class PluralRules
{
    internal const string Zero = "zero";
    internal const string One = "one";
    internal const string Two = "two";
    internal const string Few = "few";
    internal const string Many = "many";
    internal const string Other = "other";

    /// <summary>
    /// The categories a whole number can fall in, by language: what a plural text of that language must
    /// carry. The <c>many</c> of the Romance languages (whole millions) is left out: a text without it
    /// falls back to <c>other</c>.
    /// </summary>
    internal static IReadOnlyList<string> Categories(string language) => Normalise(language) switch
    {
        "cs" or "sk" or "hr" or "lt" or "ro" => [One, Few, Other],
        "pl" => [One, Few, Many, Other],
        "sl" => [One, Two, Few, Other],
        "lv" => [Zero, One, Other],
        "ga" or "mt" => [One, Two, Few, Many, Other],
        _ => [One, Other]
    };

    /// <summary>The category of <paramref name="count"/> in <paramref name="language"/> (a two-letter code); the sign is ignored.</summary>
    internal static string Category(string language, long count)
    {
        var n = count == long.MinValue ? long.MaxValue : Math.Abs(count);
        var tens = n % 10;
        var hundreds = n % 100;
        return Normalise(language) switch
        {
            "fr" => n <= 1 ? One : Million(n),
            "es" or "it" or "pt" => n == 1 ? One : Million(n),
            "cs" or "sk" => n == 1 ? One : n is >= 2 and <= 4 ? Few : Other,
            "pl" => n == 1 ? One
                : tens is >= 2 and <= 4 && hundreds is not (>= 12 and <= 14) ? Few
                : Many,
            "hr" => tens == 1 && hundreds != 11 ? One
                : tens is >= 2 and <= 4 && hundreds is not (>= 12 and <= 14) ? Few
                : Other,
            "sl" => hundreds == 1 ? One : hundreds == 2 ? Two : hundreds is 3 or 4 ? Few : Other,
            "lt" => hundreds is >= 11 and <= 19 ? Other : tens == 1 ? One : tens >= 2 ? Few : Other,
            "lv" => tens == 0 || hundreds is >= 11 and <= 19 ? Zero : tens == 1 ? One : Other,
            "ro" => n == 1 ? One : n == 0 || hundreds is >= 1 and <= 19 ? Few : Other,
            "ga" => n == 1 ? One : n == 2 ? Two : n is >= 3 and <= 6 ? Few : n is >= 7 and <= 10 ? Many : Other,
            "mt" => n == 1 ? One : n == 2 ? Two : n == 0 || hundreds is >= 3 and <= 10 ? Few : hundreds is >= 11 and <= 19 ? Many : Other,
            _ => n == 1 ? One : Other
        };
    }

    /// <summary>Whole millions take <c>many</c> in the Romance languages ("1 000 000 de lignes").</summary>
    private static string Million(long n) => n != 0 && n % 1_000_000 == 0 ? Many : Other;

    private static string Normalise(string language) => Shipped.Contains(language) ? language : "fr";

    private static readonly HashSet<string> Shipped = new(StringComparer.Ordinal)
    {
        "fr", "en", "bg", "cs", "da", "de", "el", "es", "et", "fi", "ga", "hr", "hu", "it", "lt", "lv", "mt", "nl",
        "pl", "pt", "ro", "sk", "sl", "sv"
    };
}
