using Bunit;
using OmniEurope.Blazor.Components;
using Row = OmniEurope.Blazor.Tests.DataGridRowsTestHost.Row;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The detach cycle of the frozen columns (docs/data-components.md, "Détacher les colonnes figées"):
/// frozen at the start with no control, frozen and scrolled with the control offered, detached until
/// the control is pressed again or the table returns to its start. The scroll crossings are the ones
/// omni-grid.js reports through <see cref="OmniDataGrid{TItem}.OnHorizontalScrollChangedAsync"/>.
/// </summary>
public sealed class DataGridFrozenDetachTests : OmniBunitContext
{
    private const string GridModule = "./_content/OmniEurope.Blazor/omni-grid.js";
    private const string Toggle = ".omni-data-grid__frozen-toggle";

    [Fact]
    public void FrozenAtStart_RendersTheControlHidden()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));

        var toggle = host.Find(Toggle);
        Assert.True(toggle.HasAttribute("hidden"));
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.Equal("button", toggle.GetAttribute("type"));
        Assert.DoesNotContain("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ScrolledSideways_ShowsTheDetachControlWithLocalizedTexts()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));

        await ScrollAsync(host, true);

        var toggle = host.Find(Toggle);
        Assert.False(toggle.HasAttribute("hidden"));
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.Equal("Détacher les colonnes figées", toggle.GetAttribute("aria-label"));
        Assert.Equal("Détacher les colonnes figées", toggle.GetAttribute("title"));
    }

    [Fact]
    public async Task Click_DetachesThenFreezesAgainWithAStableName()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));
        await ScrollAsync(host, true);

        host.Find(Toggle).Click();

        var toggle = host.Find(Toggle);
        Assert.Equal("true", toggle.GetAttribute("aria-pressed"));
        Assert.Equal("Détacher les colonnes figées", toggle.GetAttribute("aria-label"));
        Assert.Equal("Refiger les colonnes", toggle.GetAttribute("title"));
        Assert.Contains("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);

        host.Find(Toggle).Click();

        toggle = host.Find(Toggle);
        Assert.False(toggle.HasAttribute("hidden"));
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.Equal("Détacher les colonnes figées", toggle.GetAttribute("title"));
        Assert.DoesNotContain("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Detached_StaysDetachedWhileScrollingSidewaysAgain()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));
        await ScrollAsync(host, true);
        host.Find(Toggle).Click();

        // A repeated report of the same side (the script only reports crossings, but a stale call
        // must not flip anything either) and a vertical re-render leave the detachment alone.
        await ScrollAsync(host, true);
        host.Find(".omni-pager button[aria-label=\"Page suivante\"]").Click();

        Assert.Equal("true", host.Find(Toggle).GetAttribute("aria-pressed"));
        Assert.Contains("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReturningToTheStart_FreezesAgainAndTheNextScrollStartsFrozen()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));
        await ScrollAsync(host, true);
        host.Find(Toggle).Click();

        await ScrollAsync(host, false);

        var toggle = host.Find(Toggle);
        Assert.True(toggle.HasAttribute("hidden"));
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.DoesNotContain("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);

        await ScrollAsync(host, true);

        toggle = host.Find(Toggle);
        Assert.False(toggle.HasAttribute("hidden"));
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));
        Assert.DoesNotContain("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void HiddenControl_ClickedAnyway_DoesNotDetach()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));

        host.Find(Toggle).Click();

        Assert.Equal("false", host.Find(Toggle).GetAttribute("aria-pressed"));
        Assert.DoesNotContain("omni-data-grid--frozen-detached", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void AlreadyScrolledWhenAttached_ShowsTheControlAtOnce()
    {
        var module = JSInterop.SetupModule(GridModule);
        module.Setup<bool>("attachFrozenScroll", _ => true).SetResult(true);

        var host = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));

        Assert.Single(module.Invocations["attachFrozenScroll"]);
        Assert.False(host.Find(Toggle).HasAttribute("hidden"));
    }

    [Fact]
    public void NoFrozenColumn_RendersNoControlAndWatchesNothing()
    {
        var module = JSInterop.SetupModule(GridModule);

        var host = Render<DataGridRowsTestHost>();

        Assert.Empty(host.FindAll(Toggle));
        Assert.Empty(module.Invocations["attachFrozenScroll"]);
        Assert.DoesNotContain("omni-data-grid--has-frozen", host.Find(".omni-data-grid").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dispose_StopsWatchingTheHorizontalScroll()
    {
        var module = JSInterop.SetupModule(GridModule);
        Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.FreezeName, true));

        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["detachFrozenScroll"]);
    }

    private static Task ScrollAsync(IRenderedComponent<DataGridRowsTestHost> host, bool scrolled)
    {
        var grid = host.FindComponent<OmniDataGrid<Row>>();
        return grid.InvokeAsync(() => grid.Instance.OnHorizontalScrollChangedAsync(scrolled));
    }
}
