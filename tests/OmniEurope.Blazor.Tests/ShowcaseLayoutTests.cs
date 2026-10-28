using System.Net;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Components.Layout;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The shell of the showcase (STD-SHELL): the layout renders the library's own pieces, the sidebar toggle,
/// the application menu at the end of the header and the appearance window, and they write the same
/// <see cref="ThemeState"/> as the customizer. A bUnit render proves the markup and the wiring to the state,
/// not the browser: the push or overlay of the sidebar and the breakpoint are checked in a real browser.
/// </summary>
public sealed class ShowcaseLayoutTests : OmniBunitContext
{
    private const string Css = ":root { --omni-color-accent: #2563eb; }";

    public ShowcaseLayoutTests()
    {
        Services.AddScoped(_ => new HttpClient(new StylesheetHandler(Css)) { BaseAddress = new Uri("https://localhost/") });
        Services.AddScoped<ThemeTokenReader>();
        Services.AddScoped<ThemeState>();
        Services.AddScoped<ShowcaseLanguageSwitcher>();
    }

    [Fact]
    public void Shell_ComesFromTheLibrary_WithTheToggleThenTheBrandAndTheAppMenuAtTheEndOfTheBar() =>
        ShowcaseLanguageTests.WithUiCulture("en-IE", () =>
    {
        var layout = RenderLayout();

        var header = layout.Find("header.omni-header");
        var children = header.Children.Select(child => child.ClassName ?? string.Empty).ToArray();
        Assert.StartsWith("omni-sidebar-toggle", children[0], StringComparison.Ordinal);
        Assert.Contains("omni-header__brand", children[1], StringComparison.Ordinal);
        Assert.Contains("omni-app-menu", children[^1], StringComparison.Ordinal);
        Assert.Equal("OmniEurope.Blazor", layout.Find("a.omni-header__brand").TextContent.Trim());
        Assert.Contains("Component library", header.TextContent, StringComparison.Ordinal);
        Assert.NotNull(layout.Find("a.omni-skip-link"));
        Assert.Equal("showcase-theme", layout.Find("[data-omni-theme]").Id);
    });

    [Fact]
    public void Toggle_ControlsTheSidebar_AndClosesAndReopensIt()
    {
        var layout = RenderLayout();
        var sidebar = layout.Find("#showcase-sidebar");
        var toggle = layout.Find("button.omni-sidebar-toggle");
        Assert.Equal("showcase-sidebar", toggle.GetAttribute("aria-controls"));
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        Assert.Contains("omni-sidebar--push", sidebar.ClassName, StringComparison.Ordinal);

        toggle.Click();

        Assert.Equal("false", layout.Find("button.omni-sidebar-toggle").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("omni-sidebar--open", layout.Find("#showcase-sidebar").ClassName, StringComparison.Ordinal);

        layout.Find("button.omni-sidebar-toggle").Click();

        Assert.Equal("true", layout.Find("button.omni-sidebar-toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void OnAPhone_TheSidebarOverlaysThePageWithItsVeil_AndStartsClosed()
    {
        JSInterop.Setup<bool>("omniShowcaseLayout.narrow").SetResult(true);

        var layout = RenderLayout();

        layout.WaitForAssertion(() =>
        {
            var sidebar = layout.Find("#showcase-sidebar");
            Assert.Contains("omni-sidebar--overlay", sidebar.ClassName, StringComparison.Ordinal);
            Assert.DoesNotContain("omni-sidebar--open", sidebar.ClassName, StringComparison.Ordinal);
            Assert.Equal("false", layout.Find("button.omni-sidebar-toggle").GetAttribute("aria-expanded"));
        });
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "omniShowcaseLayout.watch");
    }

    [Fact]
    public void TheSiteMenu_ShowsOutsideTheGallery_AndTheGalleryMenuReplacesItInside() =>
        ShowcaseLanguageTests.WithUiCulture("en-IE", () =>
    {
        var layout = RenderLayout();
        var labels = layout.FindAll("#showcase-sidebar .omni-panel-menu__text").Select(text => text.TextContent.Trim()).ToArray();
        Assert.Equal(["Overview", "Documentation", "Components", "Customisation"], labels);

        Services.GetRequiredService<NavigationManager>().NavigateTo("components/theme");

        layout.WaitForAssertion(() =>
        {
            var inGallery = layout.FindAll("#showcase-sidebar .omni-panel-menu__text").Select(text => text.TextContent.Trim()).ToArray();
            Assert.Contains("Back to the site", inGallery);
            Assert.DoesNotContain("Documentation", inGallery);
        });
    });

    [Fact]
    public void TheModeButtonsOfTheAppMenu_WriteTheShowcaseTheme()
    {
        var layout = RenderLayout();
        var state = Services.GetRequiredService<ThemeState>();
        layout.Find("button.omni-app-menu__trigger").Click();

        layout.FindAll(".omni-app-menu__mode [role=radio]")[1].Click();

        Assert.Equal(ThemeMode.Dark, state.Mode);
        Assert.Equal("dark", layout.Find("#showcase-theme").GetAttribute("data-omni-theme"));
    }

    [Fact]
    public void TheThemeRowOfTheAppMenu_OpensTheAppearanceWindow_ThatDrivesTheSameState()
    {
        var layout = RenderLayout();
        var state = Services.GetRequiredService<ThemeState>();
        Assert.Empty(layout.FindAll(".omni-appearance-window"));
        layout.Find("button.omni-app-menu__trigger").Click();

        layout.Find(".omni-app-menu__theme").Click();

        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll(".omni-appearance-window")));
        var themes = layout.FindAll(".omni-appearance-window select")[0];
        var galet = themes.QuerySelectorAll("option").Single(option => option.TextContent.StartsWith("Galet", StringComparison.Ordinal));
        themes.Change(galet.GetAttribute("value")!);

        Assert.Equal("Galet", state.Theme.Name);
        Assert.True(state.HasThemePalette);
    }

    [Fact]
    public void TheLanguageRowOfTheAppMenu_ListsBothLanguages_AndSavesThePickedOne()
    {
        JSInterop.Setup<bool>("omniShowcaseCulture.save", ShowcaseLanguages.StorageKey, "fr").SetResult(true);
        ShowcaseLanguageTests.WithUiCulture("en-IE", () =>
        {
            var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
            var layout = RenderLayout();
            layout.Find("button.omni-app-menu__trigger").Click();

            var select = layout.Find(".omni-app-menu__language-row select");
            Assert.Equal(["English", "Français"], select.QuerySelectorAll("option").Select(option => option.TextContent).ToArray());
            Assert.Equal("English", select.QuerySelector("option[selected]")?.TextContent);

            select.Change(select.QuerySelectorAll("option").Single(option => option.TextContent == "Français").GetAttribute("value")!);

            Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "omniShowcaseCulture.save");
            var reload = Assert.Single(navigation.History);
            Assert.True(reload.Options.ForceLoad);
        });
    }

    [Fact]
    public void TheAppMenu_CarriesTheVersionOfTheLibrary()
    {
        ShowcaseLanguageTests.WithUiCulture("en-IE", () =>
        {
            var layout = RenderLayout();
            layout.Find("button.omni-app-menu__trigger").Click();

            Assert.Matches(@"^Version \d+\.\d+\.\d+$", layout.Find(".omni-app-menu__version").TextContent.Trim());
        });
    }

    private IRenderedComponent<MainLayout> RenderLayout() =>
        Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p id=\"page\">page</p>"))));

    /// <summary>Answers the stylesheet request of the token reader, the one call the layout's first render makes.</summary>
    private sealed class StylesheetHandler(string css) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(css) });
    }
}
