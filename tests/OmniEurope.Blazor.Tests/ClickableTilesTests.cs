using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Clickable tiles (recette R1-20): an <see cref="OmniCard"/> with Href or OnClick and an
/// <see cref="OmniStatTile"/> with Href stay tiles of an <see cref="OmniTileGrid"/> (same classes on the
/// root, so the grid rule and DoubleWidth reach them) and become one real link or one real button. The
/// markup, the callback and the refusals are proved here; the measured sizes and the focus ring are
/// proved in a real browser.
/// </summary>
public sealed class ClickableTilesTests : OmniBunitContext
{
    private static readonly RenderFragment Heading = builder =>
    {
        builder.OpenElement(0, "h3");
        builder.AddContent(1, "Titre");
        builder.CloseElement();
    };

    // The element reference Blazor adds and the white space between the spans (inside a flex column, so it
    // draws nothing) are not part of what the tile writes.
    private static string Normalised(string markup) =>
        Regex.Replace(Regex.Replace(markup, " blazor:elementReference=\"[^\"]*\"", string.Empty), @">\s+<", "><");

    // ---- OmniCard -------------------------------------------------------------------------------

    [Fact]
    public void ACard_WithoutHrefOrOnClick_KeepsItsSectionMarkup()
    {
        var card = Render<OmniCard>(parameters => parameters
            .Add(component => component.Header, Heading)
            .Add(component => component.Footer, "Pied")
            .AddChildContent("Corps"));

        Assert.Equal(
            "<section class=\"omni-card\"><header class=\"omni-card__header\"><h3>Titre</h3></header><div class=\"omni-card__body\">Corps</div><footer class=\"omni-card__footer\">Pied</footer></section>",
            card.Markup);
    }

    [Fact]
    public void ACard_WithAnEmptyHref_IsStillAPlainSection()
    {
        var card = Render<OmniCard>(parameters => parameters.Add(component => component.Href, " ").AddChildContent("Corps"));

        Assert.Equal("SECTION", card.Find(".omni-card").TagName);
        Assert.Empty(card.FindAll("a"));
    }

