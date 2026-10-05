using System.Globalization;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Localization;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The plural reader at its edges: every kind of number it counts, the forms it falls back on, the
/// blocks it refuses to read, and the localizer path that leaves a text without block to the localizer.
/// </summary>
public sealed class PluralMessageTests
{
    private const string Files = "{0, plural, =0 {aucun fichier} one {# fichier} other {# fichiers}}";

    private static string Fr(string text, params object[] arguments) => PluralMessage.Format(text, "fr", CultureInfo.InvariantCulture, arguments);

    public static TheoryData<object, string> Counts => new()
    {
        { (sbyte)1, "1 fichier" },
        { (byte)2, "2 fichiers" },
        { (short)1, "1 fichier" },
        { (ushort)3, "3 fichiers" },
        { 1, "1 fichier" },
        { 4u, "4 fichiers" },
        { 1L, "1 fichier" },
        { 1UL, "1 fichier" },
        { ulong.MaxValue, "18446744073709551615 fichiers" },
        { 1d, "1 fichier" },
        { 1.5d, "1.5 fichiers" },
        { 1e300, "1E+300 fichiers" },
        { double.NaN, "NaN fichiers" },
        { 1m, "1 fichier" },
        { 1.5m, "1.5 fichiers" },
        { 79228162514264337593543950335m, "79228162514264337593543950335 fichiers" },
        { 1f, "1 fichiers" },
        { "1", "1 fichiers" },
        { 0, "aucun fichier" },
    };

    [Theory]
    [MemberData(nameof(Counts))]
    public void EveryWholeNumber_IsCounted_AndAnythingElseTakesOther(object count, string expected) =>
        Assert.Equal(expected, Fr(Files, count));

    [Fact]
    public void BlockNamingAMissingArgument_IsAFormatError()
    {
        var error = Assert.Throws<FormatException>(() => Fr("{1, plural, other {#}}", 3));
        Assert.Contains("argument 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BlockWithoutOther_FailsForANumberItsFormsMiss()
    {
        Assert.Equal("un", Fr("{0, plural, one {un}}", 1));
        Assert.Throws<FormatException>(() => Fr("{0, plural, one {un}}", 5));
        Assert.Throws<FormatException>(() => Fr("{0, plural, one {un}}", "texte"));
    }

    [Fact]
    public void SpacesAroundEveryPart_AreRead()
    {
        Assert.Equal("2 pommes", Fr("{ 0 ,  plural ,  one  {# pomme}  other  {# pommes}  }", 2));
    }

    [Fact]
    public void NestedPlaceholders_AndHashesInsideThem_KeepTheirMeaning()
    {
        Assert.Equal("2 fichiers de b", Fr("{0, plural, other {# fichiers de {1}}}", 2, "b"));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{0")]
    [InlineData("{x, plural, other {a}}")]
    [InlineData("{0 plural, other {a}}")]
    [InlineData("{0, select, other {a}}")]
    [InlineData("{0, plur")]
    [InlineData("{0, plural other {a}}")]
    [InlineData("{0, plural,")]
    [InlineData("{0, plural, other")]
    [InlineData("{0, plural, other x")]
    [InlineData("{0, plural, {a}}")]
    [InlineData("{0, plural, other {a")]
    [InlineData("{0, plural, }")]
    [InlineData("{99999999999, plural, other {a}}")]
    public void MalformedBlock_IsNoBlock(string text)
    {
        Assert.False(PluralMessage.HasBlock(text));
    }

    [Fact]
    public void Localize_LeavesATextWithoutBlockOrArgumentsToTheLocalizer()
    {
        var localizer = new Texts(new Dictionary<string, string>
        {
            ["Plain"] = "Bonjour {0}",
            ["Counted"] = Files,
            ["Fixed"] = "Fermer"
        });
        var culture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        try
        {
            Assert.Equal("Fermer", PluralMessage.Localize(localizer, "Fixed", []));
            Assert.Equal("Bonjour Ana", PluralMessage.Localize(localizer, "Plain", ["Ana"]));
            Assert.Equal("2 fichiers", PluralMessage.Localize(localizer, "Counted", [2]));
            Assert.Equal("Absent", PluralMessage.Localize(localizer, "Absent", [2]));
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }

    private sealed class Texts(IReadOnlyDictionary<string, string> values) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            values.TryGetValue(name, out var value) ? new(name, value) : new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.InvariantCulture, this[name].Value, arguments), this[name].ResourceNotFound);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
