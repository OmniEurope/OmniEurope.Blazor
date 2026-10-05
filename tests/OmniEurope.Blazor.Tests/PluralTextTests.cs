using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-009 lot 12: a package text whose wording follows a number carries one form per plural category
/// of its language (Unicode CLDR), in the plural block of the ICU message syntax.
/// </summary>
public sealed class PluralTextTests : OmniBunitContext
{
    private static readonly string[] Languages =
    [
        "fr", "en", "bg", "cs", "da", "de", "el", "es", "et", "fi", "ga", "hr", "hu", "it", "lt", "lv", "mt", "nl",
        "pl", "pt", "ro", "sk", "sl", "sv"
    ];

    [Theory]
    // French counts zero with one; the others do not.
    [InlineData("fr", 0, "one")]
    [InlineData("fr", 1, "one")]
    [InlineData("fr", 2, "other")]
    [InlineData("fr", 1_000_000, "many")]
    [InlineData("en", 0, "other")]
    [InlineData("en", 1, "one")]
    [InlineData("de", 21, "other")]
    [InlineData("pt", 0, "other")]
    [InlineData("cs", 1, "one")]
    [InlineData("cs", 3, "few")]
    [InlineData("cs", 5, "other")]
    [InlineData("sk", 4, "few")]
    [InlineData("pl", 1, "one")]
    [InlineData("pl", 2, "few")]
    [InlineData("pl", 5, "many")]
    [InlineData("pl", 12, "many")]
    [InlineData("pl", 22, "few")]
    [InlineData("pl", 0, "many")]
    [InlineData("hr", 1, "one")]
    [InlineData("hr", 21, "one")]
    [InlineData("hr", 11, "other")]
    [InlineData("hr", 3, "few")]
    [InlineData("hr", 13, "other")]
    [InlineData("sl", 1, "one")]
    [InlineData("sl", 101, "one")]
    [InlineData("sl", 2, "two")]
    [InlineData("sl", 4, "few")]
    [InlineData("sl", 5, "other")]
    [InlineData("lt", 1, "one")]
    [InlineData("lt", 21, "one")]
    [InlineData("lt", 11, "other")]
    [InlineData("lt", 5, "few")]
    [InlineData("lt", 10, "other")]
    [InlineData("lv", 0, "zero")]
    [InlineData("lv", 10, "zero")]
    [InlineData("lv", 11, "zero")]
    [InlineData("lv", 1, "one")]
    [InlineData("lv", 21, "one")]
    [InlineData("lv", 31, "one")]
    [InlineData("lv", 5, "other")]
    [InlineData("ro", 1, "one")]
    [InlineData("ro", 0, "few")]
    [InlineData("ro", 19, "few")]
    [InlineData("ro", 20, "other")]
    [InlineData("ro", 101, "few")]
    [InlineData("ro", 120, "other")]
    [InlineData("ga", 1, "one")]
    [InlineData("ga", 2, "two")]
    [InlineData("ga", 6, "few")]
    [InlineData("ga", 7, "many")]
    [InlineData("ga", 11, "other")]
    [InlineData("mt", 1, "one")]
    [InlineData("mt", 2, "two")]
    [InlineData("mt", 0, "few")]
    [InlineData("mt", 10, "few")]
    [InlineData("mt", 11, "many")]
    [InlineData("mt", 19, "many")]
    [InlineData("mt", 20, "other")]
    [InlineData("es", 1, "one")]
    [InlineData("it", 1_000_000, "many")]
    [InlineData("pt", 2, "other")]
    [InlineData("cs", 0, "other")]
    [InlineData("lt", 102, "few")]
    [InlineData("ro", 100, "other")]
    [InlineData("ga", 0, "other")]
    [InlineData("mt", 102, "other")]
    // The sign is ignored, the smallest long included.
    [InlineData("en", -1, "one")]
    [InlineData("en", long.MinValue, "other")]
    // A language the package does not ship shows the French text, so it takes the French rule.
    [InlineData("ja", 0, "one")]
    public void Each_language_files_a_whole_number_in_its_cldr_category(string language, long count, string expected)
    {
        Assert.Equal(expected, PluralRules.Category(language, count));
    }

