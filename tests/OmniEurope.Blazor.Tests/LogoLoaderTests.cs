using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The loading indicator drawn with the site's logo: a status said in words, a decorative mark that
/// floats and never turns, and a still mark for less motion.
/// </summary>
public sealed class LogoLoaderTests : OmniBunitContext
{
    [Fact]
    public void TheLoader_IsAStatusSaidInWords_AroundADecorativeLogo()
    {
        var loader = Render<OmniLogoLoader>(parameters => parameters
            .Add(component => component.Size, OmniControlSize.Large)
            .AddChildContent("<img src=\"logo.svg\" alt=\"\" />"));

        var root = loader.Find(".omni-logo-loader");
        Assert.Equal("status", root.GetAttribute("role"));
        Assert.Contains("omni-logo-loader--large", root.ClassList);
        Assert.Equal("true", loader.Find(".omni-logo-loader__mark").GetAttribute("aria-hidden"));
        Assert.NotNull(loader.Find(".omni-logo-loader__mark > img"));
        Assert.Equal("Chargement en cours", loader.Find(".omni-visually-hidden").TextContent);

        var named = Render<OmniLogoLoader>(parameters => parameters
            .Add(component => component.Label, "Chargement du tableau de bord")
            .AddChildContent("<svg></svg>"));
        Assert.Equal("Chargement du tableau de bord", named.Find(".omni-visually-hidden").TextContent);
    }

    [Fact]
    public void TheLogo_Floats_NeverTurns_AndStandsStillForLessMotion()
    {
        var mark = ShippedLookTests.Body(".omni-logo-loader__mark");
        Assert.StartsWith("omni-float ", ShippedLookTests.Value(mark, "animation"), StringComparison.Ordinal);

        var css = StylesheetSource.Read();
        var keyframes = System.Text.RegularExpressions.Regex.Match(css, @"@keyframes omni-float \{[^@]*?\}\s*\}");
        Assert.True(keyframes.Success, "No @keyframes omni-float in the stylesheet.");
        Assert.Contains("translateY", keyframes.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("rotate", keyframes.Value, StringComparison.Ordinal);
        Assert.Contains("    .omni-logo-loader__mark { animation: none; }", ReducedMotionBlock(css), StringComparison.Ordinal);
    }

    // The reduced-motion block of the loader: from its media query to its closing brace.
    private static string ReducedMotionBlock(string css)
    {
        var start = css.IndexOf("@media (prefers-reduced-motion: reduce) {\n    .omni-logo-loader__mark", StringComparison.Ordinal);
        Assert.True(start >= 0, "No reduced-motion block for the logo loader.");
        return css[start..css.IndexOf("\n}", start, StringComparison.Ordinal)];
    }

    [Fact]
    public void ALogoThatAnimatesItself_PlaysItsOwnAnimation_AndHoldsStillForLessMotion()
    {
        // Aetheus, 2026-10-03: an animated plane must play its own keyframes rather than float.
        var animated = Render<OmniLogoLoader>(parameters => parameters
            .Add(component => component.AnimatedMark, true)
            .AddChildContent("<svg class=\"plane\"></svg>"));
        var plain = Render<OmniLogoLoader>(parameters => parameters.AddChildContent("<svg></svg>"));

        Assert.Contains("omni-logo-loader--animated", animated.Find(".omni-logo-loader").ClassList);
        Assert.DoesNotContain("omni-logo-loader--animated", plain.Find(".omni-logo-loader").ClassList);
        Assert.Equal("status", animated.Find(".omni-logo-loader").GetAttribute("role"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-logo-loader--animated .omni-logo-loader__mark"), "animation"));
        Assert.Contains(".omni-logo-loader--animated .omni-logo-loader__mark * { animation: none !important; }", ReducedMotionBlock(StylesheetSource.Read()), StringComparison.Ordinal);
    }
}
