using System.Globalization;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The placeholder of the date pickers spells the pattern of the culture with the letters of the interface
/// language. A short date pattern may hold a quoted literal (the Bulgarian one ends with the year mark,
/// <c>d.MM.yyyy 'г'.</c>): the quotes belong to the pattern, never to what the reader sees, and the literal
/// is shown as the date itself shows it.
/// </summary>
public sealed class PickerPlaceholderCultureTests
{
    private static readonly PickerLetters Letters = new("j", "m", "a", "h", "m", "s");

    public static TheoryData<string> ShippedCultures =>
    [
        "fr-FR", "en-IE", "bg-BG", "cs-CZ", "da-DK", "de-DE", "el-GR", "es-ES", "et-EE", "fi-FI", "ga-IE", "hr-HR",
        "hu-HU", "it-IT", "lt-LT", "lv-LV", "mt-MT", "nl-NL", "pl-PL", "pt-PT", "ro-RO", "sk-SK", "sl-SI", "sv-SE"
    ];

    [Theory]
    [MemberData(nameof(ShippedCultures))]
    public void Placeholder_ShowsThePatternAsADateShowsIt_WithoutTheQuotesOfALiteral(string name)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        var pattern = PickerFormat.DatePattern(culture);

        var placeholder = PickerFormat.Placeholder(pattern, Letters);

        Assert.DoesNotContain('\'', placeholder);
        // The same shape as a written date: each digit of the date stands where a letter of the placeholder does.
        var written = new DateOnly(2026, 11, 19).ToString(pattern, culture);
        Assert.Equal(written.Length, placeholder.Length);
        for (var index = 0; index < written.Length; index++)
        {
            if (!char.IsDigit(written[index]))
            {
                Assert.Equal(written[index], placeholder[index]);
            }
        }
    }

    [Fact]
    public void Placeholder_KeepsAQuotedLiteral_AndAnEscapedCharacter_AsText()
    {
        Assert.Equal("j.mm.aaaa г.", PickerFormat.Placeholder("d.MM.yyyy 'г'.", Letters));
        Assert.Equal("jj d mm", PickerFormat.Placeholder(@"dd \d MM", Letters));
        Assert.Equal("jj de mm", PickerFormat.Placeholder("dd 'de' MM", Letters));
    }
}