    [Fact]
    public void A_block_takes_the_form_of_its_number_and_writes_the_number_where_the_hash_stands()
    {
        const string text = "Limite : {0, plural, one {# ligne} other {# lignes}} au plus";

        Assert.Equal("Limite : 1 ligne au plus", PluralMessage.Format(text, "fr", CultureInfo.InvariantCulture, [1]));
        Assert.Equal("Limite : 12 lignes au plus", PluralMessage.Format(text, "fr", CultureInfo.InvariantCulture, [12]));
    }

    [Fact]
    public void A_text_holds_several_blocks_and_ordinary_placeholders_around_them()
    {
        const string text = "{0} : {1, plural, one {# line added} other {# lines added}}, {2, plural, one {# line removed} other {# lines removed}}";

        Assert.Equal("a.cs : 1 line added, 4 lines removed", PluralMessage.Format(text, "en", CultureInfo.InvariantCulture, ["a.cs", 1, 4]));
    }

    [Fact]
    public void An_exact_number_wins_over_the_rule_and_a_missing_category_falls_back_on_other()
    {
        const string text = "{0, plural, =0 {aucun fichier} one {# fichier} other {# fichiers}}";

        Assert.Equal("aucun fichier", PluralMessage.Format(text, "fr", CultureInfo.InvariantCulture, [0]));
        // A whole million is "many" in French, which this text does not carry.
        Assert.Equal("1000000 fichiers", PluralMessage.Format(text, "fr", CultureInfo.InvariantCulture, [1_000_000]));
    }

    [Fact]
    public void A_placeholder_inside_a_form_keeps_its_format_and_its_hash()
    {
        const string text = "{0, plural, one {{0:#,0} row} other {{0:#,0} rows}}";

        Assert.Equal("12,500 rows", PluralMessage.Format(text, "en", CultureInfo.InvariantCulture, [12_500]));
    }

    [Fact]
    public void A_counted_number_is_filed_by_its_value_and_written_in_its_own_format()
    {
        const string text = "{0, plural, one {# row} other {# rows}}";

        Assert.Equal("1 row", PluralMessage.Format(text, "en", CultureInfo.InvariantCulture, [new PluralCount(1, "N0")]));
        Assert.Equal("12,500 rows", PluralMessage.Format(text, "en", CultureInfo.InvariantCulture, [new PluralCount(12_500, "N0")]));
    }

    [Fact]
    public void A_text_without_a_block_is_an_ordinary_composite_format()
    {
        Assert.False(PluralMessage.HasBlock("Vybrané soubory: {0}."));
        Assert.Equal("Vybrané soubory: 3.", PluralMessage.Format("Vybrané soubory: {0}.", "cs", CultureInfo.InvariantCulture, [3]));
    }

    [Theory]
    [MemberData(nameof(LanguageData))]
    public void Every_plural_block_of_a_language_carries_exactly_its_categories(string language)
    {
        var required = PluralRules.Categories(language);
        var faulty = new List<string>();
        var blocks = 0;
        foreach (var (key, value) in Entries(language))
        {
            foreach (var block in PluralMessage.Blocks(value))
            {
                blocks++;
                var categories = block.Forms.Keys.Where(selector => !selector.StartsWith('=')).ToArray();
                // The whole-million "many" of the Romance languages is optional; nothing else is.
                var extra = categories.Except(required).Where(category => category != PluralRules.Many || language is not ("fr" or "es" or "it" or "pt"));
                if (required.Except(categories).Any() || extra.Any() || block.Forms.Values.Any(string.IsNullOrWhiteSpace))
                {
                    faulty.Add($"{key} [{string.Join(", ", categories)}]");
                }
            }
        }

        Assert.True(faulty.Count == 0, $"{language} expects [{string.Join(", ", required)}]: {string.Join("; ", faulty)}");
        // Hungarian keeps its nouns singular after a number and needs no block at all.
        Assert.Equal(language != "hu", blocks > 0);
    }

