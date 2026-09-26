using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniToggleButton.AriaLabel"/>: an accessible name for an icon-only toggle.</summary>
public sealed class ToggleButtonAriaLabelTests : OmniBunitContext
{
    [Fact]
    public void AriaLabel_NamesTheButton_AndItStillToggles()
    {
        var value = false;
        var toggle = Render<OmniToggleButton>(parameters => parameters
            .Add(component => component.AriaLabel, "Afficher les notes")
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated));

        var button = toggle.Find("button");
        Assert.Equal("Afficher les notes", button.GetAttribute("aria-label"));
        button.Click();
        Assert.True(value);
    }

    [Fact]
    public void WithoutAriaLabel_AnAdditionalAriaLabelStillApplies_AndNoneIsInvented()
    {
        var splatted = Render<OmniToggleButton>(parameters => parameters
            .AddUnmatched("aria-label", "Par attribut")
            .Add(component => component.AriaLabel, "Par paramètre"));
        var bare = Render<OmniToggleButton>();

        Assert.Equal("Par attribut", splatted.Find("button").GetAttribute("aria-label"));
        Assert.False(bare.Find("button").HasAttribute("aria-label"));
    }
}
