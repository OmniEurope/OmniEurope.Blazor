using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Layout and page components in the shapes their own suites leave out: containers without content, a
/// linked brand with its logo, column spans past the grid, a plain tile without icon or detail, a trail
/// followed by a listener, and scripts released on a lost circuit or after their owner went.
/// </summary>
public sealed class LayoutAndPagesBranchTests : OmniBunitContext
{
    [Fact]
    public void Containers_WithoutContent_DrawTheirFrameOnly()
    {
        var row = Render<OmniRow>();
        var main = Render<OmniMain>();
        var scrolling = Render<OmniMain>(parameters => parameters.Add(component => component.Scrollable, true));

        Assert.Equal(string.Empty, row.Find(".omni-row").TextContent.Trim());
        Assert.Equal(string.Empty, main.Find("main").TextContent.Trim());
        Assert.NotNull(scrolling.Find(".omni-main-scroll > main.omni-main--scrollable"));
    }

    [Fact]
    public void Header_LinkedBrandWithALogo_DrawsTheLogoInsideTheLink()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Omni")
            .Add(component => component.BrandLogo, "/logo.svg")
            .Add(component => component.BrandHref, "/"));

        Assert.Equal("/logo.svg", header.Find("a.omni-header__brand--link img.omni-header__logo-image").GetAttribute("src"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Column_SpanOutsideTheTwelveColumns_IsRefused(int span) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniColumn>(parameters => parameters.Add(component => component.LargeSpan, span)));

    [Fact]
    public void StatTile_WithoutIconOrDetail_DrawsItsValueAndLabelOnly()
    {
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "12")
            .Add(component => component.Label, "Factures"));

        Assert.Empty(tile.FindAll(".omni-stat-tile__icon"));
        Assert.Empty(tile.FindAll(".omni-stat-tile__detail"));
        Assert.Equal("Factures", tile.Find(".omni-stat-tile__label").TextContent);
    }

    [Fact]
    public void BreadcrumbPush_TellsItsListener()
    {
        var service = new OmniBreadcrumbService(Services.GetRequiredService<NavigationManager>());
        var changes = 0;
        service.Changed += () => changes++;

        service.Push(new OmniBreadcrumbEntry("Accueil", "/"));

        Assert.Equal(1, changes);
        service.Dispose();
    }

    [Fact]
    public void PageHeader_UntitledOnALoadingCrumb_WaitsForIt_WithItsTrailSwitchedOff_AndBadgesWithoutActions()
    {
        var trail = Services.GetRequiredService<OmniBreadcrumbService>();
        trail.Set(new OmniBreadcrumbEntry("Accueil", "/"), new OmniBreadcrumbEntry("Facture", "/factures/1", Loading: true));
        RenderFragment badges = builder => builder.AddContent(0, "Payée");

        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.ShowTrail, false)
            .Add(component => component.Badges, badges));

        Assert.Empty(header.FindAll(".omni-page-header__trail nav, .omni-page-header__trail ol"));
        Assert.Equal("Payée", header.Find(".omni-page-header__badges").TextContent);
        Assert.Empty(header.FindAll(".omni-page-header__actions"));
        Assert.Single(header.FindAll(".omni-page-header__title-loading"));

        trail.Set(new OmniBreadcrumbEntry("Accueil", "/"), new OmniBreadcrumbEntry("Facture 1", "/factures/1"));
        header.WaitForAssertion(() => Assert.Equal("Facture 1", header.Find(".omni-page-header__title").TextContent.Trim()));
    }

    [Fact]
    public void PageHeader_WithoutTitleNorTrail_HasAnEmptyTitle()
    {
        Services.RemoveAll<OmniBreadcrumbService>();

        var header = Render<OmniPageHeader>();

        Assert.Equal(string.Empty, header.Find(".omni-page-header__title").TextContent.Trim());
    }

    [Fact]
    public void DetailShell_WithoutBackText_SaysTheDefaultOne()
    {
        var shell = Render<OmniDetailShell>(parameters => parameters
            .Add(component => component.State, OmniDetailState.NotFound)
            .Add(component => component.BackHref, "/factures"));

        Assert.Contains("Retour", shell.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FieldsetAndStack_ReleasingTheirScriptOnALostCircuit_AreQuiet()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var fieldset = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, (RenderFragment)(builder => builder.AddContent(0, "Adresse")))
            .Add(component => component.Collapsible, true)
            .Add(component => component.ExpandedChanged, _ => { }));
        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.Overflow, OmniStackOverflow.Scroll)
            .AddChildContent("Contenu"));

        await fieldset.Instance.DisposeAsync();
        await stack.Instance.DisposeAsync();

        Assert.Contains("disposeScrollOverflow", runtime.Module.Calls);
    }

    [Fact]
    public async Task BootSplashGoneWhileItsScriptLoads_StillRemovesTheSplash_AndReportsNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var reported = new List<bool>();
        var splash = Render<OmniBootSplash>(parameters => parameters.Add(component => component.OnHidden, hidden => reported.Add(hidden)));

        await splash.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        // The splash is hidden, then the module released at once since nobody is left to keep it.
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Contains("hideBootSplash", runtime.Module.Calls);
        Assert.Empty(reported);
    }
}
