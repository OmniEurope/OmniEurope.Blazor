using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The favorite toggle of a navigation tile: drawn only when IsFavorite is set, a pressed-state button that is a
/// sibling of the link (never nested in it), and a click that reports the toggled value without navigating.
/// </summary>
public sealed class NavTileFavoriteTests : OmniBunitContext
{
    [Fact]
    public void WithoutIsFavorite_NoToggleAndNoHost()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A"));

        Assert.Empty(tile.FindAll("button"));
        Assert.Empty(tile.FindAll(".omni-nav-tile-host"));
        Assert.Equal("A", tile.Find("a.omni-nav-tile .omni-nav-tile__title").TextContent);
    }

    [Fact]
    public void Class_GoesOnTheLinkOnly_NeverOnTheHostOrTheStar()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.Class, "requires-online-navigation")
            .Add(component => component.IsFavorite, false));

        Assert.Single(tile.FindAll(".requires-online-navigation"));
        Assert.True(tile.Find("a.omni-nav-tile").ClassList.Contains("requires-online-navigation"));
        Assert.False(tile.Find(".omni-nav-tile-host").ClassList.Contains("requires-online-navigation"));
        Assert.False(tile.Find("button.omni-nav-tile__favorite").ClassList.Contains("requires-online-navigation"));
    }

    [Theory]
    [InlineData(true, "true", "Retirer des favoris")]
    [InlineData(false, "false", "Ajouter aux favoris")]
    public void IsFavorite_SetsAriaPressedAndTheLocalizedName(bool favorite, string pressed, string name)
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.IsFavorite, favorite));

        var button = tile.Find("button.omni-nav-tile__favorite");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal(pressed, button.GetAttribute("aria-pressed"));
        Assert.Equal(name, button.GetAttribute("aria-label"));
        Assert.Equal(favorite, button.ClassList.Contains("omni-nav-tile__favorite--on"));
    }

    [Fact]
    public void TheToggle_IsASiblingOfTheLink_NotInsideIt()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.IsFavorite, false));

        var anchor = tile.Find("a.omni-nav-tile");
        var button = tile.Find("button.omni-nav-tile__favorite");
        Assert.Empty(anchor.QuerySelectorAll("button"));
        Assert.Same(anchor.ParentElement, button.ParentElement);
        Assert.Contains("omni-nav-tile-host", button.ParentElement!.ClassList);
    }

    [Fact]
    public void ClickingTheToggle_ReportsTheToggledValue_AndDoesNotNavigate()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var before = navigation.Uri;
        bool? reported = null;
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.IsFavorite, false)
            .Add(component => component.IsFavoriteChanged, value => reported = value));

        tile.Find("button.omni-nav-tile__favorite").Click();

        Assert.True(reported);
        Assert.Equal(before, navigation.Uri);
    }

    [Fact]
    public void ClickingAFavoriteTile_ReportsFalse()
    {
        bool? reported = null;
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.IsFavorite, true)
            .Add(component => component.IsFavoriteChanged, value => reported = value));

        tile.Find("button.omni-nav-tile__favorite").Click();

        Assert.False(reported);
    }

    [Fact]
    public void ADoubleWidthFavoriteTile_PutsTheSpanClassOnTheHost()
    {
        var tile = Render<OmniNavTile>(parameters => parameters
            .Add(component => component.Href, "/a")
            .Add(component => component.Title, "A")
            .Add(component => component.DoubleWidth, true)
            .Add(component => component.IsFavorite, false));

        Assert.Contains("omni-tile--double", tile.Find(".omni-nav-tile-host").ClassList);
        Assert.DoesNotContain("omni-tile--double", tile.Find("a.omni-nav-tile").ClassList);
    }
}