    [Fact]
    public void ACard_WithHref_IsOneLink_WhoseContentStaysFlowContent()
    {
        var card = Render<OmniCard>(parameters => parameters
            .Add(component => component.Href, "/details")
            .Add(component => component.Header, Heading)
            .Add(component => component.Footer, "Pied")
            .AddChildContent("Corps"));

        var root = card.Find(".omni-card");
        Assert.Equal("A", root.TagName);
        Assert.Equal("/details", root.GetAttribute("href"));
        Assert.Contains("omni-card--action", root.ClassList);
        Assert.Null(root.GetAttribute("type"));
        Assert.Empty(card.FindAll("button"));
        Assert.Empty(card.FindAll("section"));
        // The heading is kept and exposed: a link holds flow content, so its parts are divisions.
        Assert.Equal("DIV", card.Find(".omni-card__header").TagName);
        Assert.Equal("Titre", card.Find(".omni-card__header h3").TextContent);
        Assert.Equal("DIV", card.Find(".omni-card__body").TagName);
        Assert.Equal("DIV", card.Find(".omni-card__footer").TagName);
        Assert.DoesNotContain("style=", card.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ACard_WithOnClick_IsOneButton_WhoseContentIsPhrasingContent_AndClicksTheCallback()
    {
        var clicks = 0;
        var card = Render<OmniCard>(parameters => parameters
            .Add(component => component.OnClick, () => clicks++)
            .Add(component => component.Header, "En-tête")
            .Add(component => component.Footer, "Pied")
            .AddChildContent("Corps"));

        var root = card.Find(".omni-card");
        Assert.Equal("BUTTON", root.TagName);
        Assert.Equal("button", root.GetAttribute("type"));
        Assert.Null(root.GetAttribute("href"));
        Assert.Contains("omni-card--action", root.ClassList);
        Assert.Empty(card.FindAll("section"));
        Assert.Empty(card.FindAll("div"));
        Assert.Equal("SPAN", card.Find(".omni-card__header").TagName);
        Assert.Equal("SPAN", card.Find(".omni-card__body").TagName);
        Assert.Equal("SPAN", card.Find(".omni-card__footer").TagName);
        Assert.DoesNotContain("style=", card.Markup, StringComparison.OrdinalIgnoreCase);

        root.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void AClickableCard_KeepsItsTileClassesOnItsRoot_AndTheDensityAndAttributes()
    {
        var link = Render<OmniCard>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.DoubleWidth, true)
            .Add(component => component.Density, OmniDensity.Compact)
            .AddUnmatched("aria-label", "Voir le détail")
            .AddChildContent("x"));
        var button = Render<OmniCard>(parameters => parameters
            .Add(component => component.OnClick, () => { })
            .Add(component => component.DoubleWidth, true)
            .Add(component => component.Id, "carte")
            .AddChildContent("x"));

        var linkRoot = link.Find(".omni-card");
        Assert.Contains("omni-tile--double", linkRoot.ClassList);
        Assert.Equal("Voir le détail", linkRoot.GetAttribute("aria-label"));
        Assert.Equal("compact", linkRoot.GetAttribute("data-omni-density"));
        var buttonRoot = button.Find(".omni-card");
        Assert.Contains("omni-tile--double", buttonRoot.ClassList);
        Assert.Equal("carte", buttonRoot.GetAttribute("id"));

        var single = Render<OmniCard>(parameters => parameters.Add(component => component.Href, "/a").AddChildContent("x"));
        Assert.DoesNotContain("omni-tile--double", single.Find(".omni-card").ClassList);
    }

    [Fact]
    public void AClickableCard_InATileGrid_IsADirectChildOfTheGrid()
    {
        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniCard>(card => card.Add(component => component.Href, "/a").AddChildContent("a"))
            .AddChildContent<OmniCard>(card => card.Add(component => component.OnClick, () => { }).AddChildContent("b")));

        var children = grid.Find(".omni-tile-grid").Children;
        Assert.Equal(["A", "BUTTON"], children.Select(child => child.TagName).ToArray());
        Assert.All(children, child => Assert.Contains("omni-card", child.ClassList));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,x")]
    [InlineData("/a\nb")]
    public void ACard_RefusesAnUnsafeHref(string href)
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniCard>(parameters => parameters
            .Add(component => component.Href, href)
            .AddChildContent("x")));
    }

    [Theory]
    [InlineData("/details")]
    [InlineData("details?x=1")]
    [InlineData("https://example.org/a")]
    [InlineData("mailto:a@example.org")]
    [InlineData("tel:+3212345678")]
    public void ACard_AcceptsASafeHref(string href)
    {
        var card = Render<OmniCard>(parameters => parameters.Add(component => component.Href, href).AddChildContent("x"));

        Assert.Equal(href, card.Find("a.omni-card").GetAttribute("href"));
    }

    [Fact]
    public void ACard_WithHrefAndOnClick_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniCard>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.OnClick, () => { })
            .AddChildContent("x")));

        Assert.Equal("Href", exception.ParamName);
    }

    // ---- OmniStatTile ---------------------------------------------------------------------------

    [Fact]
    public void AStatTile_WithoutHrefOrOnClick_KeepsItsBlockMarkup()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "83 %")
            .Add(component => component.Label, "Hebdomadaire")
            .Add(component => component.Detail, "Réinitialisation"));

        Assert.Equal(
            "<div class=\"omni-stat-tile\"><span class=\"omni-stat-tile__text\"><span class=\"omni-stat-tile__value\">83 %</span><span class=\"omni-stat-tile__label\">Hebdomadaire</span><span class=\"omni-stat-tile__detail\">Réinitialisation</span></span></div>",
            Normalised(tile.Markup));
    }

    [Fact]
    public void AStatTile_WithHref_IsOneNamedLink_DrawnLikeTheButton()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "83 %")
            .Add(component => component.Label, "Hebdomadaire")
            .Add(component => component.Detail, "Réinitialisation")
            .Add(component => component.Href, "#quotas"));

        var link = tile.Find("a.omni-stat-tile.omni-stat-tile--action");
        Assert.Equal("#quotas", link.GetAttribute("href"));
        Assert.Equal("Hebdomadaire : 83 %", link.GetAttribute("aria-label"));
        Assert.Null(link.GetAttribute("type"));
        Assert.Empty(tile.FindAll("button"));
        Assert.Equal("83 %", tile.Find(".omni-stat-tile__value").TextContent);
        Assert.Equal("Réinitialisation", tile.Find(".omni-stat-tile__detail").TextContent);
        Assert.DoesNotContain("style=", tile.Markup, StringComparison.OrdinalIgnoreCase);

        var named = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Label, "Jetons")
            .Add(component => component.Href, "/jetons"));
        Assert.Equal("Jetons", named.Find("a").GetAttribute("aria-label"));
    }

    [Fact]
    public void AClickableStatTile_KeepsItsGridClassesOnItsRoot_InBothModes()
    {
        var link = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Label, "Jetons")
            .Add(component => component.Href, "/a")
            .Add(component => component.DoubleWidth, true));
        var button = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Label, "Jetons")
            .Add(component => component.OnClick, () => { })
            .Add(component => component.DoubleWidth, true));

        Assert.Contains("omni-tile--double", link.Find(".omni-stat-tile").ClassList);
        Assert.Contains("omni-tile--double", button.Find(".omni-stat-tile").ClassList);

        var grid = Render<OmniTileGrid>(parameters => parameters
            .AddChildContent<OmniStatTile>(tile => tile.Add(component => component.Label, "A").Add(component => component.Href, "/a"))
            .AddChildContent<OmniStatTile>(tile => tile.Add(component => component.Label, "B").Add(component => component.OnClick, () => { })));
        Assert.Equal(["A", "BUTTON"], grid.Find(".omni-tile-grid").Children.Select(child => child.TagName).ToArray());
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("vbscript:x")]
    public void AStatTile_RefusesAnUnsafeHref(string href)
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Label, "Jetons")
            .Add(component => component.Href, href)));
    }

    [Fact]
    public void AStatTile_WithHrefAndOnClick_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Label, "Jetons")
            .Add(component => component.Href, "/a")
            .Add(component => component.OnClick, () => { })));

        Assert.Equal("Href", exception.ParamName);
    }

    // ---- The shipped rules ---------------------------------------------------------------------

    [Fact]
    public void TheClickableLook_IsShippedForTheCardAndSharesTheStatTileHoverAndFocusRing()
    {
        var css = ShippedLookTests.Css;
        var action = ShippedLookTests.Body(".omni-card--action");
        Assert.Equal("100%", ShippedLookTests.Value(action, "inline-size"));
        Assert.Equal("inherit", ShippedLookTests.Value(action, "font"));
        Assert.Equal("start", ShippedLookTests.Value(action, "text-align"));
        Assert.Equal("none", ShippedLookTests.Value(action, "text-decoration"));
        Assert.Equal("flex", ShippedLookTests.Value(action, "display"));
        Assert.Equal("hidden", ShippedLookTests.Value(ShippedLookTests.Body(".omni-tile-grid > .omni-card.omni-card--action > .omni-card__body"), "overflow"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-stat-tile--action"), "text-decoration"));
        Assert.Contains(".omni-stat-tile--action:focus-visible,\n.omni-card--action:focus-visible", css, StringComparison.Ordinal);
        Assert.Contains("var(--omni-focus-ring)", ShippedLookTests.Body(".omni-stat-tile--action:focus-visible,\n.omni-card--action:focus-visible"), StringComparison.Ordinal);
    }
}
