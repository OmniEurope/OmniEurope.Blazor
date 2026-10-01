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
        Assert.Contains("@media (prefers-reduced-motion: reduce) { .omni-logo-loader__mark { animation: none; } }", css, StringComparison.Ordinal);
    }
}
