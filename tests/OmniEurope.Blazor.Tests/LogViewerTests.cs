using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniLogViewer: the lines and their decorations, the level filter, the search and its steps, the
/// follow state with its jump button, the viewport script and the appended tail.
/// </summary>
public sealed class LogViewerTests : OmniBunitContext
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly OmniLogLine[] Mixed =
    [
        new("Démarrage", OmniLogLevel.Information, Noon),
        new("Détail du cache", OmniLogLevel.Debug),
        new("Disque presque plein", OmniLogLevel.Warning),
        new("Connexion perdue", OmniLogLevel.Error),
        new("Reprise de la connexion", OmniLogLevel.Information),
    ];

    private BunitJSModuleInterop Module() => JSInterop.SetupModule(OmniModules.LogViewer);

    private IRenderedComponent<OmniLogViewer> RenderViewer(IReadOnlyList<OmniLogLine> lines, Action<ComponentParameterCollectionBuilder<OmniLogViewer>>? extra = null) =>
        Render<OmniLogViewer>(parameters =>
        {
            parameters.Add(viewer => viewer.Lines, lines).Add(viewer => viewer.TimeZone, TimeZoneInfo.Utc);
            extra?.Invoke(parameters);
        });

    private static IReadOnlyList<string> Texts(IRenderedComponent<OmniLogViewer> viewer) =>
        [.. viewer.FindAll(".omni-log-viewer__text").Select(text => text.TextContent)];

    // ---- Parameters ----------------------------------------------------------------------------

    [Fact]
    public void RefusesMissingLinesOrTimeZoneAndAnUnknownHeight()
    {
        Assert.Throws<ArgumentNullException>(() => Render<OmniLogViewer>(parameters => parameters.Add(viewer => viewer.Lines, null!)));
        Assert.Throws<ArgumentNullException>(() => Render<OmniLogViewer>(parameters => parameters.Add(viewer => viewer.Lines, Mixed).Add(viewer => viewer.TimeZone, null!)));
        var error = Assert.Throws<ArgumentException>(() => RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.Height, "20 lines")));
        Assert.Equal("Height", error.ParamName);
    }

    [Theory]
    [InlineData("480px")]
    [InlineData("24.5rem")]
    [InlineData("60vh")]
    [InlineData("100%")]
    public void AcceptsACssHeight_AndHandsItToTheScript(string height)
    {
        var module = Module();
        RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.Height, height));

        Assert.Contains(module.Invocations, call => call.Identifier == "applyLayout" && Equals(call.Arguments[3], height));
    }

    // ---- Lines ---------------------------------------------------------------------------------

    [Fact]
    public void EmptyLog_SaysSo_WithItsDefaultOrOwnText()
    {
        Assert.Equal("Aucune ligne pour le moment.", RenderViewer([]).Find(".omni-log-viewer__empty").TextContent);
        Assert.Equal("Rien", RenderViewer([], parameters => parameters.Add(viewer => viewer.EmptyText, "Rien")).Find(".omni-log-viewer__empty").TextContent);
    }

    [Fact]
    public void Viewport_IsANamedLog()
    {
        Assert.Equal("Journal", RenderViewer(Mixed).Find("[role=log]").GetAttribute("aria-label"));
        Assert.Equal("Build", RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.Label, "Build")).Find("[role=log]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Lines_ShowNumberTimeAndTintedLevel()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.TimestampFormat, "HH:mm"));

        var rows = viewer.FindAll(".omni-log-viewer__line");
        Assert.Equal(5, rows.Count);
        Assert.Equal(["1", "2", "3", "4", "5"], viewer.FindAll(".omni-log-viewer__number").Select(number => number.TextContent));
        var time = viewer.Find("time.omni-log-viewer__time");
        Assert.Equal("12:00", time.TextContent);
        Assert.Equal("2026-10-04T12:00:00.000Z", time.GetAttribute("datetime"));
        Assert.Single(viewer.FindAll("time"));
        Assert.Contains("omni-log-viewer__line--quiet", rows[1].ClassList);
        Assert.Contains("omni-log-viewer__line--warning", rows[2].ClassList);
        Assert.Contains("omni-log-viewer__line--error", rows[3].ClassList);
        Assert.Equal(["omni-log-viewer__line"], rows[0].ClassList);
    }

    [Fact]
    public void Levels_AreShownOrOnlyNamedToScreenReadersForWarningsAndErrors()
    {
        var hidden = RenderViewer(Mixed);
        Assert.Empty(hidden.FindAll(".omni-log-viewer__level-name"));
        Assert.Equal(2, hidden.FindAll(".omni-log-viewer__line .omni-visually-hidden").Count);

        var shown = RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.ShowLevels, true));
        Assert.Equal(5, shown.FindAll(".omni-log-viewer__level-name").Count);
        Assert.Empty(shown.FindAll(".omni-log-viewer__line .omni-visually-hidden"));
    }

    [Fact]
    public void NumbersTimesAndWrap_FollowTheirSwitches()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowLineNumbers, false)
            .Add(viewer => viewer.ShowTimestamps, false)
            .Add(viewer => viewer.Wrap, true));

        Assert.Empty(viewer.FindAll(".omni-log-viewer__number"));
        Assert.Empty(viewer.FindAll("time"));
        Assert.Contains("omni-log-viewer--wrap", viewer.Find(".omni-log-viewer").ClassList);
    }

    [Fact]
    public void AppendedTail_KeepsTheLinesBefore_WhetherSameListOrNewOne()
    {
        var lines = new List<OmniLogLine>(Mixed);
        var viewer = RenderViewer(lines);

        lines.Add(new("Sixième", OmniLogLevel.Information));
        viewer.Render(parameters => parameters.Add(component => component.Lines, lines));
        Assert.Equal(6, Texts(viewer).Count);

        viewer.Render(parameters => parameters.Add(component => component.Lines, [.. lines, new OmniLogLine("Septième")]));
        Assert.Equal("Septième", Texts(viewer)[^1]);
    }

    [Fact]
    public void ShorterOrRewrittenHistory_StartsOver()
    {
        var viewer = RenderViewer(Mixed);

        viewer.Render(parameters => parameters.Add(component => component.Lines, Mixed[..2]));
        Assert.Equal(["Démarrage", "Détail du cache"], Texts(viewer));

        viewer.Render(parameters => parameters.Add(component => component.Lines, [new OmniLogLine("Autre"), new OmniLogLine("Encore")]));
        Assert.Equal(["Autre", "Encore"], Texts(viewer));
    }

    // ---- Level filter --------------------------------------------------------------------------

    [Fact]
    public void LevelFilter_HidesLessSevereLines_AndKeepsTheirNumbers()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.MinimumLevel, OmniLogLevel.Warning));

        Assert.Equal(["Disque presque plein", "Connexion perdue"], Texts(viewer));
        Assert.Equal(["3", "4"], viewer.FindAll(".omni-log-viewer__number").Select(number => number.TextContent));
    }

    [Fact]
    public void LevelFilterControl_ChangesTheLines_AndReportsTheLevel()
    {
        OmniLogLevel? reported = null;
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowLevelFilter, true)
            .Add(viewer => viewer.MinimumLevelChanged, level => reported = level));

        var select = viewer.Find("select.omni-log-viewer__level");
        Assert.Equal("Niveau minimal", select.GetAttribute("aria-label"));
        var errors = select.QuerySelectorAll("option").Single(option => option.TextContent.StartsWith("Erreur", StringComparison.Ordinal));
        select.Change(errors.GetAttribute("value"));

        Assert.Equal(OmniLogLevel.Error, reported);
        Assert.Equal(["Connexion perdue"], Texts(viewer));
    }

    // ---- Search --------------------------------------------------------------------------------

    [Fact]
    public void Search_MarksEveryOccurrence_IgnoringCaseAndAccents()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "CONNEXION"));

        Assert.Equal(["Connexion", "connexion"], viewer.FindAll("mark.omni-log-viewer__match").Select(mark => mark.TextContent));
        Assert.Equal("0 sur 2", viewer.Find(".omni-log-viewer__matches").TextContent);

        viewer.Render(parameters => parameters.Add(component => component.SearchText, "detail"));
        Assert.Equal(["Détail"], viewer.FindAll("mark").Select(mark => mark.TextContent));
    }

    [Fact]
    public void Search_WithoutMatch_SaysSoAndDisablesTheSteps()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "absent"));

        Assert.Equal("Aucune correspondance", viewer.Find(".omni-log-viewer__matches").TextContent);
        Assert.True(viewer.Find(".omni-log-viewer__next").HasAttribute("disabled"));
        Assert.True(viewer.Find(".omni-log-viewer__previous").HasAttribute("disabled"));
    }

    [Fact]
    public void SearchBox_ReportsTheText_AndItsPlaceholderCanBeNamed()
    {
        string? reported = null;
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchPlaceholder, "Filtrer")
            .Add(viewer => viewer.SearchTextChanged, text => reported = text));

        var box = viewer.Find("input.omni-log-viewer__search");
        Assert.Equal("Filtrer", box.GetAttribute("aria-label"));
        box.Input("perdue");

        Assert.Equal("perdue", reported);
        Assert.Single(viewer.FindAll("mark"));
        Assert.Empty(RenderViewer(Mixed).FindAll(".omni-log-viewer__toolbar"));
    }

    [Fact]
    public void Steps_GoFromMatchToMatch_AndWrapAround()
    {
        var module = Module();
        var follow = new List<bool>();
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "connexion")
            .Add(viewer => viewer.FollowChanged, value => follow.Add(value)));

        viewer.Find(".omni-log-viewer__next").Click();
        Assert.Equal("1 sur 2", viewer.Find(".omni-log-viewer__matches").TextContent);
        Assert.Equal([false], follow);
        Assert.Contains("omni-log-viewer__line--current", viewer.FindAll(".omni-log-viewer__line")[3].ClassList);
        Assert.Contains(module.Invocations, call => call.Identifier == "reveal");

        viewer.Find(".omni-log-viewer__next").Click();
        Assert.Equal("2 sur 2", viewer.Find(".omni-log-viewer__matches").TextContent);
        viewer.Find(".omni-log-viewer__next").Click();
        Assert.Equal("1 sur 2", viewer.Find(".omni-log-viewer__matches").TextContent);
        viewer.Find(".omni-log-viewer__previous").Click();
        Assert.Equal("2 sur 2", viewer.Find(".omni-log-viewer__matches").TextContent);
    }

    [Fact]
    public void Step_WaitingForItsScroll_KeepsTheScrollTheSnapshotReports()
    {
        // The step asked the script to scroll to the match: the snapshot read before that scroll is
        // stale and must not move the window back.
        var module = Module();
        module.Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(new GridViewportSnapshot { ScrollTop = 0, ViewportHeight = 40 });
        var lines = Enumerable.Range(1, 60).Select(index => new OmniLogLine(index == 50 ? "cible" : $"ligne {index}")).ToArray();
        var viewer = RenderViewer(lines, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "cible"));

        viewer.Find(".omni-log-viewer__next").Click();

        Assert.Contains("cible", Texts(viewer));
        Assert.Contains(module.Invocations, call => call.Identifier == "reveal");
    }

    [Fact]
    public void FirstStepBack_TakesTheLastMatchAboveTheView()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "connexion"));

        viewer.Find(".omni-log-viewer__previous").Click();

        // At the top of the log no match lies above: the step wraps to the last one.
        Assert.Equal("2 sur 2", viewer.Find(".omni-log-viewer__matches").TextContent);
    }

    [Fact]
    public async Task FirstStep_StartsFromTheViewport()
    {
        var lines = Enumerable.Range(1, 40).Select(index => new OmniLogLine(index % 10 == 6 ? $"cible {index}" : $"ligne {index}")).ToArray();
        var viewer = RenderViewer(lines, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "cible"));

        // The reader scrolled to line 22 (20 px a line): the next match is line 26, the previous one 16.
        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(420, 200, false, false));
        viewer.Find(".omni-log-viewer__next").Click();
        Assert.Equal("3 sur 4", viewer.Find(".omni-log-viewer__matches").TextContent);

        var back = RenderViewer(lines, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "cible"));
        await back.InvokeAsync(() => back.Instance.OnViewportChangedAsync(420, 200, false, false));
        back.Find(".omni-log-viewer__previous").Click();
        Assert.Equal("2 sur 4", back.Find(".omni-log-viewer__matches").TextContent);

        var below = RenderViewer(lines, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "cible"));
        await below.InvokeAsync(() => below.Instance.OnViewportChangedAsync(800, 200, true, false));
        below.Find(".omni-log-viewer__next").Click();

        // Past the last match, the first step wraps to the first one.
        Assert.Equal("1 sur 4", below.Find(".omni-log-viewer__matches").TextContent);
    }

    [Fact]
    public void AppendedLines_AreSearchedToo()
    {
        var lines = new List<OmniLogLine>(Mixed);
        var viewer = RenderViewer(lines, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "connexion"));

        lines.Add(new("Connexion rétablie"));
        viewer.Render(parameters => parameters.Add(component => component.Lines, lines));

        Assert.Equal("0 sur 3", viewer.Find(".omni-log-viewer__matches").TextContent);
    }

    [Fact]
    public void SearchCleared_ByTheHostOrInTheBox_RemovesTheMarks()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "connexion"));

        viewer.Render(parameters => parameters.Add(component => component.SearchText, null));
        Assert.Empty(viewer.FindAll("mark"));
        Assert.Empty(viewer.FindAll(".omni-log-viewer__matches"));

        viewer.Find("input.omni-log-viewer__search").Input("perdue");
        viewer.Find("input.omni-log-viewer__search").Input(string.Empty);
        Assert.Empty(viewer.FindAll("mark"));
    }

    [Fact]
    public void StaleStepClick_AfterTheMatchesWent_DoesNothing()
    {
        // A click can arrive for a render that still showed matches, after a new search emptied them.
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.SearchText, "connexion"));
        var next = viewer.Find(".omni-log-viewer__next");

        viewer.Render(parameters => parameters.Add(component => component.SearchText, "absent"));
        next.Click();

        Assert.Equal("Aucune correspondance", viewer.Find(".omni-log-viewer__matches").TextContent);
        Assert.True(viewer.Instance.IsFollowing);
    }

    [Fact]
    public async Task UnfilteredLinesArrivingWhileAway_AreAllCounted()
    {
        var lines = new List<OmniLogLine>(Mixed);
        var viewer = RenderViewer(lines);
        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 200, false, false));

        lines.Add(new("Bavard", OmniLogLevel.Trace));
        lines.Add(new("Encore"));
        viewer.Render(parameters => parameters.Add(component => component.Lines, lines));

        Assert.Equal("2 nouvelles lignes", viewer.Find(".omni-log-viewer__jump").TextContent.Trim());
    }

    [Fact]
    public void FirstLines_AfterAnEmptyLog_AreAnAppend()
    {
        var viewer = RenderViewer([]);

        viewer.Render(parameters => parameters.Add(component => component.Lines, Mixed));

        Assert.Equal(5, Texts(viewer).Count);
    }

    [Fact]
    public void SnapshotWithoutRows_StillTakesTheViewportHeight()
    {
        var module = Module();
        module.Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(new GridViewportSnapshot { ViewportHeight = 40 });
        var lines = Enumerable.Range(1, 40).Select(index => new OmniLogLine($"ligne {index}")).ToArray();

        var viewer = RenderViewer(lines, parameters => parameters.Add(viewer => viewer.Follow, false));

        // 40 px of viewport instead of the fallback of twelve lines: fewer lines are rendered.
        JSInterop.SetupModule(OmniModules.LogViewer).Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(null);
        var fallback = RenderViewer(lines, parameters => parameters.Add(viewer => viewer.Follow, false));
        Assert.True(Texts(viewer).Count < Texts(fallback).Count, $"{Texts(viewer).Count} lines rendered for 40 px, {Texts(fallback).Count} without a height.");
    }

    [Fact]
    public async Task RenderAfterDispose_LeavesTheScriptAlone()
    {
        var module = Module();
        var viewer = RenderViewer(Mixed);
        await viewer.Instance.DisposeAsync();
        var calls = module.Invocations.Count;

        viewer.Render();

        Assert.Equal(calls, module.Invocations.Count);
    }

    // ---- Follow --------------------------------------------------------------------------------

    [Fact]
    public void FollowButton_TogglesAndReports()
    {
        var follow = new List<bool>();
        var viewer = RenderViewer(Mixed, parameters => parameters
            .Add(viewer => viewer.ShowSearch, true)
            .Add(viewer => viewer.FollowChanged, value => follow.Add(value)));
        var button = viewer.Find(".omni-log-viewer__follow");
        Assert.Equal("true", button.GetAttribute("aria-pressed"));

        button.Click();
        Assert.False(viewer.Instance.IsFollowing);
        Assert.Equal("false", viewer.Find(".omni-log-viewer__follow").GetAttribute("aria-pressed"));

        viewer.Find(".omni-log-viewer__follow").Click();
        Assert.Equal([false, true], follow);
    }

    [Fact]
    public void FollowParameter_IsTakenOnlyWhenItChanges()
    {
        var viewer = RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.Follow, false));
        Assert.False(viewer.Instance.IsFollowing);

        viewer.Render(parameters => parameters.Add(component => component.Follow, true));
        Assert.True(viewer.Instance.IsFollowing);
    }

    [Fact]
    public async Task LinesArrivingWhileAway_AreCountedOnTheJumpButton_ThatFollowsAgain()
    {
        var lines = new List<OmniLogLine>(Mixed);
        var viewer = RenderViewer(lines, parameters => parameters.Add(viewer => viewer.MinimumLevel, OmniLogLevel.Information));
        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 200, false, false));

        lines.Add(new("Nouvelle", OmniLogLevel.Information));
        lines.Add(new("Bavard", OmniLogLevel.Trace));
        viewer.Render(parameters => parameters.Add(component => component.Lines, lines));

        // The trace line is under the filter: one new line counts.
        Assert.Equal("1 nouvelle ligne", viewer.Find(".omni-log-viewer__jump").TextContent.Trim());

        viewer.Find(".omni-log-viewer__jump").Click();
        Assert.True(viewer.Instance.IsFollowing);
        Assert.Empty(viewer.FindAll(".omni-log-viewer__jump"));
    }

    [Fact]
    public async Task JumpButton_OffersTheLatestLines_WhenTheReaderIsAboveTheEnd()
    {
        var viewer = RenderViewer(Mixed);
        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 40, false, false));
        Assert.Equal("Aller aux dernières lignes", viewer.Find(".omni-log-viewer__jump").TextContent.Trim());

        await viewer.InvokeAsync(viewer.Instance.JumpToLatestAsync);
        Assert.True(viewer.Instance.IsFollowing);
    }

    [Fact]
    public async Task ViewportReport_ThatChangesNothing_DoesNotRender()
    {
        var viewer = RenderViewer(Mixed);
        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 200, true, true));
        var renders = viewer.RenderCount;

        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 200, true, true));

        Assert.Equal(renders, viewer.RenderCount);
    }

    [Fact]
    public async Task UnboundHost_RenderingAgain_KeepsWhatTheReaderChoseInTheLog()
    {
        // The host binds none of Follow, MinimumLevel and SearchText: its renders pass their unchanged
        // defaults, which used to pull the reader back to the tail and clear the filter and the search.
        var host = Render<LogViewerUnboundHost>();
        var viewer = host.FindComponent<OmniLogViewer>();
        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 200, false, false));
        host.Find("select.omni-log-viewer__level").Change("3");
        host.Find("input.omni-log-viewer__search").Input("perdue");

        await host.InvokeAsync(() => host.Instance.Append("Connexion perdue encore"));

        Assert.False(viewer.Instance.IsFollowing);
        Assert.Equal("1 nouvelle ligne", host.Find(".omni-log-viewer__jump").TextContent.Trim());
        Assert.Equal(["Connexion perdue", "Connexion perdue encore"], host.FindAll(".omni-log-viewer__text").Select(text => text.TextContent));
        Assert.Equal(2, host.FindAll("mark").Count);
    }

    // ---- Script --------------------------------------------------------------------------------

    [Fact]
    public void Script_IsAttachedOnce_PinnedWhileFollowing_AndToldOfAFollowChange()
    {
        var module = Module();
        var viewer = RenderViewer(Mixed);
        viewer.Render();

        Assert.Single(module.Invocations, call => call.Identifier == "attach");
        Assert.Contains(module.Invocations, call => call.Identifier == "pin");

        viewer.Render(parameters => parameters.Add(component => component.Follow, false));
        Assert.Contains(module.Invocations, call => call.Identifier == "setFollowing" && Equals(call.Arguments[1], false));
    }

    [Fact]
    public void Snapshot_MeasuresTheLines_AndTakesTheScrollOnlyWhenNotFollowing()
    {
        var module = Module();
        module.Setup<GridViewportSnapshot?>("sync", _ => true).SetResult(new GridViewportSnapshot
        {
            ScrollTop = 30,
            ViewportHeight = 60,
            Rows = [new GridRowMeasurement { Index = 0, Height = 44 }],
        });
        var lines = Enumerable.Range(1, 40).Select(index => new OmniLogLine($"ligne {index}")).ToArray();

        var following = RenderViewer(lines);
        // Following: the tail is shown whatever the reported scroll.
        Assert.Equal("ligne 40", Texts(following)[^1]);

        var reading = RenderViewer(lines, parameters => parameters.Add(viewer => viewer.Follow, false));
        Assert.Equal("ligne 1", Texts(reading)[0]);
        Assert.DoesNotContain("ligne 40", Texts(reading));
    }

    [Fact]
    public async Task ViewportReport_AfterDispose_IsIgnored()
    {
        var follow = new List<bool>();
        var viewer = RenderViewer(Mixed, parameters => parameters.Add(viewer => viewer.FollowChanged, value => follow.Add(value)));
        await viewer.Instance.DisposeAsync();

        await viewer.InvokeAsync(() => viewer.Instance.OnViewportChangedAsync(0, 200, false, false));

        Assert.Empty(follow);
        Assert.True(viewer.Instance.IsFollowing);
    }

    [Fact]
    public async Task Dispose_DetachesAndReleasesTheScript_AndTakesALostCircuit()
    {
        var module = Module();
        var viewer = RenderViewer(Mixed);
        await viewer.Instance.DisposeAsync();
        Assert.Contains(module.Invocations, call => call.Identifier == "detach");

        var lost = JSInterop.SetupModule(OmniModules.LogViewer);
        lost.SetupVoid("detach", _ => true).SetException(new JSDisconnectedException("perdu"));
        var other = RenderViewer(Mixed);
        await other.Instance.DisposeAsync();

        // Never attached: nothing to detach, nothing thrown.
        await new OmniLogViewer().DisposeAsync();
    }

    [Fact]
    public async Task DisposedWhileTheScriptLoads_ReleasesItOnArrival()
    {
        // The import is still pending when the viewer goes: DisposeAsync has no module to release yet,
        // so the render that awaited the import releases it as soon as it arrives.
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var viewer = RenderViewer(Mixed);

        await viewer.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        // No render follows the release: wait for the release itself.
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }
}
