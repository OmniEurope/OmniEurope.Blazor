using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The help text of <see cref="OmniStatTile"/>: absent, the markup is what it always was; given, a focusable
/// "?" mark sits in the tile, outside the button or the link of a clickable tile (the root is then a block
/// holding the real button or link). The markup, the ids, the callback and the safe address are proved here;
/// the stretched click area, the focus ring and the position of the mark are proved in a real browser.
/// </summary>
public sealed class StatTileHelpTests : OmniBunitContext
{
    private const string HelpText = "Jetons consommés depuis minuit.";

    private static string Normalised(string markup) =>
        Regex.Replace(Regex.Replace(markup, " blazor:elementReference=\"[^\"]*\"", string.Empty), @">\s+<", "><");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithoutHelp_ThePlainTileRendersNoMark_AndKeepsItsMarkup(string? help)
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "83 %")
            .Add(component => component.Title, "Hebdomadaire")
            .Add(component => component.Help, help));

        Assert.Equal(
            "<div class=\"omni-stat-tile\"><span class=\"omni-stat-tile__text\"><span class=\"omni-stat-tile__value\">83 %</span><span class=\"omni-stat-tile__label\">Hebdomadaire</span></span></div>",
            Normalised(tile.Markup));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithBlankHelp_AClickableTileStaysOneButtonOrOneLink(bool button)
    {
        var tile = Render<OmniStatTile>(parameters =>
        {
            parameters.Add(component => component.Title, "Jetons").Add(component => component.Help, " ");
            if (button)
            {
                parameters.Add(component => component.OnClick, () => { });
            }
            else
            {
                parameters.Add(component => component.Href, "/jetons");
            }
        });

        var root = tile.Nodes.OfType<AngleSharp.Dom.IElement>().Single();
        Assert.Equal(button ? "BUTTON" : "A", root.TagName);
        Assert.Empty(tile.FindAll(".omni-tooltip"));
        Assert.Empty(tile.FindAll(".omni-stat-tile__action"));
    }

    [Fact]
    public void APlainTile_WithHelp_GainsAFocusableMark_AndStaysABlock()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Id, "tokens")
            .Add(component => component.Value, "12,4 k")
            .Add(component => component.Title, "Jetons")
            .Add(component => component.Help, HelpText));

        var root = tile.Find("div.omni-stat-tile");
        Assert.Contains("omni-stat-tile--help", root.ClassList);
        Assert.DoesNotContain("omni-stat-tile--action", root.ClassList);
        Assert.Equal("tokens", root.GetAttribute("id"));
        Assert.Empty(tile.FindAll("button"));
        Assert.Empty(tile.FindAll("a"));

        var tooltip = root.QuerySelector(":scope > .omni-tooltip.omni-stat-tile__help")!;
        Assert.Equal("tokens-help", tooltip.GetAttribute("id"));
        var trigger = tooltip.QuerySelector(".omni-tooltip__trigger")!;
        Assert.Equal("0", trigger.GetAttribute("tabindex"));
        Assert.Equal("tokens-help-content", trigger.GetAttribute("aria-describedby"));
        var mark = trigger.QuerySelector(".omni-stat-tile__help-mark")!;
        Assert.Equal("img", mark.GetAttribute("role"));
        Assert.Equal("Aide", mark.GetAttribute("aria-label"));
        var content = tile.Find("#tokens-help-content");
        Assert.Equal("tooltip", content.GetAttribute("role"));
        Assert.Equal(HelpText, content.TextContent);
        // The tile's own text is not changed by the help.
        Assert.Equal("12,4 k", tile.Find(".omni-stat-tile__value").TextContent);
        Assert.DoesNotContain("style=", tile.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void APlainTile_WithHelpAndNoId_HasNoIdOnTheMark()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, "Jetons")
            .Add(component => component.Help, HelpText));

        Assert.Null(tile.Find(".omni-stat-tile__help").GetAttribute("id"));
    }

    [Fact]
    public void AButtonTile_WithHelp_IsABlockHoldingTheNamedButton_AndTheMarkOutsideIt()
    {
        var clicks = 0;
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Id, "tokens")
            .Add(component => component.Class, "host-class")
            .Add(component => component.Value, "83 %")
            .Add(component => component.Title, "Hebdomadaire")
            .Add(component => component.Detail, "Réinitialisation")
            .Add(component => component.DoubleWidth, true)
            .Add(component => component.WrapTitle, true)
            .Add(component => component.Size, OmniControlSize.Large)
            .Add(component => component.Help, HelpText)
            .Add(component => component.OnClick, () => clicks++));

        var root = tile.Find("div.omni-stat-tile");
        Assert.Equal("tokens", root.GetAttribute("id"));
        foreach (var expected in new[] { "omni-stat-tile", "omni-stat-tile--action", "omni-stat-tile--help", "omni-tile--double", "omni-stat-tile--wrap", "omni-stat-tile--large", "host-class" })
        {
            Assert.Contains(expected, root.ClassList);
        }

        var button = root.QuerySelector(":scope > button.omni-stat-tile__action")!;
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Hebdomadaire : 83 %", button.GetAttribute("aria-label"));
        Assert.Null(button.GetAttribute("id"));
        Assert.Equal("83 %", button.QuerySelector(".omni-stat-tile__value")!.TextContent);
        Assert.Equal("Réinitialisation", button.QuerySelector(".omni-stat-tile__detail")!.TextContent);

        // The mark is beside the button, never inside it, and carries its ids.
        Assert.Single(tile.FindAll("button"));
        Assert.Null(button.QuerySelector(".omni-tooltip"));
        var tooltip = root.QuerySelector(":scope > .omni-tooltip.omni-stat-tile__help")!;
        Assert.Equal("tokens-help", tooltip.GetAttribute("id"));
        Assert.Equal("tokens-help-content", tooltip.QuerySelector(".omni-tooltip__trigger")!.GetAttribute("aria-describedby"));
        Assert.Equal(HelpText, tile.Find("#tokens-help-content").TextContent);

        button.Click();
        Assert.Equal(1, clicks);
        Assert.DoesNotContain("style=", tile.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ALinkTile_WithHelp_IsABlockHoldingTheNamedLink_AndTheMarkOutsideIt()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Id, "quota")
            .Add(component => component.Value, "83 %")
            .Add(component => component.Title, "Hebdomadaire")
            .Add(component => component.Help, HelpText)
            .Add(component => component.Href, "#quotas"));

        var root = tile.Find("div.omni-stat-tile.omni-stat-tile--action");
        var link = root.QuerySelector(":scope > a.omni-stat-tile__action")!;
        Assert.Equal("#quotas", link.GetAttribute("href"));
        Assert.Equal("Hebdomadaire : 83 %", link.GetAttribute("aria-label"));
        Assert.Empty(tile.FindAll("button"));
        Assert.Single(tile.FindAll("a"));
        Assert.Null(link.QuerySelector(".omni-tooltip"));
        Assert.NotNull(root.QuerySelector(":scope > .omni-tooltip.omni-stat-tile__help"));
        Assert.Equal("quota-help", tile.Find(".omni-tooltip").GetAttribute("id"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example/x")]
    public void ALinkTile_WithHelp_StillRefusesAnUnsafeHref(string href)
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, "Jetons")
            .Add(component => component.Help, HelpText)
            .Add(component => component.Href, href)));
    }

    [Fact]
    public void ATile_WithHelp_HrefAndOnClick_StillThrows()
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Title, "Jetons")
            .Add(component => component.Help, HelpText)
            .Add(component => component.Href, "/a")
            .Add(component => component.OnClick, () => { })));

        Assert.Equal("Href", exception.ParamName);
    }

    [Fact]
    public void TheHelpStyles_AreShipped_WithTheMarkAboveTheStretchedButton()
    {
        var help = ShippedLookTests.Body(".omni-stat-tile > .omni-stat-tile__help");
        Assert.Equal("absolute", ShippedLookTests.Value(help, "position"));
        Assert.Equal("1", ShippedLookTests.Value(help, "z-index"));
        var stretched = ShippedLookTests.Body(".omni-stat-tile__action::after");
        Assert.Equal("absolute", ShippedLookTests.Value(stretched, "position"));
        Assert.Equal("0", ShippedLookTests.Value(stretched, "inset"));
        Assert.Contains("var(--omni-focus-ring)", ShippedLookTests.Body(".omni-stat-tile--action:has(> .omni-stat-tile__action:focus-visible)"), StringComparison.Ordinal);
    }
}
