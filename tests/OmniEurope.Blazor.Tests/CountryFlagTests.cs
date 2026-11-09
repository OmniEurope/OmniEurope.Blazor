using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniCountryFlag"/> draws no picture unless the host gives a template, names itself by the
/// country in the current UI culture and falls back to the code when the code is not a known country.
/// </summary>
public sealed class CountryFlagTests : OmniBunitContext
{
    private const string Template = "https://flags.example/{code}/{CODE}.svg";

    [Fact]
    public void A_valid_code_with_a_template_renders_the_substituted_image_and_the_code()
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, " fr ").Add(c => c.ImageUrlTemplate, Template));

        var image = flag.Find("img");
        Assert.Equal("https://flags.example/fr/FR.svg", image.GetAttribute("src"));
        Assert.Equal(string.Empty, image.GetAttribute("alt"));
        Assert.Equal("true", image.GetAttribute("aria-hidden"));
        Assert.Equal("no-referrer", image.GetAttribute("referrerpolicy"));
        Assert.Equal("lazy", image.GetAttribute("loading"));
        Assert.Equal("FR", flag.Find(".omni-country-flag__code").TextContent);
    }

    [Theory]
    [InlineData("fr-FR", "FR", "France")]
    [InlineData("fr-FR", "DE", "Allemagne")]
    [InlineData("en-US", "DE", "Germany")]
    public void The_name_is_the_country_in_the_current_ui_culture(string culture, string code, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, code).Add(c => c.ImageUrlTemplate, Template));

            var root = flag.Find(".omni-country-flag");
            Assert.Equal("img", root.GetAttribute("role"));
            Assert.Equal(expected, root.GetAttribute("aria-label"));
            Assert.Equal(expected, root.GetAttribute("title"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("France")]
    [InlineData("F1")]
    [InlineData("ZZ")]
    public void An_invalid_code_renders_the_code_only(string code)
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, code).Add(c => c.ImageUrlTemplate, Template));

        Assert.Empty(flag.FindAll("img"));
        Assert.Equal(code.ToUpperInvariant(), flag.Find(".omni-country-flag__code").TextContent);
        Assert.Equal(code.ToUpperInvariant(), flag.Find(".omni-country-flag").GetAttribute("aria-label"));
    }

    [Fact]
    public void Without_a_template_there_is_no_image_and_the_code_is_a_badge()
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, "FR"));

        Assert.Empty(flag.FindAll("img"));
        Assert.Contains("omni-country-flag--badge", flag.Find(".omni-country-flag").ClassName);
        Assert.Equal("FR", flag.Find(".omni-country-flag__code").TextContent);
    }

    [Fact]
    public void ShowCode_false_hides_the_code_beside_the_image()
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, "FR").Add(c => c.ImageUrlTemplate, Template).Add(c => c.ShowCode, false));

        Assert.NotEmpty(flag.FindAll("img"));
        Assert.Empty(flag.FindAll(".omni-country-flag__code"));
    }

    [Theory]
    [InlineData("javascript:alert({code})")]
    [InlineData("//evil.example/{code}.svg")]
    public void A_template_that_is_not_an_http_or_rooted_address_draws_no_image(string template)
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, "FR").Add(c => c.ImageUrlTemplate, template));

        Assert.Empty(flag.FindAll("img"));
    }

    [Fact]
    public void ShowCode_false_without_a_picture_still_shows_the_code()
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, "FR").Add(c => c.ShowCode, false));

        Assert.Empty(flag.FindAll("img"));
        Assert.Equal("FR", flag.Find(".omni-country-flag__code").TextContent);
    }

    [Fact]
    public void A_code_of_two_non_ascii_letters_is_not_a_country()
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, "éé").Add(c => c.ImageUrlTemplate, Template));

        Assert.Empty(flag.FindAll("img"));
        Assert.Equal("ÉÉ", flag.Find(".omni-country-flag").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("F1")]
    [InlineData("ZZ")]
    [InlineData("ÉÉ")]
    public void RegionOf_is_null_unless_the_code_is_a_known_two_letter_region(string? code)
    {
        Assert.Null(OmniCountryFlag.RegionOf(code));
    }

    [Fact]
    public void RegionOf_returns_the_region_of_a_known_code()
    {
        Assert.Equal("FR", OmniCountryFlag.RegionOf("FR")!.TwoLetterISORegionName);
    }

    [Theory]
    [InlineData("fr", "fr-DE")]
    [InlineData("en", "en-DE")]
    [InlineData("iv", "en-DE")]
    [InlineData("tlh", "en-DE")]
    [InlineData("", "en-DE")]
    public void NameOf_asks_for_the_culture_of_the_page_language_and_defaults_to_english(string language, string expectedCulture)
    {
        string? requested = null;

        OmniCountryFlag.NameOf("DE", "Deutschland", language, name =>
        {
            requested = name;
            throw new CultureNotFoundException();
        });

        Assert.Equal(expectedCulture, requested);
    }

    [Fact]
    public void NameOf_takes_the_country_from_the_parentheses_of_the_culture_name()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.Equal("Germany", OmniCountryFlag.NameOf("DE", "Deutschland", "fr", CultureInfo.GetCultureInfo));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
    [Fact]
    public void NameOf_uses_the_region_own_name_when_the_culture_does_not_exist()
    {
        Assert.Equal("Deutschland", OmniCountryFlag.NameOf("DE", "Deutschland", "en", _ => throw new CultureNotFoundException()));
    }

    [Fact]
    public void NameOf_uses_the_region_own_name_when_the_culture_name_has_no_country_in_parentheses()
    {
        Assert.Equal("Deutschland", OmniCountryFlag.NameOf("DE", "Deutschland", "en", _ => CultureInfo.GetCultureInfo("de")));
    }

    [Fact]
    public void NameOf_is_null_when_nothing_names_the_country()
    {
        Assert.Null(OmniCountryFlag.NameOf("DE", " ", "en", _ => CultureInfo.GetCultureInfo("de")));
    }

    [Fact]
    public void A_blank_code_draws_nothing()
    {
        var flag = Render<OmniCountryFlag>(p => p.Add(c => c.CountryCode, "  "));

        Assert.Empty(flag.FindAll(".omni-country-flag"));
    }
}
