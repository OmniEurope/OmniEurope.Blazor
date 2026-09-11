using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class LoadingBarTests : OmniBunitContext
{
    [Fact]
    public void LoadingBar_TakesNoPlaceOutsideALoad()
    {
        var bar = Render<OmniLoadingBar>();

        Assert.Empty(bar.FindAll(".omni-loading-bar__track"));
        Assert.Null(bar.Find(".omni-loading-bar").GetAttribute("role"));
    }

    [Fact]
    public void LoadingBar_EndedLoad_GoesToTheEndBeforeLeaving()
    {
        var state = Services.GetRequiredService<OmniLoadingState>();
        var bar = Render<OmniLoadingBar>(parameters => parameters.Add(component => component.Mode, OmniLoadingBarMode.Continuous));

        state.Begin();
        bar.WaitForAssertion(() => Assert.Single(bar.FindAll(".omni-loading-bar__track")));
        Assert.DoesNotContain("omni-loading-bar--done", bar.Find(".omni-loading-bar").ClassName, StringComparison.Ordinal);

        state.End();

        // Right after the load, the bar is still drawn, marked as finishing, and carries no bucket
        // class that would pin it at the width the curve had reached.
        bar.WaitForAssertion(() => Assert.Contains("omni-loading-bar--done", bar.Find(".omni-loading-bar").ClassName, StringComparison.Ordinal));
        Assert.Equal("omni-loading-bar__indicator", bar.Find(".omni-loading-bar__indicator").ClassName);

        bar.WaitForAssertion(
            () => Assert.Empty(bar.FindAll(".omni-loading-bar__track")),
            TimeSpan.FromSeconds(5));
        Assert.DoesNotContain("omni-loading-bar--done", bar.Find(".omni-loading-bar").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadingBar_NewLoadDuringTheFinish_TakesTheBarBack()
    {
        var state = Services.GetRequiredService<OmniLoadingState>();
        var bar = Render<OmniLoadingBar>();

        state.Begin();
        state.End();
        bar.WaitForAssertion(() => Assert.Contains("omni-loading-bar--done", bar.Find(".omni-loading-bar").ClassName, StringComparison.Ordinal));

        state.Begin();

        bar.WaitForAssertion(() => Assert.DoesNotContain("omni-loading-bar--done", bar.Find(".omni-loading-bar").ClassName, StringComparison.Ordinal));
        Assert.Contains("omni-loading-bar--active", bar.Find(".omni-loading-bar").ClassName, StringComparison.Ordinal);
        Assert.Single(bar.FindAll(".omni-loading-bar__track"));
    }
}
