using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniInkCanvas"/> in bUnit. The pointer is read by <c>omni-ink-canvas.js</c> (its own node tests and
/// the showcase probe cover the pixels and the events); here the strokes the script reports reach .NET through the
/// same internal handlers its bridge calls, so the value, undo, clear, keys, toolbar, localization and export call
/// path are covered, with the module's calls and their arguments read back from bUnit's interop.
/// </summary>
public sealed class InkCanvasTests : OmniBunitContext
{
    private static readonly double[] Flat = [0.1, 0.2, -1, 0.3, 0.4, 0.5];

    private readonly BunitJSModuleInterop _module;
    private readonly List<IReadOnlyList<OmniInkStroke>> _raised = [];

    public InkCanvasTests() => _module = JSInterop.SetupModule(OmniModules.InkCanvas);

    private IRenderedComponent<OmniInkCanvas> RenderCanvas(Action<ComponentParameterCollectionBuilder<OmniInkCanvas>>? configure = null)
        => Render<OmniInkCanvas>(parameters =>
        {
            parameters.Add(canvas => canvas.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, strokes => _raised.Add(strokes)));
            configure?.Invoke(parameters);
        });

    private static Task Draw(IRenderedComponent<OmniInkCanvas> canvas, string color = "#112233", double width = 4, double[]? flat = null)
        => canvas.InvokeAsync(() => canvas.Instance.HandleStrokeAsync(color, width, flat ?? Flat));

    private static string Announcement(IRenderedComponent<OmniInkCanvas> canvas) => canvas.Find("[role=status]").TextContent.Trim(' ');

    private static OmniInkStroke Stroke(string color = "#abcdef") => new(color, 3, [new OmniInkPoint(0.25, 0.5, null), new OmniInkPoint(0.75, 0.5, 0.75)]);

    // ----- Markup ------------------------------------------------------------------------------------

    [Fact]
    public void Canvas_IsANamedImage_DescribedByItsHelp_InsideAFrame_WithNoInlineStyle()
    {
        var canvas = RenderCanvas();

        var surface = canvas.Find("canvas.omni-ink-canvas__surface");
        Assert.Equal("img", surface.GetAttribute("role"));
        Assert.Equal("Zone de dessin", surface.GetAttribute("aria-label"));
        Assert.Equal("0", surface.GetAttribute("tabindex"));
        var help = canvas.Find($"#{surface.GetAttribute("aria-describedby")}");
        Assert.Contains("Ctrl+Z", help.TextContent, StringComparison.Ordinal);
        Assert.Contains("omni-visually-hidden", help.ClassName, StringComparison.Ordinal);
        Assert.Single(canvas.FindAll(".omni-ink-canvas__frame canvas"));
        Assert.DoesNotContain("style=", canvas.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("polite", canvas.Find("[role=status]").GetAttribute("aria-live"));
    }

    [Fact]
    public void Label_NamesTheCanvas_AndIdBecomesTheRootIdAndTheBaseOfTheHelpId()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Label, "Signature").Add(c => c.Id, "sign").Add(c => c.Class, "mine"));

        Assert.Equal("Signature", canvas.Find("canvas").GetAttribute("aria-label"));
        Assert.Equal("sign-help", canvas.Find("canvas").GetAttribute("aria-describedby"));
        var root = canvas.Find("div.omni-ink-canvas");
        Assert.Equal("sign", root.Id);
        Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutAnId_TheRootGetsAGeneratedOne()
    {
        var canvas = RenderCanvas();

        var id = canvas.Find("div.omni-ink-canvas").Id;
        Assert.StartsWith("omni-ink-canvas-", id, StringComparison.Ordinal);
        Assert.Equal($"{id}-help", canvas.Find("canvas").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void DisabledAndReadOnly_AreMarked_Disabled_LeavesTheTabOrder_ReadOnly_KeepsTheFocus()
    {
        var disabled = RenderCanvas(parameters => parameters.Add(c => c.Disabled, true));
        var readOnly = RenderCanvas(parameters => parameters.Add(c => c.ReadOnly, true));
        var normal = RenderCanvas();

        Assert.Contains("omni-ink-canvas--disabled", disabled.Find("div.omni-ink-canvas").ClassName, StringComparison.Ordinal);
        Assert.False(disabled.Find("canvas").HasAttribute("tabindex"));
        Assert.Equal("true", disabled.Find("canvas").GetAttribute("data-omni-ink-locked"));
        Assert.Contains("omni-ink-canvas--readonly", readOnly.Find("div.omni-ink-canvas").ClassName, StringComparison.Ordinal);
        Assert.Equal("0", readOnly.Find("canvas").GetAttribute("tabindex"));
        Assert.Equal("true", readOnly.Find("canvas").GetAttribute("data-omni-ink-locked"));
        Assert.False(normal.Find("canvas").HasAttribute("data-omni-ink-locked"));
        Assert.DoesNotContain("--disabled", normal.Find("div.omni-ink-canvas").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("--readonly", normal.Find("div.omni-ink-canvas").ClassName, StringComparison.Ordinal);
    }

    // ----- Parameters --------------------------------------------------------------------------------

    [Fact]
    public void Parameters_AreChecked()
    {
        Assert.Throws<ArgumentNullException>(() => RenderCanvas(parameters => parameters.Add(c => c.Value, null!)));
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderCanvas(parameters => parameters.Add(c => c.StrokeWidth, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderCanvas(parameters => parameters.Add(c => c.StrokeWidth, double.NaN)));
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderCanvas(parameters => parameters.Add(c => c.AspectRatio, 0d)));
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderCanvas(parameters => parameters.Add(c => c.AspectRatio, double.PositiveInfinity)));
        Assert.Throws<ArgumentException>(() => RenderCanvas(parameters => parameters.Add(c => c.Width, "wide")));
        Assert.Throws<ArgumentException>(() => RenderCanvas(parameters => parameters.Add(c => c.Width, "0px")));
        Assert.Throws<ArgumentException>(() => RenderCanvas(parameters => parameters.Add(c => c.Height, "12 rem")));
        Assert.Throws<ArgumentException>(() => RenderCanvas(parameters => parameters.Add(c => c.Height, "10ch")));
    }

    [Fact]
    public void Layout_IsGivenToTheScript_OnTheFirstRender_AndAgainOnlyWhenItChanges()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Width, "30rem").Add(c => c.Height, "10rem"));

        var first = Assert.Single(_module.Invocations["setLayout"]).Arguments;
        Assert.Equal(new object?[] { "30rem", "10rem", null }, first.Skip(1));

        canvas.Render();
        Assert.Single(_module.Invocations["setLayout"]);

        canvas.Render(parameters => parameters.Add(c => c.AspectRatio, 3d));
        Assert.Equal(2, _module.Invocations["setLayout"].Count);
        Assert.Equal(new object?[] { "30rem", "10rem", 3d }, _module.Invocations["setLayout"][1].Arguments.Skip(1));
    }

    // ----- Script ------------------------------------------------------------------------------------

    [Fact]
    public void FirstRender_AttachesTheScriptOnce_AndGivesItTheValue()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Value, [Stroke()]));

        Assert.Single(_module.Invocations["attach"]);
        canvas.Render();
        Assert.Single(_module.Invocations["attach"]);
        var pushed = Assert.Single(_module.Invocations["setStrokes"]).Arguments;
        Assert.Equal(["#abcdef"], (string[])pushed[1]!);
        Assert.Equal([3d], (double[])pushed[2]!);
        Assert.Equal(new[] { new[] { 0.25, 0.5, -1d, 0.75, 0.5, 0.75 } }, (double[][])pushed[3]!);
    }

    [Fact]
    public async Task ADifferentListFromTheHost_IsPushedToTheScript_ButNotTheOneTheCanvasRaised()
    {
        var canvas = RenderCanvas();
        await Draw(canvas);
        Assert.Single(_module.Invocations["setStrokes"]);

        canvas.Render(parameters => parameters.Add(c => c.Value, _raised[^1]));
        Assert.Single(_module.Invocations["setStrokes"]);

        canvas.Render(parameters => parameters.Add(c => c.Value, [Stroke("#010203")]));
        Assert.Equal(2, _module.Invocations["setStrokes"].Count);
        Assert.Equal(["#010203"], (string[])_module.Invocations["setStrokes"][1].Arguments[1]!);
    }

    [Fact]
    public async Task WithoutABinding_TheCanvasKeepsItsDrawing_WhenItsParentRendersAgain()
    {
        var canvas = RenderCanvas();
        await Draw(canvas);

        canvas.Render();

        Assert.False(canvas.FindAll("button:not([aria-pressed])")[0].HasAttribute("disabled"));
        Assert.Single(_module.Invocations["setStrokes"]);
    }

    [Fact]
    public async Task LostCircuit_WhileTheScriptIsCalled_IsIgnored()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);

        var canvas = RenderCanvas();
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        await canvas.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
    }

    [Fact]
    public async Task Dispose_DetachesAndReleasesTheScript_AndLaterCallsDoNothing()
    {
        var canvas = RenderCanvas();
        await Draw(canvas);
        var raised = _raised.Count;

        await canvas.Instance.DisposeAsync();
        await Draw(canvas);
        canvas.Render();

        Assert.Single(_module.Invocations["detach"]);
        Assert.Equal(raised, _raised.Count);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => canvas.Instance.ExportPngAsync(10, 10, Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposingBeforeTheRenderHappened_DoesNothing()
    {
        await new OmniInkCanvas().DisposeAsync();
    }

    [Fact]
    public async Task CanvasGoneWhileItsScriptLoads_ReleasesTheScriptOnArrival_AndAnExportRefuses()
    {
        var runtime = this.InstallHeldImports();
        var canvas = RenderCanvas();
        var export = canvas.Instance.ExportPngAsync(10, 10, Xunit.TestContext.Current.CancellationToken);

        await runtime.DisposeWhileTheImportWaitsAsync(canvas.Instance, TimeSpan.FromSeconds(10));

        await Assert.ThrowsAsync<ObjectDisposedException>(() => export);
        Assert.Empty(runtime.Module.Calls);
    }

    // ----- Strokes from the pointer ------------------------------------------------------------------

    [Fact]
    public async Task AStrokeFromTheScript_IsAddedToTheValue_RaisedOnce_AndNotDrawnAgain()
    {
        var canvas = RenderCanvas();

        await Draw(canvas, "#112233", 4, [0.1, 0.2, -1, 0.3, 0.4, 0.5, 1.5, -0.5, 0.25]);

        var value = Assert.Single(_raised);
        var stroke = Assert.Single(value);
        Assert.Equal("#112233", stroke.Color);
        Assert.Equal(4, stroke.Width);
        Assert.Equal([new OmniInkPoint(0.1, 0.2, null), new(0.3, 0.4, 0.5), new(1, 0, 0.25)], stroke.Points);
        Assert.Single(_module.Invocations["setStrokes"]);
        Assert.False(canvas.FindAll("button:not([aria-pressed])")[0].HasAttribute("disabled"));
        Assert.False(canvas.FindAll("button:not([aria-pressed])")[1].HasAttribute("disabled"));
    }

    [Fact]
    public async Task StrokesAccumulate_InOrder_AsNewLists()
    {
        var canvas = RenderCanvas();

        await Draw(canvas, "#111111");
        await Draw(canvas, "#222222");

        Assert.Equal(2, _raised.Count);
        Assert.Equal(["#111111"], _raised[0].Select(stroke => stroke.Color));
        Assert.Equal(["#111111", "#222222"], _raised[1].Select(stroke => stroke.Color));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task AStrokeThatCannotBeOne_IsRefused_AndTheScriptGetsTheValueBack(int length)
    {
        var canvas = RenderCanvas();

        await Draw(canvas, flat: new double[length]);

        Assert.Empty(_raised);
        Assert.Equal(2, _module.Invocations["setStrokes"].Count);
    }

    [Fact]
    public async Task AStrokeOnALockedCanvas_IsRefused_AndTheScriptGetsTheValueBack()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.ReadOnly, true));

        await Draw(canvas);

        Assert.Empty(_raised);
        Assert.Equal(2, _module.Invocations["setStrokes"].Count);
        Assert.Empty((string[])_module.Invocations["setStrokes"][1].Arguments[1]!);
    }

    // ----- Undo and clear ----------------------------------------------------------------------------

    [Fact]
    public async Task Undo_RemovesTheLastStroke_RaisesTheShorterList_PushesItAndAnnouncesIt()
    {
        var canvas = RenderCanvas();
        await Draw(canvas, "#111111");
        await Draw(canvas, "#222222");

        canvas.FindAll("button:not([aria-pressed])")[0].Click();

        Assert.Equal(["#111111"], _raised[^1].Select(stroke => stroke.Color));
        Assert.Equal(["#111111"], (string[])_module.Invocations["setStrokes"][1].Arguments[1]!);
        Assert.Equal("Dernier trait annulé", Announcement(canvas));
    }

    [Fact]
    public async Task Clear_RemovesEveryStroke_RaisesAnEmptyList_PushesItAndAnnouncesIt()
    {
        var canvas = RenderCanvas();
        await Draw(canvas);
        await Draw(canvas);

        canvas.FindAll("button:not([aria-pressed])")[1].Click();

        Assert.Empty(_raised[^1]);
        Assert.Empty((string[])_module.Invocations["setStrokes"][1].Arguments[1]!);
        Assert.Equal("Dessin effacé", Announcement(canvas));
        Assert.True(canvas.FindAll("button:not([aria-pressed])")[0].HasAttribute("disabled"));
        Assert.True(canvas.FindAll("button:not([aria-pressed])")[1].HasAttribute("disabled"));
    }

    [Fact]
    public async Task AnnouncingTwiceTheSameWords_StillChangesTheText()
    {
        var canvas = RenderCanvas();
        await Draw(canvas);
        await Draw(canvas);
        await Draw(canvas);

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        var first = canvas.Find("[role=status]").TextContent;
        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        var second = canvas.Find("[role=status]").TextContent;

        Assert.NotEqual(first, second);
        Assert.Equal("Dernier trait annulé", Announcement(canvas));
    }

    [Fact]
    public async Task UndoAndClear_OnAnEmptyDrawing_DoNothing()
    {
        var canvas = RenderCanvas();

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        await canvas.InvokeAsync(canvas.Instance.ClearAsync);

        Assert.Empty(_raised);
        Assert.Equal(string.Empty, Announcement(canvas));
    }

    [Fact]
    public async Task TheHostCanUndoAndClear_EvenWhenTheCanvasIsReadOnly()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.ReadOnly, true).Add(c => c.Value, [Stroke("#111111"), Stroke("#222222")]));

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        Assert.Equal(["#111111"], _raised[^1].Select(stroke => stroke.Color));
        await canvas.InvokeAsync(canvas.Instance.ClearAsync);
        Assert.Empty(_raised[^1]);
    }

    [Fact]
    public async Task TheKeysOfTheScript_UndoAndClear_UnlessTheCanvasIsLockedOrGone()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Value, [Stroke("#111111"), Stroke("#222222")]));

        await canvas.InvokeAsync(canvas.Instance.HandleUndoKeyAsync);
        Assert.Equal(["#111111"], _raised[^1].Select(stroke => stroke.Color));
        await canvas.InvokeAsync(canvas.Instance.HandleClearKeyAsync);
        Assert.Empty(_raised[^1]);
        var raised = _raised.Count;

        canvas.Render(parameters => parameters.Add(c => c.Value, [Stroke()]).Add(c => c.ReadOnly, true));
        await canvas.InvokeAsync(canvas.Instance.HandleUndoKeyAsync);
        await canvas.InvokeAsync(canvas.Instance.HandleClearKeyAsync);
        canvas.Render(parameters => parameters.Add(c => c.ReadOnly, false).Add(c => c.Disabled, true));
        await canvas.InvokeAsync(canvas.Instance.HandleUndoKeyAsync);
        Assert.Equal(raised, _raised.Count);

        canvas.Render(parameters => parameters.Add(c => c.Disabled, false));
        await canvas.Instance.DisposeAsync();
        await canvas.InvokeAsync(canvas.Instance.HandleUndoKeyAsync);
        await canvas.InvokeAsync(canvas.Instance.HandleClearKeyAsync);
        Assert.Equal(raised, _raised.Count);
    }

    [Fact]
    public async Task TheBridge_ForwardsWhatTheScriptCalls()
    {
        var calls = new List<string>();
        var bridge = new InkCanvasInteropBridge(
            (color, width, points) => { calls.Add($"stroke {color} {width} {points.Length}"); return Task.CompletedTask; },
            () => { calls.Add("undo"); return Task.CompletedTask; },
            () => { calls.Add("clear"); return Task.CompletedTask; },
            color => { calls.Add($"theme {color}"); return Task.CompletedTask; },
            (indices, count) => { calls.Add($"erase {string.Join(',', indices)} of {count}"); return Task.CompletedTask; });

        await bridge.OnStroke("red", 2, [1, 2, 3]);
        await bridge.OnUndo();
        await bridge.OnClear();
        await bridge.OnThemeColor("#e0e0e0");
        await bridge.OnErase([0, 2], 3);

        Assert.Equal(["stroke red 2 3", "undo", "clear", "theme #e0e0e0", "erase 0,2 of 3"], calls);
    }

    // ----- Shared drawing: ids, events, tool ---------------------------------------------------------

    private readonly List<OmniInkStroke> _added = [];
    private readonly List<IReadOnlyList<OmniInkStroke>> _removed = [];

    private IRenderedComponent<OmniInkCanvas> RenderShared(Action<ComponentParameterCollectionBuilder<OmniInkCanvas>>? configure = null)
        => RenderCanvas(parameters =>
        {
            parameters.Add(canvas => canvas.StrokeAdded, EventCallback.Factory.Create<OmniInkStroke>(this, stroke => _added.Add(stroke)));
            parameters.Add(canvas => canvas.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, strokes => _removed.Add(strokes)));
            configure?.Invoke(parameters);
        });

    private static Task Erase(IRenderedComponent<OmniInkCanvas> canvas, int count, params int[] indices)
        => canvas.InvokeAsync(() => canvas.Instance.HandleEraseAsync(indices, count));

    private static OmniInkStroke Remote(string id, string color = "#abcdef") => Stroke(color) with { Id = id };

    [Fact]
    public async Task AStrokeDrawnOnTheCanvas_GetsAFreshId_32HexDigits_AndTheEventsCarryTheSameStroke()
    {
        var canvas = RenderShared();

        await Draw(canvas, "#111111");
        await Draw(canvas, "#222222");

        var first = _raised[0].Single();
        var second = _raised[1][1];
        Assert.Matches("^[0-9a-f]{32}$", first.Id);
        Assert.Matches("^[0-9a-f]{32}$", second.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Same(first, _raised[1][0]);
        Assert.Equal([first, second], _added);
    }

    [Fact]
    public void AStrokeTheHostGives_KeepsItsOwnIdOrNone_AndRaisesNothing()
    {
        var withId = Remote("remote-1");
        var withoutId = Stroke();
        var canvas = RenderShared(parameters => parameters.Add(c => c.Value, [withId, withoutId]));

        var pushed = (string[])_module.Invocations["setStrokes"][0].Arguments[1]!;
        Assert.Equal(2, pushed.Length);
        Assert.Equal("remote-1", withId.Id);
        Assert.Null(withoutId.Id);
        canvas.Render(parameters => parameters.Add(c => c.Value, [withId, withoutId, Remote("remote-2")]));
        Assert.Empty(_added);
        Assert.Empty(_removed);
        Assert.Empty(_raised);
    }

    /// <summary>Renders a canvas that adds "value" to <paramref name="order"/> on each change, plus the handlers <paramref name="more"/> adds.</summary>
    private IRenderedComponent<OmniInkCanvas> RenderRecordingChanges(List<string> order, Action<ComponentParameterCollectionBuilder<OmniInkCanvas>> more) =>
        Render<OmniInkCanvas>(parameters =>
        {
            parameters.Add(c => c.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, _ => order.Add("value")));
            more(parameters);
        });

    [Fact]
    public async Task StrokeAdded_IsRaisedAfterValueChanged_ForEveryDrawnStroke_ButNotForARefusedOne()
    {
        var order = new List<string>();
        var canvas = RenderRecordingChanges(order, parameters => parameters
            .Add(c => c.StrokeAdded, EventCallback.Factory.Create<OmniInkStroke>(this, _ => order.Add("added"))));

        await Draw(canvas);
        await Draw(canvas, flat: new double[2]);
        canvas.Render(parameters => parameters.Add(c => c.ReadOnly, true));
        await Draw(canvas);

        Assert.Equal(["value", "added"], order);
    }

    [Fact]
    public async Task StrokesRemoved_IsRaisedForUndo_AfterValueChanged_WithTheStrokeRemoved()
    {
        var order = new List<string>();
        var canvas = RenderRecordingChanges(order, parameters => parameters
            .Add(c => c.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, strokes => { order.Add("removed"); _removed.Add(strokes); })));
        await Draw(canvas, "#111111");
        await Draw(canvas, "#222222");
        order.Clear();

        canvas.FindAll("button:not([aria-pressed])")[0].Click();

        Assert.Equal(["value", "removed"], order);
        var removed = Assert.Single(_removed);
        Assert.Equal(["#222222"], removed.Select(stroke => stroke.Color));
    }

    [Fact]
    public async Task StrokesRemoved_IsRaisedForClear_WithEveryStroke_AndForUndoAndClearFromTheKeysAndTheHost()
    {
        var canvas = RenderShared();
        await Draw(canvas, "#111111");
        await Draw(canvas, "#222222");
        await Draw(canvas, "#333333");

        await canvas.InvokeAsync(canvas.Instance.HandleUndoKeyAsync);
        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        await canvas.InvokeAsync(canvas.Instance.HandleClearKeyAsync);
        Assert.Equal([["#333333"], ["#222222"], ["#111111"]], _removed.Select(list => list.Select(stroke => stroke.Color).ToArray()));

        await Draw(canvas, "#444444");
        await Draw(canvas, "#555555");
        _removed.Clear();
        canvas.FindAll("button:not([aria-pressed])")[1].Click();
        Assert.Equal(["#444444", "#555555"], Assert.Single(_removed).Select(stroke => stroke.Color));

        await canvas.InvokeAsync(canvas.Instance.ClearAsync);
        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        Assert.Single(_removed);
    }

    [Fact]
    public async Task TheHostCanUndoAndClear_ReadOnly_AndStillGetsStrokesRemoved()
    {
        var canvas = RenderShared(parameters => parameters.Add(c => c.ReadOnly, true).Add(c => c.Value, [Stroke("#111111"), Stroke("#222222")]));

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        await canvas.InvokeAsync(canvas.Instance.ClearAsync);

        Assert.Equal([["#222222"], ["#111111"]], _removed.Select(list => list.Select(stroke => stroke.Color).ToArray()));
    }

    [Fact]
    public async Task AValueFromTheHost_RaisesNoStrokeAddedAndNoStrokesRemoved_EvenWhenItDropsStrokes()
    {
        var canvas = RenderShared(parameters => parameters.Add(c => c.Value, [Remote("a"), Remote("b")]));
        await Draw(canvas);
        _added.Clear();

        canvas.Render(parameters => parameters.Add(c => c.Value, [Remote("a")]));
        canvas.Render(parameters => parameters.Add(c => c.Value, []));

        Assert.Empty(_added);
        Assert.Empty(_removed);
    }

    [Fact]
    public async Task Undo_RemovesTheLastStrokeDrawnHere_WhenRemoteStrokesCameAfter()
    {
        var canvas = RenderShared();
        await Draw(canvas, "#aaaaaa");
        var mine = _raised[^1].Single();
        var withRemotes = new List<OmniInkStroke>(_raised[^1]) { Remote("r1", "#bbbbbb"), Remote("r2", "#cccccc") };
        canvas.Render(parameters => parameters.Add(c => c.Value, withRemotes));
        _removed.Clear();

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);

        Assert.Equal(["#bbbbbb", "#cccccc"], _raised[^1].Select(stroke => stroke.Color));
        Assert.Same(mine, Assert.Single(Assert.Single(_removed)));
        Assert.Equal(["#bbbbbb", "#cccccc"], ((string[])_module.Invocations["setStrokes"][^1].Arguments[1]!));
    }

    [Fact]
    public async Task Undo_TakesTheOwnStrokesNewestFirst_ThenFallsBackToTheLastStroke()
    {
        var canvas = RenderShared();
        await Draw(canvas, "#111111");
        await Draw(canvas, "#222222");
        var all = new List<OmniInkStroke> { Remote("r0", "#999999"), _raised[^1][0], Remote("r1", "#888888"), _raised[^1][1], Remote("r2", "#777777") };
        canvas.Render(parameters => parameters.Add(c => c.Value, all));

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        Assert.Equal(["#999999", "#111111", "#888888", "#777777"], _raised[^1].Select(stroke => stroke.Color));
        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        Assert.Equal(["#999999", "#888888", "#777777"], _raised[^1].Select(stroke => stroke.Color));

        // Nothing of this canvas is left: the last stroke of the drawing goes, as for a single user.
        await canvas.InvokeAsync(canvas.Instance.UndoAsync);
        Assert.Equal(["#999999", "#888888"], _raised[^1].Select(stroke => stroke.Color));
    }

    [Fact]
    public async Task Undo_WithAHostListThatDroppedTheOwnStroke_UndoesTheLastStrokeInstead()
    {
        var canvas = RenderShared();
        await Draw(canvas, "#111111");
        canvas.Render(parameters => parameters.Add(c => c.Value, [Remote("r0", "#999999"), Remote("r1", "#888888")]));

        await canvas.InvokeAsync(canvas.Instance.UndoAsync);

        Assert.Equal(["#999999"], _raised[^1].Select(stroke => stroke.Color));
    }

    [Fact]
    public async Task Eraser_RemovesTheTouchedStrokes_InOneValueChanged_ThenStrokesRemoved_AndPushesTheRest()
    {
        var canvas = RenderShared(parameters => parameters.Add(c => c.Value, [Remote("a", "#111111"), Remote("b", "#222222"), Remote("c", "#333333")]));

        await Erase(canvas, 3, 2, 0, 2);

        Assert.Equal(["#222222"], Assert.Single(_raised).Select(stroke => stroke.Color));
        Assert.Equal(["#111111", "#333333"], Assert.Single(_removed).Select(stroke => stroke.Color));
        Assert.Empty(_added);
        Assert.Equal(["#222222"], (string[])_module.Invocations["setStrokes"][^1].Arguments[1]!);
        Assert.Equal("Traits effacés", Announcement(canvas));
    }

    [Fact]
    public async Task Eraser_OnALockedCanvas_AHostListInTheMeantime_OrBadIndices_RemovesNothing_AndTheScriptGetsTheStrokesBack()
    {
        var canvas = RenderShared(parameters => parameters.Add(c => c.ReadOnly, true).Add(c => c.Value, [Remote("a"), Remote("b")]));
        var pushes = _module.Invocations["setStrokes"].Count;

        await Erase(canvas, 2, 0);
        Assert.Equal(pushes + 1, _module.Invocations["setStrokes"].Count);

        canvas.Render(parameters => parameters.Add(c => c.ReadOnly, false));
        pushes = _module.Invocations["setStrokes"].Count;
        await Erase(canvas, 3, 0);
        await Erase(canvas, 2, 2);
        await Erase(canvas, 2, -1);
        await Erase(canvas, 2);
        await canvas.InvokeAsync(() => canvas.Instance.HandleEraseAsync(null!, 2));

        Assert.Equal(pushes + 5, _module.Invocations["setStrokes"].Count);
        Assert.Empty(_raised);
        Assert.Empty(_removed);
    }

    [Fact]
    public async Task Eraser_AfterDispose_DoesNothing()
    {
        var canvas = RenderShared(parameters => parameters.Add(c => c.Value, [Remote("a")]));
        await canvas.Instance.DisposeAsync();

        await Erase(canvas, 1, 0);

        Assert.Empty(_raised);
        Assert.Empty(_removed);
    }

    [Fact]
    public void Tool_IsPen_ByDefault_AndTheMarkupTellsTheScriptWhichOneItIs()
    {
        var canvas = RenderCanvas();
        Assert.Equal("pen", canvas.Find("canvas").GetAttribute("data-omni-ink-tool"));
        Assert.DoesNotContain("omni-ink-canvas--eraser", canvas.Find("div.omni-ink-canvas").ClassName, StringComparison.Ordinal);

        canvas.Render(parameters => parameters.Add(c => c.Tool, OmniInkTool.Eraser));
        Assert.Equal("eraser", canvas.Find("canvas").GetAttribute("data-omni-ink-tool"));
        Assert.Contains("omni-ink-canvas--eraser", canvas.Find("div.omni-ink-canvas").ClassName, StringComparison.Ordinal);
        Assert.DoesNotContain("style=", canvas.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Toolbar_HasAPenAndAnEraserToggle_PressedAccordingToTheTool_AndLocalized()
    {
        var canvas = RenderCanvas();

        var tools = canvas.FindAll("[role=toolbar] button[aria-pressed]");
        Assert.Equal(["Crayon", "Gomme"], tools.Select(button => button.GetAttribute("aria-label")));
        Assert.Equal(["Crayon", "Gomme"], tools.Select(button => button.GetAttribute("title")));
        Assert.Equal(["true", "false"], tools.Select(button => button.GetAttribute("aria-pressed")));
        Assert.All(tools, button => Assert.False(button.HasAttribute("disabled")));

        canvas.Render(parameters => parameters.Add(c => c.Tool, OmniInkTool.Eraser));
        Assert.Equal(["false", "true"], canvas.FindAll("[role=toolbar] button[aria-pressed]").Select(button => button.GetAttribute("aria-pressed")));
    }

    [Fact]
    public void ToolButtons_ChooseTheTool_RaiseToolChanged_AndTheParameterTakesOverWhenItChanges()
    {
        var raised = new List<OmniInkTool>();
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.ToolChanged, EventCallback.Factory.Create<OmniInkTool>(this, raised.Add)));

        canvas.FindAll("[role=toolbar] button[aria-pressed]")[1].Click();
        Assert.Equal([OmniInkTool.Eraser], raised);
        Assert.Equal("eraser", canvas.Find("canvas").GetAttribute("data-omni-ink-tool"));

        // Pressing the tool already chosen raises nothing.
        canvas.FindAll("[role=toolbar] button[aria-pressed]")[1].Click();
        Assert.Single(raised);

        canvas.Render();
        Assert.Equal("eraser", canvas.Find("canvas").GetAttribute("data-omni-ink-tool"));

        canvas.FindAll("[role=toolbar] button[aria-pressed]")[0].Click();
        Assert.Equal([OmniInkTool.Eraser, OmniInkTool.Pen], raised);
        Assert.Equal("pen", canvas.Find("canvas").GetAttribute("data-omni-ink-tool"));

        canvas.Render(parameters => parameters.Add(c => c.Tool, OmniInkTool.Eraser));
        Assert.Equal("eraser", canvas.Find("canvas").GetAttribute("data-omni-ink-tool"));
    }

    [Fact]
    public void ToolButtons_AreDisabled_WhenTheCanvasIsReadOnlyOrDisabled_AndALockedCanvasMarksTheScript()
    {
        var readOnly = RenderCanvas(parameters => parameters.Add(c => c.ReadOnly, true).Add(c => c.Tool, OmniInkTool.Eraser));
        var disabled = RenderCanvas(parameters => parameters.Add(c => c.Disabled, true));

        Assert.All(readOnly.FindAll("[role=toolbar] button[aria-pressed]"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.All(disabled.FindAll("[role=toolbar] button[aria-pressed]"), button => Assert.True(button.HasAttribute("disabled")));
        // The eraser is blocked in the script like the pen: the same locked marker, read at every press.
        Assert.Equal("true", readOnly.Find("canvas").GetAttribute("data-omni-ink-locked"));
        Assert.Equal("eraser", readOnly.Find("canvas").GetAttribute("data-omni-ink-tool"));
    }

    [Fact]
    public async Task TheKeysStayTheSame_WhateverTheTool()
    {
        var canvas = RenderShared(parameters => parameters.Add(c => c.Tool, OmniInkTool.Eraser).Add(c => c.Value, [Remote("a"), Remote("b")]));

        await canvas.InvokeAsync(canvas.Instance.HandleUndoKeyAsync);
        Assert.Equal(["#abcdef"], _raised[^1].Select(stroke => stroke.Color));
        await canvas.InvokeAsync(canvas.Instance.HandleClearKeyAsync);
        Assert.Empty(_raised[^1]);
    }

    // ----- Toolbar -----------------------------------------------------------------------------------

    [Fact]
    public void Toolbar_HasAColourField_AWidthSlider_AndNamedUndoAndClearButtons()
    {
        var canvas = RenderCanvas();

        var toolbar = canvas.Find("[role=toolbar]");
        Assert.Equal("Outils de dessin", toolbar.GetAttribute("aria-label"));
        Assert.Equal("Couleur du trait", canvas.Find("input[type=color]").GetAttribute("aria-label"));
        Assert.Equal("Épaisseur du trait", canvas.Find("input[type=range]").GetAttribute("aria-label"));
        var buttons = canvas.FindAll("[role=toolbar] button:not([aria-pressed])");
        Assert.Equal(["Annuler le dernier trait", "Effacer le dessin"], buttons.Select(button => button.GetAttribute("aria-label")));
        Assert.All(buttons, button => Assert.True(button.HasAttribute("disabled")));
        Assert.Empty(canvas.FindAll("[role=toolbar] output"));
    }

    [Fact]
    public void ShowToolbar_False_DrawsNoToolbar()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.ShowToolbar, false));

        Assert.Empty(canvas.FindAll("[role=toolbar]"));
        Assert.Single(canvas.FindAll("canvas"));
    }

    [Fact]
    public void Toolbar_IsDisabled_WhenTheCanvasIsReadOnlyOrDisabled()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.ReadOnly, true).Add(c => c.Value, [Stroke()]));

        Assert.True(canvas.Find("input[type=color]").HasAttribute("disabled"));
        Assert.True(canvas.Find("input[type=range]").HasAttribute("disabled"));
        Assert.All(canvas.FindAll("[role=toolbar] button"), button => Assert.True(button.HasAttribute("disabled")));

        canvas.Render(parameters => parameters.Add(c => c.ReadOnly, false));
        Assert.False(canvas.Find("input[type=color]").HasAttribute("disabled"));
        Assert.All(canvas.FindAll("[role=toolbar] button"), button => Assert.False(button.HasAttribute("disabled")));
    }

    [Fact]
    public void Swatch_FollowsTheThemeColourTheScriptResolved_WhenThePenIsTheThemeColour()
    {
        _module.Setup<string?>("attach", _ => true).SetResult("#e0e0e0");

        var canvas = RenderCanvas();

        Assert.Equal("currentColor", canvas.Find("canvas").GetAttribute("data-omni-ink-color"));
        Assert.Equal("#e0e0e0", Swatch(canvas));
    }

    /// <summary>The colour the swatch shows.</summary>
    private static string? Swatch(IRenderedComponent<OmniInkCanvas> canvas) => canvas.Find("input[type=color]").GetAttribute("value");

    /// <summary>Reports <paramref name="color"/> as the theme colour the script resolved.</summary>
    private static Task ThemeColorAsync(IRenderedComponent<OmniInkCanvas> canvas, string color) =>
        canvas.InvokeAsync(() => canvas.Instance.HandleThemeColorAsync(color));

    [Fact]
    public async Task Swatch_FollowsALaterThemeChangeTheScriptReports_ButNotAChosenColour()
    {
        var canvas = RenderCanvas();
        Assert.Equal("#000000", Swatch(canvas));

        await ThemeColorAsync(canvas, "#101010");
        Assert.Equal("#101010", Swatch(canvas));

        canvas.Find("input[type=color]").Input("#ff0000");
        await ThemeColorAsync(canvas, "#202020");
        Assert.Equal("#FF0000", Swatch(canvas));
    }

    [Fact]
    public async Task ThemeColour_ThatIsNotAHex_TheSameAgain_OrAfterDispose_ChangesNothing()
    {
        var canvas = RenderCanvas();
        await ThemeColorAsync(canvas, "#101010");

        await ThemeColorAsync(canvas, "rgb(1, 2, 3)");
        Assert.Equal("#101010", Swatch(canvas));

        await ThemeColorAsync(canvas, "#101010");
        Assert.Equal("#101010", Swatch(canvas));

        await canvas.Instance.DisposeAsync();
        await ThemeColorAsync(canvas, "#202020");
        Assert.Equal("#101010", Swatch(canvas));
    }

    [Fact]
    public void Pen_ByDefault_IsTheThemeColourAndAWidthOfTwo()
    {
        var canvas = RenderCanvas();

        var surface = canvas.Find("canvas");
        Assert.Equal("currentColor", surface.GetAttribute("data-omni-ink-color"));
        Assert.Equal("2", surface.GetAttribute("data-omni-ink-width"));
        Assert.Equal("#000000", Swatch(canvas));
    }

    [Fact]
    public void ColourParameter_ColoursTheNextStrokes_AndTheToolbarFieldOverridesItUntilTheParameterChanges()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Color, "#336699"));
        Assert.Equal("#336699", canvas.Find("canvas").GetAttribute("data-omni-ink-color"));
        Assert.Equal("#336699", Swatch(canvas));

        canvas.Find("input[type=color]").Input("#ff0000");
        Assert.Equal("#FF0000", canvas.Find("canvas").GetAttribute("data-omni-ink-color"));

        canvas.Render();
        Assert.Equal("#FF0000", canvas.Find("canvas").GetAttribute("data-omni-ink-color"));

        canvas.Render(parameters => parameters.Add(c => c.Color, "#00ff00"));
        Assert.Equal("#00ff00", canvas.Find("canvas").GetAttribute("data-omni-ink-color"));
    }

    [Fact]
    public void ANamedColour_DrawsInThatColour_WhileTheFieldShowsBlack()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Color, "navy"));

        Assert.Equal("navy", canvas.Find("canvas").GetAttribute("data-omni-ink-color"));
        Assert.Equal("#000000", Swatch(canvas));
    }

    [Fact]
    public void WidthParameter_SetsTheNextStrokes_AndTheSliderOverridesItUntilTheParameterChanges()
    {
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.StrokeWidth, 3.5));
        Assert.Equal("3.5", canvas.Find("canvas").GetAttribute("data-omni-ink-width"));

        canvas.Find("input[type=range]").Input("7");
        Assert.Equal("7", canvas.Find("canvas").GetAttribute("data-omni-ink-width"));

        canvas.Render();
        Assert.Equal("7", canvas.Find("canvas").GetAttribute("data-omni-ink-width"));

        canvas.Render(parameters => parameters.Add(c => c.StrokeWidth, 5));
        Assert.Equal("5", canvas.Find("canvas").GetAttribute("data-omni-ink-width"));
    }

    // ----- Export ------------------------------------------------------------------------------------

    [Fact]
    public async Task ExportPng_AsksTheScriptToDrawTheStrokes_AtTheAskedSize_AndReturnsItsBytes()
    {
        byte[] png = [137, 80, 78, 71];
        _module.Setup<byte[]>("exportPng", _ => true).SetResult(png);
        var canvas = RenderCanvas(parameters => parameters.Add(c => c.Value, [Stroke()]));

        var bytes = await canvas.Instance.ExportPngAsync(300, 100, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(png, bytes);
        var arguments = Assert.Single(_module.Invocations["exportPng"]).Arguments;
        Assert.Equal(["#abcdef"], (string[])arguments[0]!);
        Assert.Equal([3d], (double[])arguments[1]!);
        Assert.Equal(new[] { new[] { 0.25, 0.5, -1d, 0.75, 0.5, 0.75 } }, (double[][])arguments[2]!);
        Assert.Equal(300, arguments[3]);
        Assert.Equal(100, arguments[4]);
    }

    [Fact]
    public async Task ExportDataUrl_IsThePngAsABase64Address()
    {
        _module.Setup<byte[]>("exportPng", _ => true).SetResult([1, 2, 3]);
        var canvas = RenderCanvas();

        var url = await canvas.Instance.ExportDataUrlAsync(8, 4, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal("data:image/png;base64,AQID", url);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(8193, 10)]
    [InlineData(10, 0)]
    [InlineData(10, 8193)]
    [InlineData(-5, 10)]
    public async Task Export_RefusesASizeOutsideOneTo8192(int width, int height)
    {
        var canvas = RenderCanvas();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => canvas.Instance.ExportPngAsync(width, height, Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => canvas.Instance.ExportDataUrlAsync(width, height, Xunit.TestContext.Current.CancellationToken));
        Assert.Empty(_module.Invocations["exportPng"]);
    }

    [Fact]
    public async Task Export_Works_EvenOnTheLargestAcceptedSize()
    {
        _module.Setup<byte[]>("exportPng", _ => true).SetResult([]);
        var canvas = RenderCanvas();

        await canvas.Instance.ExportPngAsync(8192, 8192, Xunit.TestContext.Current.CancellationToken);
        await canvas.Instance.ExportPngAsync(1, 1, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(2, _module.Invocations["exportPng"].Count);
    }

    [Fact]
    public async Task Export_WithACancelledToken_Throws_AndNeverReachesTheScript()
    {
        var canvas = RenderCanvas();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canvas.Instance.ExportPngAsync(10, 10, source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canvas.Instance.ExportDataUrlAsync(10, 10, source.Token));

        Assert.Empty(_module.Invocations["exportPng"]);
    }

    [Fact]
    public async Task Export_PassesTheTokenToTheScriptCall_AndCancellingWhilePendingThrowsWithoutAResult()
    {
        _module.Setup<byte[]>("exportPng", _ => true);
        var canvas = RenderCanvas();
        using var source = new CancellationTokenSource();

        var export = canvas.Instance.ExportPngAsync(10, 10, source.Token);
        var invocation = Assert.Single(_module.Invocations["exportPng"]);
        Assert.Equal(source.Token, invocation.CancellationToken);
        Assert.False(export.IsCompleted);

        await source.CancelAsync();

        // bUnit's double reports a cancelled pending call as its own exception rather than an OperationCanceledException;
        // the point here is that the token reached the call and the export yields no result once it is cancelled.
        await Assert.ThrowsAnyAsync<Exception>(() => export);
        Assert.False(export.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Export_CancelledWhileTheScriptLoads_Throws_AndLeavesTheSharedLoadAlone()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var canvas = RenderCanvas();
        using var source = new CancellationTokenSource();

        var export = canvas.Instance.ExportPngAsync(10, 10, source.Token);
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => export);
        Assert.Empty(runtime.Module.Calls);
        runtime.PendingImport.SetResult(runtime.Module);
    }

    // ----- Localization ------------------------------------------------------------------------------

    [Fact]
    public void Texts_AreLocalized()
    {
        var english = new System.Globalization.CultureInfo("en");
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = english;
        try
        {
            var canvas = RenderCanvas();

            Assert.Equal("Drawing area", canvas.Find("canvas").GetAttribute("aria-label"));
            Assert.Equal("Drawing tools", canvas.Find("[role=toolbar]").GetAttribute("aria-label"));
            Assert.Equal("Undo last stroke", canvas.FindAll("[role=toolbar] button:not([aria-pressed])")[0].GetAttribute("aria-label"));
            Assert.Equal("Clear drawing", canvas.FindAll("[role=toolbar] button:not([aria-pressed])")[1].GetAttribute("aria-label"));
            Assert.Equal("Stroke colour", canvas.Find("input[type=color]").GetAttribute("aria-label"));
            Assert.Equal(["Pen", "Eraser"], canvas.FindAll("[role=toolbar] button[aria-pressed]").Select(button => button.GetAttribute("aria-label")));
            Assert.Equal("Stroke width", canvas.Find("input[type=range]").GetAttribute("aria-label"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }
}
