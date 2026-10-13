using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMailRenderer"/>: values written as text or checked addresses, the look written on each
/// element, the layout in presentation tables, colours resolved from a theme of the package, and a
/// template that cannot carry markup or code into the mail.
/// </summary>
public sealed class MailRendererTests
{
    private static readonly HtmlParser Parser = new();

    private static IDocument Parse(OmniMailRenderResult result) => Parser.ParseDocument(result.Html);

    private static IElement ContentCell(IDocument document) =>
        document.QuerySelectorAll("td").Single(cell => cell.GetAttribute("style")?.Contains("padding:32px", StringComparison.Ordinal) == true);

    [Fact]
    public void Variables_AreReplacedAsText_NeverAsMarkup()
    {
        var result = OmniMailRenderer.Render(
            "<p>Bonjour {{client.name}}, votre dossier {{ file }} est prêt.</p>",
            new Dictionary<string, string?> { ["client.name"] = "<b>Anne</b> & Cie", ["file"] = "D-42" });

        var paragraph = ContentCell(Parse(result)).QuerySelector("p")!;
        Assert.Equal("Bonjour <b>Anne</b> & Cie, votre dossier D-42 est prêt.", paragraph.TextContent);
        Assert.Null(paragraph.QuerySelector("b"));
        Assert.Contains("&lt;b&gt;Anne&lt;/b&gt; &amp; Cie", result.Html, StringComparison.Ordinal);
        Assert.Empty(result.MissingVariables);
    }

    [Fact]
    public void MissingVariables_RenderNothing_AndAreListedOnce_InOrder()
    {
        var result = OmniMailRenderer.Render(
            "<p>{{b}} {{a}} {{b}} {{c}}</p>",
            new Dictionary<string, string?> { ["c"] = "trois", ["a"] = null },
            new OmniMailRenderOptions { Subject = "Objet {{d}}" });

        Assert.Equal(["d", "b", "a"], result.MissingVariables);
        Assert.Equal("trois", ContentCell(Parse(result)).QuerySelector("p")!.TextContent.Trim());
        Assert.Equal("Objet", result.Subject);
    }

    [Fact]
    public void FindVariables_ListsEachNameOnce_InOrder()
    {
        Assert.Equal(["client", "date", "x-y.z"], OmniMailRenderer.FindVariables("{{client}} {{ date }} {{client}} {{x-y.z}} {{1bad}} {single}"));
        Assert.Empty(OmniMailRenderer.FindVariables(null));
    }

