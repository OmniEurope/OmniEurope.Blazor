using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class BusyButtonTests : OmniBunitContext
{
    [Fact]
    public void Button_KeepsItsRestingContentAndOnlyGainsTheVeilWhenBusy()
    {
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Busy, false)
            .AddChildContent("<span class=\"icon\"></span>Save"));
        var resting = button.Find("button");
        var restingContent = resting.InnerHtml;
        Assert.DoesNotContain("omni-busy", resting.ClassName, StringComparison.Ordinal);
        Assert.False(resting.HasAttribute("aria-busy"));

        button.Render(parameters => parameters.Add(component => component.Busy, true));
        var busy = button.Find("button");

        Assert.Equal(restingContent, busy.InnerHtml);
        Assert.Contains("omni-busy", busy.ClassName, StringComparison.Ordinal);
        Assert.Equal("true", busy.GetAttribute("aria-busy"));
        Assert.False(busy.HasAttribute("disabled"));
        Assert.Empty(button.FindAll("[class*='busy'] [class*='busy']"));
    }

    [Fact]
    public void Button_IgnoresClicksAndCancelsTheDefaultActionOnlyWhileBusy()
    {
        var clicks = 0;
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.ButtonType, OmniButtonType.Submit)
            .Add(component => component.OnClick, (MouseEventArgs _) => clicks++)
            .AddChildContent("Send"));

        button.Find("button").Click();
        Assert.Equal(0, clicks);
        Assert.True(button.Find("button").HasAttribute("blazor:onclick:preventDefault"));

        button.Render(parameters => parameters.Add(component => component.Busy, false));
        button.Find("button").Click();
        Assert.Equal(1, clicks);
        Assert.False(button.Find("button").HasAttribute("blazor:onclick:preventDefault"));
    }

    [Fact]
    public void Button_DisabledStillDisablesIndependentlyOfBusy()
    {
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Disabled, true)
            .AddChildContent("Save"));

        Assert.True(button.Find("button").HasAttribute("disabled"));
        Assert.DoesNotContain("omni-busy", button.Find("button").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void SplitButton_VeilsBothHalvesAndKeepsItsMenuClosedWhileBusy()
    {
        var clicks = 0;
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Publish")
            .Add(component => component.Busy, true)
            .Add(component => component.OnClick, () => clicks++)
            .AddChildContent("<button type=\"button\">Draft</button>"));
        var main = split.Find(".omni-split-button__main");
        var toggle = split.Find(".omni-split-button__toggle");

        Assert.Contains("omni-busy", main.ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-busy", toggle.ClassName, StringComparison.Ordinal);
        Assert.Equal("true", main.GetAttribute("aria-busy"));
        Assert.False(main.HasAttribute("disabled"));
        Assert.False(toggle.HasAttribute("disabled"));

        main.Click();
        toggle.Click();
        split.Find(".omni-split-button").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.Equal(0, clicks);
        Assert.Empty(split.FindAll("[role='menu']"));
        Assert.Equal("false", split.Find(".omni-split-button__toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void ToggleButton_KeepsItsValueAndGainsTheVeilWhileBusy()
    {
        var changes = 0;
        var toggle = Render<OmniToggleButton>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.ValueChanged, (bool _) => changes++)
            .AddChildContent("Pin"));
        var button = toggle.Find("button");

        Assert.Contains("omni-busy", button.ClassName, StringComparison.Ordinal);
        Assert.Equal("true", button.GetAttribute("aria-busy"));
        Assert.False(button.HasAttribute("disabled"));

        button.Click();
        Assert.Equal(0, changes);
    }
}
