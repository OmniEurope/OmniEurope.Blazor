using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using static OmniEurope.Blazor.Tests.ShippedLookTests;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniButtonVariant.GhostDanger"/>: the quiet button in the danger text colour. The markup is
/// rendered; the look is read from the stylesheet and held to the rules of Ghost (flat, no fill) and of the
/// danger text (the colour the theme contrast matrix measures on the page and on both ghost tints).
/// </summary>
public sealed class GhostDangerButtonTests : OmniBunitContext
{
    [Fact]
    public void GhostDanger_IsAppendedAfterEveryPublishedVariant()
    {
        Assert.Equal(7, (int)OmniButtonVariant.GhostDanger);
        Assert.Equal(OmniButtonVariant.Primary, new OmniButton().Variant);
    }

    [Fact]
    public void Button_RendersTheGhostDangerModifierAndNotTheDangerFill()
    {
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Variant, OmniButtonVariant.GhostDanger)
            .AddChildContent("Abandon"));

        var classes = button.Find("button").ClassList;
        Assert.Contains("omni-button--ghostdanger", classes);
        Assert.DoesNotContain("omni-button--danger", classes);
        Assert.DoesNotContain("omni-button--ghost", classes);
    }

    [Fact]
    public void Button_StillRaisesOnClickAndHonoursDisabled()
    {
        var clicks = 0;
        var button = Render<OmniButton>(parameters => parameters
            .Add(component => component.Variant, OmniButtonVariant.GhostDanger)
            .Add(component => component.OnClick, (MouseEventArgs _) => clicks++)
            .AddChildContent("Abandon"));

        button.Find("button").Click();
        Assert.Equal(1, clicks);

        button.Render(parameters => parameters.Add(component => component.Disabled, true));
        Assert.True(button.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void SplitButton_RendersTheGhostDangerModifier()
    {
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Abandon")
            .Add(component => component.Variant, OmniButtonVariant.GhostDanger));

        Assert.Contains("omni-split-button--ghostdanger", split.Find(".omni-split-button").ClassList);
    }

    [Fact]
    public void GhostDanger_IsFlatAndBodyLessInTheDangerTextColour()
    {
        var quiet = Body(".omni-button--ghostdanger");

        Assert.Equal("transparent", Value(quiet, "--omni-button-fill"));
        Assert.Equal("var(--omni-color-danger)", Value(quiet, "--omni-button-ink"));
        Assert.Equal("0 0 #0000", Value(quiet, "--omni-button-shadow"));
        Assert.Equal("transparent", Value(quiet, "border-color"));
        Assert.NotEqual(Value(Body(".omni-button--ghost"), "--omni-button-ink"), Value(quiet, "--omni-button-ink"));

        var press = Body(".omni-button--ghostdanger:active:not(:disabled)");
        Assert.Contains("box-shadow: 0 0 #0000", press, StringComparison.Ordinal);
        Assert.StartsWith("color-mix(in srgb, var(--omni-color-danger)", Value(press, "border-color"), StringComparison.Ordinal);
    }

    [Fact]
    public void GhostDanger_TintsOnHoverAndPressOnlyWithAFillTheDangerTextHoldsItsRatioOn()
    {
        var quiet = Body(".omni-button--ghostdanger");

        // The danger text colour on the danger tint is a pair of the theme contrast matrix (every theme, palette
        // and mode); the page tints of Ghost are not, since the danger text falls below the text ratio on them.
        Assert.Equal("var(--omni-color-danger-subtle)", Value(quiet, "--omni-button-fill-hover"));
        Assert.Equal("var(--omni-color-danger-subtle)", Value(quiet, "--omni-button-fill-active"));
        Assert.Contains(ThemeContrastMatrixTests.Pairs, pair => pair is ("--omni-color-danger", "--omni-color-danger-subtle", >= 4.5));
        Assert.Contains(ThemeContrastMatrixTests.Pairs, pair => pair is ("--omni-color-danger", "--omni-color-surface", >= 4.5));
    }

    [Fact]
    public void SplitGhostDanger_HasTheColoursOfTheButtonAndStaysFlat()
    {
        var button = Body(".omni-button--ghostdanger");
        var split = Body(".omni-split-button--ghostdanger");
        foreach (var token in new[] { "--omni-button-fill", "--omni-button-fill-hover", "--omni-button-fill-active", "--omni-button-ink" })
        {
            Assert.Equal(Value(button, token), Value(split, token));
        }

        const string Parts = ".omni-split-button--ghostdanger > :is(.omni-split-button__main, .omni-split-button__toggle)";
        Assert.Contains(Rules(), rule => rule.Selector.EndsWith(Parts, StringComparison.Ordinal) && rule.Body.Contains("border-color: transparent", StringComparison.Ordinal));
        Assert.Contains(Rules(), rule => rule.Selector.EndsWith(Parts + ":active:not(:disabled)", StringComparison.Ordinal) && rule.Body.Contains("box-shadow: 0 0 #0000", StringComparison.Ordinal));
        Assert.Contains(".omni-split-button:not(.omni-split-button--ghost):not(.omni-split-button--ghostdanger) > .omni-split-button__toggle", Rules().Select(rule => rule.Selector));
    }
}