    [Fact]
    public void ALinkAddress_TakesItsVariable_OnlyForHttpHttpsAndMailto()
    {
        var result = OmniMailRenderer.Render(
            "<p><a href=\"{{good}}\">ouvrir</a> <a href=\"{{bad}}\">piège</a> <a href=\"{{mail}}\">écrire</a></p>",
            new Dictionary<string, string?>
            {
                ["good"] = "https://exemple.eu/dossier?id=4&v=2",
                ["bad"] = "javascript:alert(1)",
                ["mail"] = "mailto:accueil@exemple.eu"
            });

        var links = ContentCell(Parse(result)).QuerySelectorAll("a");
        Assert.Equal("https://exemple.eu/dossier?id=4&v=2", links[0].GetAttribute("href"));
        Assert.Null(links[1].GetAttribute("href"));
        Assert.Equal("mailto:accueil@exemple.eu", links[2].GetAttribute("href"));
        Assert.DoesNotContain("javascript", result.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheTemplate_IsSanitised_BeforeRendering()
    {
        var result = OmniMailRenderer.Render(
            "<p onclick=\"x()\" style=\"color:red\">texte</p><script>alert(1)</script><img src=x onerror=y()><iframe src=\"https://x\"></iframe>");

        var cell = ContentCell(Parse(result));
        Assert.Empty(cell.QuerySelectorAll("script, img, iframe"));
        Assert.Null(cell.QuerySelector("p")!.GetAttribute("onclick"));
        Assert.DoesNotContain("color:red", result.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void EachElement_CarriesItsLook_AndTheEditorClassesBecomeDeclarations()
    {
        var theme = OmniMailTheme.Default with { Link = "#123456", Border = "#abcdef" };
        var result = OmniMailRenderer.Render(
            "<h1 class=\"omni-align-center\">Titre</h1><p class=\"omni-font-size-large\"><a href=\"https://exemple.eu\">lien</a></p>"
            + "<table><tbody><tr><th>A</th></tr><tr><td>1</td></tr></tbody></table>",
            options: new OmniMailRenderOptions { Theme = theme });

        var cell = ContentCell(Parse(result));
        Assert.Empty(cell.QuerySelectorAll("[class]"));
        Assert.Contains("text-align:center", cell.QuerySelector("h1")!.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("font-size:28px", cell.QuerySelector("h1")!.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("font-size:20px", cell.QuerySelector("p")!.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("color:#123456", cell.QuerySelector("a")!.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("border:1px solid #abcdef", cell.QuerySelector("td")!.GetAttribute("style"), StringComparison.Ordinal);
        var table = cell.QuerySelector("table")!;
        Assert.Equal("0", table.GetAttribute("cellpadding"));
        Assert.Contains("border-collapse:collapse", table.GetAttribute("style"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheLayout_IsPresentationTables_WithAFixedWidthForOutlook()
    {
        var result = OmniMailRenderer.Render("<p>x</p>", options: new OmniMailRenderOptions { Width = 640, Language = "fr-BE", Subject = "Votre <dossier>", Preheader = "Aperçu {{n}}" });
        var document = Parse(result);

        Assert.StartsWith("<!DOCTYPE html>", result.Html, StringComparison.Ordinal);
        Assert.Equal("fr-BE", document.DocumentElement.GetAttribute("lang"));
        Assert.Equal("Votre <dossier>", document.Title);
        Assert.Contains("<!--[if mso]><table role=\"presentation\" width=\"640\"", result.Html, StringComparison.Ordinal);
        Assert.Contains("max-width:640px", result.Html, StringComparison.Ordinal);
        var layoutTables = document.Body!.QuerySelectorAll("table[role=presentation]");
        Assert.Equal(2, layoutTables.Length);
        Assert.All(layoutTables, table => Assert.Equal("0", table.GetAttribute("cellspacing")));
        Assert.Equal("Aperçu", document.Body.FirstElementChild!.TextContent);
        Assert.Contains("display:none", document.Body.FirstElementChild.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Empty(document.QuerySelectorAll("style, link, script"));
    }

    [Fact]
    public void TheTextAlternative_KeepsTheTextAndEachLinkAddress()
    {
        var result = OmniMailRenderer.Render(
            "<h2>Rappel</h2><p>Voir <a href=\"https://exemple.eu/d\">le dossier</a> de {{who}}.</p><ul><li>un</li></ul>",
            new Dictionary<string, string?> { ["who"] = "Anne" });

        Assert.Equal("Rappel\n\nVoir le dossier (https://exemple.eu/d) de Anne.\n\n- un", result.Text);
    }

    [Fact]
    public void ThemeFromAPalette_TakesItsTokens()
    {
        var palette = OmniThemePalettes.All.First(item => item.Name == "Océan");
        var theme = OmniMailTheme.From(palette);

        Assert.Equal(palette.Light["--omni-color-surface"], theme.Surface);
        Assert.Equal(palette.Light["--omni-color-text"], theme.Text);
        Assert.Equal(palette.Light["--omni-color-accent-strong"], theme.Link);
        Assert.False(theme.Dark);

        var dark = OmniMailTheme.From(palette, OmniAppearance.Dark);
        Assert.Equal(palette.Dark["--omni-color-surface"], dark.Surface);
        Assert.True(dark.Dark);
        var result = OmniMailRenderer.Render("<p>x</p>", options: new OmniMailRenderOptions { Theme = dark });
        Assert.Contains($"background-color:{dark.Surface}", result.Html, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"color-scheme\" content=\"dark\">", result.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemeFromAPreset_TakesItsFontAndRadius_WithMailSafeFonts()
    {
        var preset = OmniThemePresets.All[0];
        var theme = OmniMailTheme.From(preset);

        Assert.EndsWith("sans-serif", theme.FontFamily, StringComparison.Ordinal);
        Assert.DoesNotContain("\"", theme.FontFamily, StringComparison.Ordinal);
        Assert.EndsWith("px", theme.Radius, StringComparison.Ordinal);
    }

    [Fact]
    public void FromTokens_KeepsTheDefault_ForATokenThatIsNotALiteralValue()
    {
        var theme = OmniMailTheme.FromTokens(new Dictionary<string, string>
        {
            ["--omni-color-text"] = "var(--x)",
            ["--omni-color-border"] = "color-mix(in srgb, red, blue)",
            ["--omni-color-surface"] = "#010203",
            ["--omni-radius"] = "0.5rem",
            ["--omni-font-family"] = "x;}</style>",
        });

        Assert.Equal("#010203", theme.Surface);
        Assert.Equal(new OmniMailTheme().Text, theme.Text);
        Assert.Equal(new OmniMailTheme().Border, theme.Border);
        Assert.Equal("8px", theme.Radius);
        Assert.Equal(new OmniMailTheme().FontFamily, theme.FontFamily);
    }

    [Theory]
    [InlineData("red;background:url(x)")]
    [InlineData("#fff\" onload=\"x")]
    [InlineData("expression(alert(1))")]
    public void AThemeSetByHand_RefusesAValueThatIsNotALiteralColour(string colour)
    {
        var options = new OmniMailRenderOptions { Theme = new OmniMailTheme { Text = colour } };

        Assert.Throws<ArgumentException>(() => OmniMailRenderer.Render("<p>x</p>", options: options));
    }

    [Fact]
    public void EveryElementAndEditorClass_GetsItsDeclarations()
    {
        var theme = OmniMailTheme.Default with { Accent = "#aa0000", MutedText = "#555555", Highlight = "#ffee00", CodeBackground = "#eeeeee", Border = "#999999" };
        var result = OmniMailRenderer.Render(
            "<div>d</div><h3>t3</h3><h4>t4</h4><h5>t5</h5><h6>t6</h6><ol><li>o</li></ol><blockquote>q</blockquote><pre>p</pre><p><code>c</code><mark>m</mark></p><hr>"
            + "<table><caption>cap</caption><tbody><tr><td>1</td></tr></tbody></table>"
            + "<p class=\"omni-align-left\">l</p><p class=\"omni-align-end\">e</p><p class=\"omni-align-justify\">j</p>"
            + "<p class=\"omni-font-size-small\">s</p><p class=\"omni-font-size-normal\">n</p><p class=\"omni-font-size-xlarge\">x</p>",
            options: new OmniMailRenderOptions { Theme = theme });
        var cell = ContentCell(Parse(result));
        string Look(string selector) => cell.QuerySelector(selector)!.GetAttribute("style")!;
        string LookOf(string text) => cell.QuerySelectorAll("p").Single(paragraph => paragraph.TextContent == text).GetAttribute("style")!;

        Assert.Contains("font-size:18px", Look("h3"), StringComparison.Ordinal);
        Assert.Contains("font-size:16px", Look("h4"), StringComparison.Ordinal);
        Assert.Contains("font-weight:700", Look("h5"), StringComparison.Ordinal);
        Assert.Contains("font-weight:700", Look("h6"), StringComparison.Ordinal);
        Assert.Contains("margin:0 0 16px 0", Look("div"), StringComparison.Ordinal);
        Assert.Contains("padding:0 0 0 24px", Look("ol"), StringComparison.Ordinal);
        Assert.Contains("border-left:3px solid #aa0000", Look("blockquote"), StringComparison.Ordinal);
        Assert.Contains("white-space:pre-wrap", Look("pre"), StringComparison.Ordinal);
        Assert.Contains("background-color:#eeeeee", Look("code"), StringComparison.Ordinal);
        Assert.Contains("background-color:#ffee00", Look("mark"), StringComparison.Ordinal);
        Assert.Contains("border-top:1px solid #999999", Look("hr"), StringComparison.Ordinal);
        Assert.Contains("color:#555555", Look("caption"), StringComparison.Ordinal);
        Assert.Contains("text-align:left", LookOf("l"), StringComparison.Ordinal);
        Assert.Contains("text-align:right", LookOf("e"), StringComparison.Ordinal);
        Assert.Contains("text-align:justify", LookOf("j"), StringComparison.Ordinal);
        Assert.Contains("font-size:14px", LookOf("s"), StringComparison.Ordinal);
        Assert.Contains("font-size:16px", LookOf("n"), StringComparison.Ordinal);
        Assert.Contains("font-size:26px", LookOf("x"), StringComparison.Ordinal);
        // An element the mail does not restyle (strong) keeps no style attribute.
        Assert.Null(Parse(OmniMailRenderer.Render("<p><strong>b</strong></p>")).QuerySelector("strong")!.GetAttribute("style"));
    }

    [Fact]
    public void ALinkWithoutAVariable_IsKept_AndARelativeAddressMadeOfAVariable_IsDropped()
    {
        var result = OmniMailRenderer.Render(
            "<p><a href=\"https://exemple.eu/a%20b\">fixe</a> <a href=\"{{path}}\">relatif</a></p>",
            new Dictionary<string, string?> { ["path"] = "dossiers/42" });

        var links = ContentCell(Parse(result)).QuerySelectorAll("a");
        Assert.Equal("https://exemple.eu/a%20b", links[0].GetAttribute("href"));
        Assert.Null(links[1].GetAttribute("href"));
    }

    [Fact]
    public void AnInvariantCulture_WritesEnglishAsTheLanguage()
    {
        var result = OmniMailRenderer.Render("<p>x</p>", options: new OmniMailRenderOptions { Language = string.Empty });

        Assert.Equal("en", Parse(result).DocumentElement.GetAttribute("lang"));
    }

    [Theory]
    [InlineData("Georgia", "Georgia, Arial, Helvetica, sans-serif")]
    [InlineData("\"Source Serif 4\", serif", "'Source Serif 4', serif")]
    public void TheThemeFont_EndsWithFontsEveryClientHas(string family, string expected) =>
        Assert.Equal(expected, OmniMailTheme.FromTokens(new Dictionary<string, string> { ["--omni-font-family"] = family }).FontFamily);

    [Theory]
    [InlineData("6px", "6px")]
    [InlineData("1em", "16px")]
    [InlineData("0", "0px")]
    [InlineData("12", "8px")]
    [InlineData("large", "8px")]
    public void TheThemeRadius_IsWholePixels_OrTheDefault(string token, string expected) =>
        Assert.Equal(expected, OmniMailTheme.FromTokens(new Dictionary<string, string> { ["--omni-card-radius"] = token }).Radius);

    [Fact]
    public void AThemeSetByHand_RefusesAFontOrARadiusThatIsNotALiteralValue()
    {
        Assert.Throws<ArgumentException>(() => OmniMailRenderer.Render("<p>x</p>",
            options: new OmniMailRenderOptions { Theme = new OmniMailTheme { FontFamily = "x;}" } }));
        Assert.Throws<ArgumentException>(() => OmniMailRenderer.Render("<p>x</p>",
            options: new OmniMailRenderOptions { Theme = new OmniMailTheme { Radius = "calc(1px)" } }));
    }

    [Theory]
    [InlineData(319)]
    [InlineData(1201)]
    public void AWidthOutsideTheRange_IsRefused(int width) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OmniMailRenderer.Render("<p>x</p>", options: new OmniMailRenderOptions { Width = width }));
}
