using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The layout requests of the recette: the navigation tile (R-036), the settings tiles two
/// per row (R-060) and the sidebar toggle the header places itself (R-037).
/// </summary>
public sealed class NavTileAndSettingsPairTests : OmniBunitContext
{
    [Fact]
    public void ANavTile_IsOneRealLink_WithItsIconOnATintedSquare_AndItsTone()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/admin/users")
            .Add(component => component.Title, "Utilisateurs")
            .Add(component => component.Description, "Comptes et rôles")
            .Add(component => component.Tone, OmniTone.Success)
            .Add(component => component.Icon, (RenderFragment)(builder => builder.AddMarkupContent(0, "<svg class=\"omni-icon\"></svg>"))));

        var link = tile.Find("a.omni-nav-tile");
        Assert.Equal("/admin/users", link.GetAttribute("href"));
        Assert.Contains("omni-nav-tile--success", link.ClassList);
        Assert.Equal("true", tile.Find(".omni-nav-tile__icon").GetAttribute("aria-hidden"));
        Assert.Equal("Utilisateurs", tile.Find(".omni-nav-tile__title").TextContent);
        Assert.Equal("Comptes et rôles", tile.Find(".omni-nav-tile__description").TextContent);
        // Hovered, only the shadow changes: no rule of the tile moves it.
        var hover = ShippedLookTests.Body(".omni-nav-tile:hover");
        Assert.Equal("var(--omni-shadow-md)", ShippedLookTests.Value(hover, "box-shadow"));
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.StartsWith(".omni-nav-tile", StringComparison.Ordinal) && rule.Body.Contains("transform", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void ANavTile_RejectsActiveUriSchemes_LikeEveryLinkOfThePackage(string href)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, href).Add(component => component.Title, "Piège")));

        Assert.Contains("URI scheme", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANavTile_WithoutDescription_NorIcon_StaysALinkWithItsTitle()
    {
        var tile = Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, "/aide").Add(component => component.Title, "Aide"));

        Assert.Contains("omni-nav-tile--accent", tile.Find("a").ClassList);
        Assert.Empty(tile.FindAll(".omni-nav-tile__icon, .omni-nav-tile__description"));
    }

    [Fact]
    public void ANavTile_WithoutFitTitle_HasNoFitClass()
    {
        var tile = Render<OmniNavTile>(parameters => parameters.Add(component => component.Href, "/aide").Add(component => component.Title, "Aide"));

        Assert.DoesNotContain(tile.Find("a").ClassList, name => name.StartsWith("omni-nav-tile--fit", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Aide", "omni-nav-tile--fit-8")]
    [InlineData("Environnements", "omni-nav-tile--fit-16")]
    [InlineData("Bibliothèques partagées", "omni-nav-tile--fit-24")]
    [InlineData("Un titre vraiment très long qui dépasse tous les paliers de la tuile", "omni-nav-tile--fit-48")]
    public void ANavTile_WithFitTitle_CarriesTheFitClass_OfTheLengthStepOfItsTitle(string title, string expectedClass)
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/aide")
            .Add(component => component.Title, title)
            .Add(component => component.FitTitle, true));

        var link = tile.Find("a");
        Assert.Contains("omni-nav-tile--fit", link.ClassList);
        Assert.Contains(expectedClass, link.ClassList);
        Assert.Null(link.GetAttribute("style"));
        Assert.Null(tile.Find(".omni-nav-tile__title").GetAttribute("style"));
    }

    [Fact]
    public void TheFitTitleRules_FitOnOneLine_WithoutBreakingAWord_AndAlignLeft()
    {
        var tile = ShippedLookTests.Body(".omni-nav-tile--fit");
        Assert.Equal("inline-size", ShippedLookTests.Value(tile, "container-type"));
        Assert.Equal("flex-start", ShippedLookTests.Value(tile, "align-items"));
        Assert.Equal("start", ShippedLookTests.Value(tile, "text-align"));
        var title = ShippedLookTests.Body(".omni-nav-tile--fit .omni-nav-tile__title");
        Assert.Equal("nowrap", ShippedLookTests.Value(title, "white-space"));
        Assert.Equal("normal", ShippedLookTests.Value(title, "overflow-wrap"));
        Assert.Equal("none", ShippedLookTests.Value(title, "hyphens"));
        Assert.Equal("ellipsis", ShippedLookTests.Value(title, "text-overflow"));
        Assert.Contains("cqi", ShippedLookTests.Value(title, "font-size"), StringComparison.Ordinal);
    }

    [Fact]
    public void APairedSection_SetsItsTilesTwoPerRow_AndAFullWidthTileTakesTheRow()
    {
        var section = Render<OmniSettingsSection>(parameters => parameters
            .Add(component => component.Title, "Général")
            .Add(component => component.Paired, true)
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniSettingsTile>(0);
                builder.AddComponentParameter(1, nameof(OmniSettingsTile.Title), "Dossier");
                builder.AddComponentParameter(2, nameof(OmniSettingsTile.FullWidth), true);
                builder.CloseComponent();
                builder.OpenComponent<OmniSettingsTile>(3);
                builder.AddComponentParameter(4, nameof(OmniSettingsTile.Title), "Démarrage");
                builder.CloseComponent();
            })));

        Assert.Contains("omni-settings-section--paired", section.Find(".omni-settings-section").ClassList);
        var tiles = section.FindAll(".omni-settings-tile");
        Assert.Contains("omni-settings-tile--full", tiles[0].ClassList);
        Assert.DoesNotContain("omni-settings-tile--full", tiles[1].ClassList);
        var grid = ShippedLookTests.Body(".omni-settings-section--paired > .omni-card__body");
        Assert.Contains("max(24rem", ShippedLookTests.Value(grid, "grid-template-columns"), StringComparison.Ordinal);
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.Contains("omni-settings-section", StringComparison.Ordinal) && rule.Body.Contains("max-inline-size", StringComparison.Ordinal));

        var plain = Render<OmniSettingsSection>(parameters => parameters.Add(component => component.Title, "Affichage"));
        Assert.DoesNotContain("omni-settings-section--paired", plain.Find(".omni-settings-section").ClassList);
    }

    [Fact]
    public void TheHeader_PlacesTheSidebarToggleItselfAtItsStart_BeforeTheBrand()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Boutique")
            .Add(component => component.SidebarToggle, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniSidebarToggle>(0);
                builder.AddComponentParameter(1, nameof(OmniSidebarToggle.Controls), "app-sidebar");
                builder.CloseComponent();
            }))
            .AddChildContent("<div class=\"header-bar\">actions</div>"));

        var children = header.Find("header").Children;
        Assert.Contains("omni-sidebar-toggle", children[0].ClassList);
        Assert.Contains("omni-header__brand", children[1].ClassList);
        // A direct child of the header: the compensation of the header's own padding is the only one.
        Assert.Contains("omni-header__brand", children[0].NextElementSibling!.ClassList);
    }
}
