using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The page veil (recette R1-18): an opaque, fixed, constant-coloured layer with the application's own
/// logo at its centre, given as free markup or as an image address, shown only after 100 ms of waiting,
/// and animating nothing.
/// </summary>
public sealed class PageVeilTests : OmniBunitContext
{
    private static readonly RenderFragment Mark = builder =>
    {
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "app-logo");
        builder.CloseElement();
    };

    [Fact]
    public void NotBusy_RendersNothing()
    {
        var veil = Render<OmniPageVeil>();

        Assert.Empty(veil.Markup.Trim());
        Assert.Empty(veil.FindAll(".omni-page-veil"));
    }

    [Fact]
    public void Busy_IsAPoliteStatusSaidInWords_WithNoLogoByDefault()
    {
        var veil = Render<OmniPageVeil>(parameters => parameters.Add(component => component.Busy, true));

        var root = veil.Find(".omni-page-veil");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Equal("Chargement en cours", root.QuerySelector(".omni-visually-hidden")!.TextContent);
        Assert.Empty(veil.FindAll(".omni-page-veil__logo"));
        Assert.Empty(veil.FindAll("img"));
        Assert.Empty(veil.FindAll("svg"));
    }

    [Fact]
    public void TheLabel_IsTheHostsOrTheLocalizedDefault()
    {
        var named = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.Label, "Ouverture du dossier"));
        Assert.Equal("Ouverture du dossier", named.Find(".omni-visually-hidden").TextContent);

        var blank = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.Label, "  "));
        Assert.Equal("Chargement en cours", blank.Find(".omni-visually-hidden").TextContent);
    }

    [Theory]
    [InlineData("fr-FR", "Chargement en cours")]
    [InlineData("en-US", "Loading")]
    [InlineData("de-DE", "Wird geladen")]
    public void TheDefaultLabel_FollowsTheUiCulture(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var veil = Render<OmniPageVeil>(parameters => parameters.Add(component => component.Busy, true));

            Assert.Equal(expected, veil.Find(".omni-visually-hidden").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void TheLogo_AsFreeMarkup_IsCentredAndDecorative()
    {
        var veil = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.Logo, Mark));

        var logo = veil.Find(".omni-page-veil__logo");
        Assert.Equal("true", logo.GetAttribute("aria-hidden"));
        Assert.NotNull(logo.QuerySelector("svg.app-logo"));
        Assert.Empty(veil.FindAll("img"));
    }

    [Fact]
    public void TheLogo_AsAnImageAddress_IsAnImgWithNoAlternativeText()
    {
        var veil = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.LogoSource, "/images/logo.svg"));

        var image = veil.Find(".omni-page-veil__logo > img");
        Assert.Equal("/images/logo.svg", image.GetAttribute("src"));
        Assert.Equal(string.Empty, image.GetAttribute("alt"));
        Assert.Equal("true", veil.Find(".omni-page-veil__logo").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void WhenBothLogosAreGiven_TheMarkupWins_AndNoImageIsDrawn()
    {
        var veil = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.Logo, Mark)
            .Add(component => component.LogoSource, "/images/logo.svg"));

        Assert.NotNull(veil.Find(".omni-page-veil__logo svg.app-logo"));
        Assert.Empty(veil.FindAll("img"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankImageAddress_DrawsNoLogo(string? source)
    {
        var veil = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.LogoSource, source));

        Assert.Empty(veil.FindAll(".omni-page-veil__logo"));
        Assert.Empty(veil.FindAll("img"));
    }

    [Fact]
    public void TheVeil_ExistsExactlyWhileBusy_SoAShortWaitRemovesItBeforeItWasEverShown()
    {
        var veil = Render<OmniPageVeil>(parameters => parameters.Add(component => component.Busy, true));
        Assert.Single(veil.FindAll(".omni-page-veil"));

        veil.Render(parameters => parameters.Add(component => component.Busy, false));
        Assert.Empty(veil.FindAll(".omni-page-veil"));

        veil.Render(parameters => parameters.Add(component => component.Busy, true));
        Assert.Single(veil.FindAll(".omni-page-veil"));
    }

    [Fact]
    public void TheIdTheClassAndTheAttributes_GoOnTheOutermostElement()
    {
        var veil = Render<OmniPageVeil>(parameters => parameters
            .Add(component => component.Busy, true)
            .Add(component => component.Id, "page-wait")
            .Add(component => component.Class, "app-veil")
            .AddUnmatched("data-testid", "veil"));

        var root = veil.Find("#page-wait");
        Assert.Contains("omni-page-veil", root.ClassList);
        Assert.Contains("app-veil", root.ClassList);
        Assert.Equal("veil", root.GetAttribute("data-testid"));
        Assert.Equal("status", root.GetAttribute("role"));
    }

    [Fact]
    public void TheVeil_IsOpaque_Fixed_OfOneThemeColour_AndShowsOnlyAfterTheDelay()
    {
        var rule = ShippedLookTests.Body(".omni-page-veil");

        Assert.Equal("fixed", ShippedLookTests.Value(rule, "position"));
        Assert.Equal("0", ShippedLookTests.Value(rule, "inset"));
        Assert.Equal("var(--omni-color-surface)", ShippedLookTests.Value(rule, "background"));
        Assert.Equal("var(--omni-z-overlay)", ShippedLookTests.Value(rule, "z-index"));

        // The 100 ms delay: a zero-length animation whose first keyframe (hidden) is held during the delay
        // only, so the veil is never visible before it and never fades in.
        Assert.Equal(
            "omni-page-veil-wait 0s linear var(--omni-page-veil-delay, 100ms) backwards",
            ShippedLookTests.Value(rule, "animation"));
        var keyframes = Regex.Match(StylesheetSource.Read(), @"@keyframes omni-page-veil-wait \{(?<body>[^@]*?\})\s*\}");
        Assert.True(keyframes.Success, "No @keyframes omni-page-veil-wait in the stylesheet.");
        Assert.Equal("from { visibility: hidden; }", Regex.Replace(keyframes.Groups["body"].Value.Trim(), @"\s+", " "));
    }

    [Fact]
    public void TheVeil_NeverAnimatesOpacity_NorTransitions_NorHasAnAnimatedLogo()
    {
        var css = ShippedLookTests.Uncommented(StylesheetSource.Read());
        var rules = Regex.Matches(css, @"(?<selector>[^{}]*\.omni-page-veil[^{}]*)\{(?<body>[^{}]*)\}")
            .Select(match => match.Groups["body"].Value)
            .ToArray();
        Assert.True(rules.Length >= 4, "The page veil rules were not found.");

        foreach (var body in rules)
        {
            Assert.DoesNotContain("opacity", body, StringComparison.Ordinal);
            Assert.DoesNotContain("transition", body, StringComparison.Ordinal);
            Assert.DoesNotContain("transform", body, StringComparison.Ordinal);
        }

        // The delay is the only animation, on the veil itself: the logo and a reduced-motion rule add none.
        Assert.Single(rules, body => body.Contains("animation", StringComparison.Ordinal));
    }
}