    [Theory]
    // The numbers the single plural form got wrong (docs/localization.md, review of 2026-09-29).
    [InlineData("ro", "LogViewerNewLines", 19, "19 rânduri noi")]
    [InlineData("ro", "LogViewerNewLines", 20, "20 de rânduri noi")]
    [InlineData("hr", "UploadMaximumFiles", 1, "Možete odabrati najviše 1 datoteku.")]
    [InlineData("hr", "UploadMaximumFiles", 21, "Možete odabrati najviše 21 datoteku.")]
    [InlineData("hr", "UploadMaximumFiles", 3, "Možete odabrati najviše 3 datoteke.")]
    [InlineData("hr", "UploadMaximumFiles", 5, "Možete odabrati najviše 5 datoteka.")]
    [InlineData("hr", "RelativeTimeFutureDays", 21, "21 dan")]
    [InlineData("lv", "RelativeTimeFutureDays", 21, "21 dienas")]
    [InlineData("lv", "RelativeTimeFutureDays", 31, "31 dienas")]
    [InlineData("lv", "RelativeTimeFutureDays", 12, "12 dienām")]
    [InlineData("pl", "RelativeTimeYears", 2, "2 lata")]
    [InlineData("pl", "RelativeTimeYears", 5, "5 lat")]
    [InlineData("sl", "RelativeTimeFutureDays", 2, "2 dneva")]
    [InlineData("en", "UploadHintMaximumFiles", 1, "Up to 1 file")]
    [InlineData("en", "UploadHintMaximumFiles", 3, "Up to 3 files")]
    [InlineData("fr", "UploadHintMaximumFiles", 1, "1 fichier au plus")]
    [InlineData("de", "LogViewerNewLines", 1, "1 neue Zeile")]
    [InlineData("mt", "RelativeTimeDays", 12, "12-il jum")]
    [InlineData("ga", "RelativeTimeYears", 8, "8 mbliana")]
    // A label form carries no block and reads the same for every number.
    [InlineData("cs", "LogViewerNewLines", 21, "Nové řádky: 21")]
    public void A_counted_text_agrees_with_its_number_in_its_language(string language, string key, int count, string expected)
    {
        var text = InCulture(language, () => PluralMessage.Localize(Services.GetRequiredService<IStringLocalizer<AppStrings>>(), key, [count]));

        Assert.Equal(expected, text);
    }

    [Fact]
    public void A_block_that_counts_the_second_argument_leaves_the_first_alone()
    {
        var text = InCulture("en", () => PluralMessage.Localize(
            Services.GetRequiredService<IStringLocalizer<AppStrings>>(), "DataAnnotationsMaxLength", ["Name", 1]));

        Assert.Equal("The Name field must be at most 1 character long.", text);
    }

    public static TheoryData<string> LanguageData() => new(Languages);

    private static string InCulture(string language, Func<string> read)
    {
        var (culture, ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            return read();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (culture, ui);
        }
    }

    private static IEnumerable<(string Key, string Value)> Entries(string language)
    {
        var name = language == "fr" ? "AppStrings.resx" : $"AppStrings.{language}.resx";
        var path = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "Resources", name);
        return XDocument.Load(path).Root!.Elements("data")
            .Select(data => ((string)data.Attribute("name")!, (string?)data.Element("value") ?? string.Empty));
    }

    [Fact]
    public void PluralCount_WritesItsNumberInItsFormat_UnlessAnotherIsAsked()
    {
        var count = new PluralCount(12_500, "N0");

        Assert.Equal("12,500", count.ToString(null, CultureInfo.InvariantCulture));
        Assert.Equal("12500.0", count.ToString("F1", CultureInfo.InvariantCulture));
        Assert.Equal(12_500.ToString("N0", CultureInfo.CurrentCulture), count.ToString());
    }
}
