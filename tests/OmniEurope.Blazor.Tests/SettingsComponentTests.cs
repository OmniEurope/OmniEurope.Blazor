using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SettingsComponentTests : OmniBunitContext
{
    private readonly ToggleModel _model = new();

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
    public void SettingsTile_WithASwitch_LabelsItByTheNameAndStretchesTheLabelOverTheTile()
    {
        var value = false;
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Thème sombre")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniSwitch>(0);
                builder.AddComponentParameter(1, nameof(OmniSwitch.Value), value);
                builder.AddComponentParameter(2, nameof(OmniSwitch.ValueChanged), EventCallback.Factory.Create<bool>(this, next => value = next));
                builder.AddComponentParameter(3, nameof(OmniSwitch.ValueExpression), (Expression<Func<bool>>)(() => value));
                builder.CloseComponent();
            })));

        var control = tile.Find("button.omni-switch");
        var label = tile.Find("label.omni-settings-tile__title");
        Assert.False(string.IsNullOrEmpty(control.Id));
        Assert.Equal(control.Id, label.GetAttribute("for"));
        Assert.Equal("Thème sombre", label.TextContent);
        Assert.Contains("omni-settings-tile--toggle", tile.Find(".omni-settings-tile").ClassList);
        // The control itself toggles once, whatever the label does around it.
        control.Click();
        Assert.True(value);
    }

    [Fact]
    public void SettingsTile_KeepsTheIdTheCheckBoxWasGiven()
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Notifications")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniCheckBox>(0);
                builder.AddComponentParameter(1, nameof(OmniCheckBox.Id), "notify");
                builder.AddComponentParameter(2, nameof(OmniCheckBox.Value), true);
                builder.AddComponentParameter(3, nameof(OmniCheckBox.ValueExpression), (Expression<Func<bool>>)(() => _model.On));
                builder.CloseComponent();
            })));

        Assert.Equal("notify", tile.Find("input.omni-checkbox").Id);
        Assert.Equal("notify", tile.Find("label.omni-settings-tile__title").GetAttribute("for"));
    }

    [Theory]
    [InlineData(typeof(OmniNullableSwitch), "button.omni-switch")]
    [InlineData(typeof(OmniNullableCheckBox), "button.omni-checkbox-nullable")]
    public void SettingsTile_LabelsTheNullableToggles(Type toggle, string selector)
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Sauvegarde")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent(0, toggle);
                builder.AddComponentParameter(1, "ValueExpression", (Expression<Func<bool?>>)(() => _model.Maybe));
                builder.CloseComponent();
            })));

        Assert.Equal(tile.Find(selector).Id, tile.Find("label.omni-settings-tile__title").GetAttribute("for"));
    }

    [Fact]
    public void SettingsTile_WithAnotherControl_KeepsAPlainNameAndNoToggleSurface()
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Densité")
            .AddChildContent("<button class=\"control\">+</button>"));

        Assert.Empty(tile.FindAll("label"));
        Assert.Equal("SPAN", tile.Find(".omni-settings-tile__title").TagName);
        Assert.DoesNotContain("omni-settings-tile--toggle", tile.Find(".omni-settings-tile").ClassList);
    }

    [Fact]
    public void Toggle_OutsideATile_RendersNoIdOfItsOwn()
    {
        var control = Render<OmniSwitch>(parameters => parameters
            .Add(component => component.ValueExpression, () => _model.On));

        Assert.False(control.Find("button").HasAttribute("id"));
    }

    [Fact]
    public void SettingsTile_ReleasesTheLabelWhenItsToggleGoes()
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Thème sombre")
            .Add(component => component.ChildContent, Toggle(show: true)));
        Assert.NotEmpty(tile.FindAll("label.omni-settings-tile__title"));

        tile.Render(parameters => parameters.Add(component => component.ChildContent, Toggle(show: false)));

        tile.WaitForAssertion(() => Assert.Empty(tile.FindAll("label")));
        Assert.DoesNotContain("omni-settings-tile--toggle", tile.Find(".omni-settings-tile").ClassList);
    }

    private RenderFragment Toggle(bool show) => builder =>
    {
        if (show)
        {
            builder.OpenComponent<OmniSwitch>(0);
            builder.AddComponentParameter(1, nameof(OmniSwitch.ValueExpression), (Expression<Func<bool>>)(() => _model.On));
            builder.CloseComponent();
        }
    };

    [Fact]
    public void SettingsTile_WithoutIconOrDescription_RendersOnlyTheName()
    {
        var tile = Render<OmniSettingsTile>(parameters => parameters.Add(component => component.Title, "Version"));

        Assert.Empty(tile.FindAll(".omni-settings-tile__icon"));
        Assert.Empty(tile.FindAll(".omni-settings-hint"));
        Assert.Equal("Version", tile.Find(".omni-settings-tile__title").TextContent);
    }

    private sealed class ToggleModel
    {
        public bool On { get; set; } = true;

        public bool? Maybe { get; set; }
    }
}
