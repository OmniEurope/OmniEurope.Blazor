using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniToggleButton.Label"/> and <see cref="OmniButton.Label"/>: the accessible name of an icon-only button.</summary>
public sealed class ToggleButtonLabelTests : OmniBunitContext
{
    [Fact]
    public void Label_NamesTheButton_AndItStillToggles()
    {
        var value = false;
        var toggle = Render<OmniToggleButton>(parameters => parameters
            .Add(component => component.Label, "Afficher les notes")
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated));

        var button = toggle.Find("button");
        Assert.Equal("Afficher les notes", button.GetAttribute("aria-label"));
        button.Click();
        Assert.True(value);
    }

    [Fact]
    public void Label_WinsOverAnAdditionalAriaLabel_AndNoneIsInvented()
    {
        var splatted = Render<OmniToggleButton>(parameters => parameters
            .AddUnmatched("aria-label", "Par attribut")
            .Add(component => component.Label, "Par paramètre"));
        var bare = Render<OmniToggleButton>();

        Assert.Equal("Par paramètre", splatted.Find("button").GetAttribute("aria-label"));
        Assert.False(bare.Find("button").HasAttribute("aria-label"));
    }

    [Fact]
    public void Button_OwnAttributesWinOverTheHostOnes()
    {
        var button = Render<OmniButton>(parameters => parameters
            .AddUnmatched("type", "submit")
            .AddUnmatched("data-probe", "kept")
            .Add(component => component.Label, "Rechercher")
            .AddChildContent("Q"));

        var element = button.Find("button");
        Assert.Equal("button", element.GetAttribute("type"));
        Assert.Equal("kept", element.GetAttribute("data-probe"));
        Assert.Equal("Rechercher", element.GetAttribute("aria-label"));
    }
}
