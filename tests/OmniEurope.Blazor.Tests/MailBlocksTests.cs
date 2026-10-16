using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The blocks of <see cref="OmniMailRenderer"/>: the action button (<c>{{#button name}}</c>), the banner
/// (<c>{{#banner tone}}</c>) and the block variables of <see cref="OmniMailRenderOptions.Blocks"/>, sanitised
/// through their own allow-list, while text variables stay escaped.
/// </summary>
public sealed class MailBlocksTests
{
    private static readonly HtmlParser Parser = new();

    private static IElement Content(OmniMailRenderResult result) =>
        Parser.ParseDocument(result.Html).QuerySelectorAll("td")
            .Single(cell => cell.GetAttribute("style")?.Contains("padding:32px", StringComparison.Ordinal) == true);

    private static OmniMailRenderResult Render(string template, Dictionary<string, string?>? values = null, Dictionary<string, string?>? blocks = null, OmniMailTheme? theme = null) =>
        OmniMailRenderer.Render(template, values, new OmniMailRenderOptions { Blocks = blocks, Theme = theme });

    [Fact]
    public void AButton_IsATableWithALinkPaddedInItsCell_ColouredFromTheTheme()
    {
        var theme = OmniMailTheme.Default with { Accent = "#112233", OnAccent = "#fafafa", Radius = "6px" };
        var result = Render(
            "<p>Avant</p><p class=\"omni-align-center\">{{#button run.url}}Ouvrir {{run.name}}</p><p>Après</p>",
            new() { ["run.url"] = "https://app.example/runs/42?tab=steps&x=1", ["run.name"] = "<nightly>" },
            theme: theme);

        var content = Content(result);
        var link = Assert.Single(content.QuerySelectorAll("a"));
        Assert.Equal("https://app.example/runs/42?tab=steps&x=1", link.GetAttribute("href"));
        Assert.Equal("Ouvrir <nightly>", link.TextContent);
        Assert.Contains("&lt;nightly&gt;", result.Html, StringComparison.Ordinal);
        Assert.Contains("color:#fafafa", link.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("display:inline-block", link.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains("border-radius:6px", link.GetAttribute("style"), StringComparison.Ordinal);

        var cell = (IElement)link.Parent!;
        Assert.Equal("td", cell.LocalName);
        Assert.Equal("#112233", cell.GetAttribute("bgcolor"));
        Assert.Contains("background-color:#112233", cell.GetAttribute("style"), StringComparison.Ordinal);
        var table = cell.Closest("table")!;
        Assert.Equal("presentation", table.GetAttribute("role"));
        Assert.Equal("center", table.GetAttribute("align"));
        Assert.Null(table.GetAttribute("width"));

        // The marker writes nothing, the paragraph is gone, its neighbours stay; no internal attribute leaks out.
        Assert.DoesNotContain("{{", result.Html, StringComparison.Ordinal);
        Assert.Equal(["Avant", "Après"], content.QuerySelectorAll("p").Select(paragraph => paragraph.TextContent));
        Assert.DoesNotContain("data-omni-mail-own", result.Html, StringComparison.Ordinal);
        Assert.Contains("Ouvrir <nightly> (https://app.example/runs/42?tab=steps&x=1)", result.Text, StringComparison.Ordinal);
        Assert.Empty(result.MissingVariables);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,x")]
    [InlineData("/relative")]
    public void AButtonWhoseAddressIsNotHttpHttpsOrMailto_LosesItsLink(string address)
    {
        var result = Render("<p>{{#button url}}Ouvrir</p>", new() { ["url"] = address });

        var content = Content(result);
        Assert.Empty(content.QuerySelectorAll("a"));
        Assert.Equal("Ouvrir", content.QuerySelector("span")!.TextContent);
        Assert.DoesNotContain(address, result.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void AButtonWithoutAddressValue_IsListedMissing_AndAnEmptyLabelDrawsNoButton()
    {
        var result = Render("<p>{{#button url}}Ouvrir</p><p>{{#button other}}{{empty}}</p>", new() { ["empty"] = "" });

        Assert.Equal(["url", "other"], result.MissingVariables);
        Assert.Single(Content(result).QuerySelectorAll("table"));
    }

    [Theory]
    [InlineData("info", "Info", "InfoSubtle")]
    [InlineData("success", "Success", "SuccessSubtle")]
    [InlineData("warning", "Warning", "WarningSubtle")]
    [InlineData("danger", "Danger", "DangerSubtle")]
    [InlineData("unknown", "Info", "InfoSubtle")]
    [InlineData(null, "Info", "InfoSubtle")]
    public void ABanner_IsAFullWidthBand_InItsTonesColours(string? tone, string strongProperty, string subtleProperty)
    {
        var theme = OmniMailTheme.Default;
        var strong = (string)typeof(OmniMailTheme).GetProperty(strongProperty)!.GetValue(theme)!;
        var subtle = (string)typeof(OmniMailTheme).GetProperty(subtleProperty)!.GetValue(theme)!;
        var marker = tone is null ? "{{#banner}}" : $"{{{{#banner {tone}}}}}";
        var result = Render($"<p>{marker} Le run <strong>{{{{name}}}}</strong> a échoué.</p>", new() { ["name"] = "<b>x</b>" });

        var cell = Content(result).QuerySelector("table td")!;
        Assert.Equal(subtle, cell.GetAttribute("bgcolor"));
        Assert.Contains($"background-color:{subtle}", cell.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Contains($"border-left:4px solid {strong}", cell.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Equal("100%", cell.Closest("table")!.GetAttribute("width"));
        Assert.Equal("<b>x</b>", cell.QuerySelector("strong")!.TextContent);
        Assert.Null(cell.QuerySelector("b"));
        Assert.Equal("Le run <b>x</b> a échoué.", cell.TextContent.Trim());
    }

    [Fact]
    public void TheBannerColours_AreReadFromTheThemeTokens()
    {
        var theme = OmniMailTheme.FromTokens(new Dictionary<string, string>
        {
            ["--omni-color-warning"] = "#aa5500",
            ["--omni-color-warning-subtle"] = "#fff3e0",
            ["--omni-color-danger"] = "var(--x)",
        });

        Assert.Equal("#aa5500", theme.Warning);
        Assert.Equal("#fff3e0", theme.WarningSubtle);
        Assert.Equal(new OmniMailTheme().Danger, theme.Danger);
        Assert.NotEqual(new OmniMailTheme().Info, OmniMailTheme.Default.Info);
    }

    [Fact]
    public void ABannerColourThatIsNotLiteral_IsRefused()
    {
        var exception = Assert.Throws<ArgumentException>(() => Render("<p>x</p>", theme: OmniMailTheme.Default with { DangerSubtle = "red;background:url(x)" }));
        Assert.Contains("DangerSubtle", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AValue_CannotMakeABlock()
    {
        var result = Render(
            "<p>Bonjour {{name}}</p>",
            new() { ["name"] = "{{#banner danger}} {{steps}}" },
            new() { ["steps"] = "<table><tr><td>secret</td></tr></table>" });

        var content = Content(result);
        Assert.Empty(content.QuerySelectorAll("table"));
        Assert.Equal("Bonjour {{#banner danger}} {{steps}}", content.QuerySelector("p")!.TextContent);
    }

    [Fact]
    public void ABlockVariableAloneInItsParagraph_IsReplacedByTheFragment_WhileATextVariableStaysEscaped()
    {
        const string Fragment = "<table><thead><tr><th>Étape</th><th>Statut</th></tr></thead><tbody><tr><td>build</td><td style=\"color: #c62828; font-weight: bold\">échec</td></tr></tbody></table>";
        var result = Render(
            "<p>Étapes {{same}} :</p><p>{{steps}}</p><p>{{same}}</p>",
            new() { ["same"] = Fragment },
            new() { ["steps"] = Fragment });

        var content = Content(result);
        var table = Assert.Single(content.QuerySelectorAll("table"));
        Assert.Equal(["Étape", "Statut"], table.QuerySelectorAll("th").Select(cell => cell.TextContent));
        var failed = table.QuerySelectorAll("td")[1];
        // The theme's look first, then the host's declarations, which win.
        var style = failed.GetAttribute("style")!;
        Assert.Contains("padding:6px 8px", style, StringComparison.Ordinal);
        Assert.True(style.IndexOf("color: #c62828", StringComparison.Ordinal) > style.IndexOf("border:", StringComparison.Ordinal), style);
        Assert.Equal("100%", table.GetAttribute("width"));

        // The same HTML given as a text value is written as text, twice.
        Assert.Equal(2, content.QuerySelectorAll("p").Count(paragraph => paragraph.TextContent.Contains("<table>", StringComparison.Ordinal)));
        Assert.Contains("build", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ABlockVariableInsideALine_IsWrittenAsItsPlainText()
    {
        var result = Render("<p>Voir {{steps}} ici</p>", blocks: new() { ["steps"] = "<p><strong>build</strong> a échoué</p>" });

        var paragraph = Assert.Single(Content(result).QuerySelectorAll("p"));
        Assert.Equal("Voir build a échoué ici", paragraph.TextContent);
        Assert.Null(paragraph.QuerySelector("strong"));
    }

    [Fact]
    public void ABlockVariable_FillsAListItemOrACell_AndANullBlockRendersNothing_ListedMissing()
    {
        var result = Render(
            "<ul><li>{{steps}}</li></ul><p>{{gone}}</p><p>{{name}}</p>",
            new() { ["name"] = "Anne" },
            new() { ["steps"] = "<em>trois</em>", ["gone"] = null });

        var content = Content(result);
        Assert.Equal("trois", content.QuerySelector("li > em")!.TextContent);
        Assert.Equal(["Anne"], content.QuerySelectorAll("p").Select(paragraph => paragraph.TextContent));
        Assert.Equal(["gone"], result.MissingVariables);
    }

    [Fact]
    public void TheTokensOfAFragment_AreNeverReplaced()
    {
        var result = Render("<p>{{steps}}</p>", new() { ["secret"] = "fuite" }, new() { ["steps"] = "<p>{{secret}}</p>" });

        Assert.Equal("{{secret}}", Content(result).QuerySelector("p")!.TextContent);
        Assert.DoesNotContain("fuite", result.Html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<script>alert(1)</script><p>ok</p>", "alert")]
    [InlineData("<p onclick=\"steal()\" onmouseover=\"x()\">ok</p>", "steal")]
    [InlineData("<link rel=\"stylesheet\" href=\"https://evil.example/a.css\"><p>ok</p>", "evil.example")]
    [InlineData("<style>@import url(https://evil.example/a.css); p { color: red }</style><p>ok</p>", "evil.example")]
    [InlineData("<p style=\"background-image: url(https://evil.example/t.gif)\">ok</p>", "evil.example")]
    [InlineData("<p style=\"color: red; width: expression(alert(1))\">ok</p>", "expression")]
    [InlineData("<p><a href=\"javascript:alert(1)\">ok</a></p>", "javascript")]
    [InlineData("<p><img src=\"https://evil.example/pixel.gif\">ok</p>", "evil.example")]
    [InlineData("<iframe src=\"https://evil.example\"></iframe><p>ok</p>", "evil.example")]
    [InlineData("<form action=\"https://evil.example\"><input name=\"p\"></form><p>ok</p>", "evil.example")]
    [InlineData("<svg><script>alert(1)</script></svg><p>ok</p>", "alert")]
    [InlineData("<p class=\"tracker\" id=\"x\" data-track=\"1\">ok</p>", "tracker")]
    public void ABlockFragment_IsSanitised_NothingThatRunsLoadsOrReachesOut_Survives(string fragment, string refused)
    {
        var result = Render("<p>{{steps}}</p>", blocks: new() { ["steps"] = fragment });

        Assert.DoesNotContain(refused, result.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ok", Content(result).TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", result.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ABlockFragment_KeepsItsSafeLinks_WithNoopener()
    {
        var result = Render("<p>{{steps}}</p>", blocks: new() { ["steps"] = "<p><a href=\"https://app.example/run/1\">run</a> <a href=\"mailto:ops@example.eu\">ops</a></p>" });

        var links = Content(result).QuerySelectorAll("a");
        Assert.Equal(["https://app.example/run/1", "mailto:ops@example.eu"], links.Select(link => link.GetAttribute("href")));
        Assert.All(links, link => Assert.Equal("noopener noreferrer", link.GetAttribute("rel")));
    }

    [Fact]
    public void ANameGivenBothAsTextAndAsBlock_IsRefused()
    {
        var exception = Assert.Throws<ArgumentException>(() => Render("<p>{{x}}</p>", new() { ["x"] = "a" }, new() { ["x"] = "<p>b</p>" }));
        Assert.Contains("'x'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FindVariables_ListsBlockVariablesAndButtonAddresses_NotBannerTones()
    {
        Assert.Equal(
            ["title", "run.url", "steps", "name"],
            OmniMailRenderer.FindVariables("<p>{{title}}</p><p>{{#button run.url}}Go</p><p>{{#banner warning}} {{steps}}</p><p>{{name}} {{#button}}</p>"));
    }

    [Fact]
    public void MissingVariables_StayInTheOrderTheyFirstAppear_BlocksIncluded()
    {
        var result = OmniMailRenderer.Render(
            "<p>{{a}}</p><p>{{block}}</p><p>{{#button url}}x</p><p>{{b}}</p>",
            null,
            new OmniMailRenderOptions { Subject = "{{s}}", Blocks = new Dictionary<string, string?> { ["block"] = null } });

        Assert.Equal(["s", "a", "block", "url", "b"], result.MissingVariables);
    }

    [Fact]
    public void AMarkerWrittenDirectlyInTheBody_TurnsItsLineIntoTheBlock()
    {
        var result = Render("{{#banner success}} Tout est <em>vert</em>.<p>Suite</p>");

        var content = Content(result);
        var cell = content.QuerySelector("table td")!;
        Assert.Equal("Tout est vert.", cell.TextContent.Trim());
        Assert.Equal("vert", cell.QuerySelector("em")!.TextContent);
        Assert.Equal("Suite", content.QuerySelector("p")!.TextContent);
    }

    [Fact]
    public void AButtonWithoutAnAddress_IsALabelWithoutALink()
    {
        var content = Content(Render("<p>{{#button}}Plus tard</p>"));

        Assert.Empty(content.QuerySelectorAll("a"));
        var label = content.QuerySelector("table td span")!;
        Assert.Equal("Plus tard", label.TextContent);
        Assert.Contains("display:inline-block", label.GetAttribute("style"), StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyBanner_DrawsNothing_AndRemovesItsParagraph()
    {
        var content = Content(Render("<p>Avant</p><p>{{#banner danger}}  </p><p>Après</p>"));

        Assert.Empty(content.QuerySelectorAll("table"));
        Assert.Equal(["Avant", "Après"], content.QuerySelectorAll("p").Select(paragraph => paragraph.TextContent));
    }

    [Fact]
    public void ABannerAlignedToTheEnd_KeepsItsAlignment()
    {
        var cell = Content(Render("<p class=\"omni-align-end\">{{#banner warning}} Fin</p>")).QuerySelector("table td")!;

        Assert.Contains("text-align:right", cell.GetAttribute("style"), StringComparison.Ordinal);
        Assert.Equal("Fin", cell.TextContent.Trim());
    }

    [Fact]
    public void TwoMarkersInOneParagraph_DrawOneBlock_TheFirstOneWins()
    {
        var content = Content(Render("<p>{{#banner success}} Bon <em>{{#banner danger}}</em> état</p>"));

        var cell = Assert.Single(content.QuerySelectorAll("table td"));
        Assert.Equal("Bon  état", cell.TextContent.Trim());
        Assert.Contains($"border-left:4px solid {OmniMailTheme.Default.Success}", cell.GetAttribute("style"), StringComparison.Ordinal);
        Assert.DoesNotContain("{{", content.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void AMarkerInsideInlineFormattingAtTheBodyLevel_WrapsTheWholeLine()
    {
        var content = Content(Render("<p>Avant</p><strong>Note</strong> <em>{{#banner info}} lisez</em> bien"));

        var cell = Assert.Single(content.QuerySelectorAll("table td"));
        Assert.Equal("Note  lisez bien", cell.TextContent.Trim());
        Assert.Equal("Note", cell.QuerySelector("strong")!.TextContent);
        Assert.Equal("Avant", content.QuerySelector("p")!.TextContent);
    }

    [Fact]
    public void ABlockVariableWrittenDirectlyInTheBody_IsWrittenAsPlainText()
    {
        var content = Content(Render("Étapes : {{steps}}", blocks: new() { ["steps"] = "<p><strong>build</strong></p>" }));

        Assert.Empty(content.QuerySelectorAll("strong"));
        Assert.Contains("Étapes : build", content.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ABlockVariableAloneInAListItem_FillsTheItem()
    {
        var content = Content(Render("<ul><li><em>{{steps}}</em></li></ul>", blocks: new() { ["steps"] = "<strong>build</strong>" }));

        var item = Assert.Single(content.QuerySelectorAll("li"));
        Assert.Equal("build", item.QuerySelector("strong")!.TextContent);
        Assert.Empty(item.QuerySelectorAll("em"));
    }

    [Fact]
    public void ABlankStyleOnAFragment_AddsNoDeclaration()
    {
        var content = Content(Render("<p>{{steps}}</p>", blocks: new() { ["steps"] = "<p style=\" \">texte</p>" }));

        var paragraph = Assert.Single(content.QuerySelectorAll("p"));
        Assert.Equal("texte", paragraph.TextContent);
        Assert.DoesNotContain(";;", paragraph.GetAttribute("style") ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void TheBlocks_WriteTheirLooksInThePreviewAttribute_UnderTheStrictCsp()
    {
        var result = Internal.MailDocument.Render(
            "<p>{{#button url}}Ouvrir</p><p>{{#banner info}} Note</p><p>{{steps}}</p>",
            new Dictionary<string, string?> { ["url"] = "https://exemple.eu" },
            new OmniMailRenderOptions { Blocks = new Dictionary<string, string?> { ["steps"] = "<p style=\"color: red\">rouge</p>" } },
            Internal.MailDocument.PreviewLook);

        Assert.DoesNotContain(" style=", result.Html, StringComparison.Ordinal);
        Assert.Contains("color:#ff0000", result.Html.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("data-omni-mail-look=\"display:inline-block", result.Html, StringComparison.Ordinal);
    }
}
