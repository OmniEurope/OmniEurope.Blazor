using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Showcase.Components.Pages;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Renders the theme customizer, the one page of the showcase that is not a demonstration.
/// </summary>
/// <remarks>
/// The gallery pages are covered by rendering every demonstration; this page is not in that
/// catalogue, so without this it would be the only screen of the site nothing renders. It also
/// proves the palette catalogue actually reaches the markup rather than merely existing in memory.
/// </remarks>
public sealed class ShowcaseCustomizerTests : OmniBunitContext
{
    public ShowcaseCustomizerTests()
    {
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
    }

    [Fact]
    public void Customizer_Renders()
    {
        var page = Render<Customizer>();

        Assert.False(string.IsNullOrWhiteSpace(page.Markup));
        Assert.Contains("customize-title", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Customizer_OffersEveryPaletteOfTheCatalogue()
    {
        var page = Render<Customizer>();

        foreach (var preset in ThemePresets.All)
        {
            Assert.Contains(preset.Name, page.Markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Customizer_StaysWithinTheContentSecurityPolicy()
    {
        var page = Render<Customizer>();

        Assert.DoesNotContain("style=", page.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<style", page.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", page.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
