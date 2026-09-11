using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SettingsComponentTests : OmniBunitContext
{
    [Fact]
    public void SettingsSection_IsACardWithItsHeadingAndSettings()
    {
        var section = Render<OmniSettingsSection>(parameters => parameters
            .Add(component => component.Title, "Apparence")
            .Add(component => component.Description, "Thème et densité.")
            .AddChildContent("<div class=\"setting\">Thème</div>"));

        var card = section.Find("section.omni-card.omni-settings-section");
        Assert.Equal("Apparence", card.QuerySelector("h2.omni-settings-section__title")!.TextContent);
        Assert.Equal("Thème et densité.", card.QuerySelector(".omni-settings-section__heading .omni-settings-hint")!.TextContent);
        Assert.NotNull(card.QuerySelector(".omni-card__body .setting"));
        Assert.DoesNotContain("style=", section.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SettingsSection_WithoutDescription_RendersNoHint()
    {
        var section = Render<OmniSettingsSection>(parameters => parameters.Add(component => component.Title, "Données"));

        Assert.Empty(section.FindAll(".omni-settings-hint"));
    }

    [Fact]
    public void SettingsTile_PutsTheControlOnTheRowAndDetailsUnderIt()
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Densité")
            .Add(component => component.Description, "L'espace entre les éléments.")
            .Add(component => component.Icon, (RenderFragment)(builder => builder.AddContent(0, "icon")))
            .Add(component => component.Details, (RenderFragment)(builder => builder.AddMarkupContent(0, "<input class=\"slider\" />")))
            .AddChildContent("<button class=\"control\">+</button>"));

        var row = tile.Find(".omni-settings-tile > .omni-settings-tile__row");
        Assert.Equal("true", row.QuerySelector(".omni-settings-tile__icon")!.GetAttribute("aria-hidden"));
        Assert.Equal("Densité", row.QuerySelector(".omni-settings-tile__title")!.TextContent);
        Assert.Equal("L'espace entre les éléments.", row.QuerySelector(".omni-settings-hint")!.TextContent);
        Assert.NotNull(row.QuerySelector("button.control"));
        Assert.NotNull(tile.Find(".omni-settings-tile > input.slider"));
    }

    [Fact]
    public void SettingsTile_WithoutIconOrDescription_RendersOnlyTheName()
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters.Add(component => component.Title, "Version"));

        Assert.Empty(tile.FindAll(".omni-settings-tile__icon"));
        Assert.Empty(tile.FindAll(".omni-settings-hint"));
        Assert.Equal("Version", tile.Find(".omni-settings-tile__title").TextContent);
    }
}
