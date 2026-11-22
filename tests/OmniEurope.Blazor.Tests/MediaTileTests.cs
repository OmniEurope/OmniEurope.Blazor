using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMediaTile"/> in bUnit. The picture, the playback and the pixels are the script's (its node tests
/// cover the rectangle arithmetic, the showcase probe the real video, the overlay and the export); here the markup, the
/// parameters, the pass-through to the drawing layer, the strokes the script cannot see, the localized texts and the
/// calls the component makes to its module and their arguments are read back from bUnit's interop.
/// </summary>
public sealed class MediaTileTests : OmniBunitContext
{
    private readonly BunitJSModuleInterop _module;

    public MediaTileTests() => _module = JSInterop.SetupModule(OmniModules.MediaTile);

    private IRenderedComponent<OmniMediaTile> RenderTile(Action<ComponentParameterCollectionBuilder<OmniMediaTile>>? configure = null)
        => Render<OmniMediaTile>(parameters => configure?.Invoke(parameters));

    private static OmniInkStroke Stroke(string id = "s1", string color = "#abcdef")
        => new(color, 3, [new OmniInkPoint(0.25, 0.5, null), new OmniInkPoint(0.75, 0.5, 0.75)]) { Id = id };

    /// <summary>The points of <see cref="Stroke"/> as the script receives them: x, y, pressure (-1 when none).</summary>
    private static double[][] StrokeFlat => new[] { new[] { 0.25, 0.5, -1d, 0.75, 0.5, 0.75 } };

    // ----- Markup ------------------------------------------------------------------------------------

    [Fact]
    public void Tile_IsANamedGroup_WithAnAutoplayInlineVideo_AndNoInlineStyle()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Name, "Ada Lovelace"));

        var root = tile.Find("div.omni-media-tile");
        Assert.Equal("group", root.GetAttribute("role"));
        Assert.Equal("Vidéo de Ada Lovelace", root.GetAttribute("aria-label"));
        var video = tile.Find("video.omni-media-tile__video");
        Assert.True(video.HasAttribute("autoplay"));
        Assert.True(video.HasAttribute("playsinline"));
        Assert.Equal("true", video.GetAttribute("aria-hidden"));
        Assert.DoesNotContain("style=", tile.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Label_ReplacesTheDefaultName_AndWithoutANameTheDefaultIsGeneric()
    {
        Assert.Equal("Vidéo", RenderTile().Find("div.omni-media-tile").GetAttribute("aria-label"));
        Assert.Equal("Salle", RenderTile(parameters => parameters.Add(t => t.Label, "Salle").Add(t => t.Name, "Ada")).Find("div.omni-media-tile").GetAttribute("aria-label"));
    }

    [Fact]
    public void IdClassAndAttributes_GoToTheRoot()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Id, "tile-1").Add(t => t.Class, "mine").AddUnmatched("data-x", "1"));

        var root = tile.Find("div.omni-media-tile");
        Assert.Equal("tile-1", root.Id);
        Assert.Contains("mine", root.ClassName, StringComparison.Ordinal);
        Assert.Equal("1", root.GetAttribute("data-x"));
    }

    [Fact]
    public void Muted_IsTrueByDefault_AndTheAttributeFollowsIt()
    {
        Assert.True(new OmniMediaTile().Muted);
        Assert.True(RenderTile().Find("video").HasAttribute("muted"));
        Assert.False(RenderTile(parameters => parameters.Add(t => t.Muted, false)).Find("video").HasAttribute("muted"));
    }

    [Theory]
    [InlineData(OmniMediaFit.Contain, "omni-media-tile--contain", "contain")]
    [InlineData(OmniMediaFit.Cover, "omni-media-tile--cover", "cover")]
    public void Fit_IsAClass_AndTheDataTheScriptReads(OmniMediaFit fit, string expectedClass, string expectedData)
    {
        var root = RenderTile(parameters => parameters.Add(t => t.Fit, fit)).Find("div.omni-media-tile");

        Assert.Contains(expectedClass, root.ClassName, StringComparison.Ordinal);
        Assert.Equal(expectedData, root.GetAttribute("data-omni-media-fit"));
    }

    [Fact]
    public void Fit_IsContainByDefault() => Assert.Equal(OmniMediaFit.Contain, new OmniMediaTile().Fit);

    // ----- No stream ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("Ada Lovelace", "AL")]
    [InlineData("  grace   brewster murray hopper", "GB")]
    [InlineData("linus", "L")]
    [InlineData("élodie Ünal", "ÉÜ")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void InitialsOf_TakesTheFirstLetterOfTheFirstTwoWords_InUpperCase(string? name, string expected)
        => Assert.Equal(expected, OmniMediaTile.InitialsOf(name));

    [Fact]
    public void InitialsOf_DoesNotCutAnEmojiInTwo() => Assert.Equal("\U0001F600A", OmniMediaTile.InitialsOf("\U0001F600 Alan"));

    [Fact]
    public void WithoutAStream_TheTileShowsADiscWithTheInitialsOfTheName_AndTheName()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Name, "Ada Lovelace"));

        Assert.Equal("AL", tile.Find(".omni-media-tile__avatar").TextContent.Trim());
        Assert.Equal("true", tile.Find(".omni-media-tile__placeholder").GetAttribute("aria-hidden"));
        Assert.Equal("Ada Lovelace", tile.Find(".omni-media-tile__name").TextContent.Trim());
        Assert.Contains("omni-media-tile--empty", tile.Find("div.omni-media-tile").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Initials_ReplaceTheOnesOfTheName()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Name, "Ada Lovelace").Add(t => t.Initials, "XY"));

        Assert.Equal("XY", tile.Find(".omni-media-tile__avatar").TextContent.Trim());
    }

    [Fact]
    public void WithNeitherNameNorInitials_TheDiscShowsAPersonIcon_AndNoNameChip()
    {
        var tile = RenderTile();

        Assert.NotNull(tile.Find(".omni-media-tile__avatar .omni-icon"));
        Assert.Empty(tile.FindAll(".omni-media-tile__name"));
    }

    [Fact]
    public void WithAStream_ThePlaceholderIsGone_AndTheTileIsNotEmpty()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()).Add(t => t.Name, "Ada"));

        Assert.Empty(tile.FindAll(".omni-media-tile__placeholder"));
        Assert.DoesNotContain("omni-media-tile--empty", tile.Find("div.omni-media-tile").ClassName, StringComparison.Ordinal);
        Assert.Equal("Ada", tile.Find(".omni-media-tile__name").TextContent.Trim());
    }

    // ----- Status icons ------------------------------------------------------------------------------

    [Fact]
    public void WithoutAStatus_NoStatusIsShown() => Assert.Empty(RenderTile().FindAll(".omni-media-tile__status"));

    [Fact]
    public void EachStatus_IsAnIconWithALocalizedAccessibleName()
    {
        var tile = RenderTile(parameters => parameters
            .Add(t => t.MicrophoneMuted, true).Add(t => t.ScreenSharing, true).Add(t => t.CameraActive, true));

        var names = tile.FindAll(".omni-media-tile__badge").Select(badge => badge.GetAttribute("aria-label")).ToArray();
        Assert.Equal(["Microphone coupé", "Partage d'écran", "Caméra active"], names);
        Assert.All(tile.FindAll(".omni-media-tile__badge"), badge =>
        {
            Assert.Equal("img", badge.GetAttribute("role"));
            Assert.Equal(badge.GetAttribute("aria-label"), badge.GetAttribute("title"));
            Assert.NotNull(badge.QuerySelector(".omni-icon"));
        });
    }

    [Theory]
    [InlineData(true, false, false, "Microphone coupé")]
    [InlineData(false, true, false, "Partage d'écran")]
    [InlineData(false, false, true, "Caméra active")]
    public void OnlyTheStatusesThatAreOn_AreShown(bool microphone, bool screen, bool camera, string expected)
    {
        var tile = RenderTile(parameters => parameters
            .Add(t => t.MicrophoneMuted, microphone).Add(t => t.ScreenSharing, screen).Add(t => t.CameraActive, camera));

        Assert.Equal(expected, Assert.Single(tile.FindAll(".omni-media-tile__badge")).GetAttribute("aria-label"));
    }

    // ----- Action bar and full screen ----------------------------------------------------------------

    [Fact]
    public void ActionsContent_IsRenderedInTheActionBar_BeforeTheFullscreenButton()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.ActionsContent, (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "id", "host-capture");
            builder.CloseElement();
        })));

        var bar = tile.Find(".omni-media-tile__actions");
        Assert.Equal("toolbar", bar.GetAttribute("role"));
        var children = bar.Children;
        Assert.Equal("host-capture", children[0].Id);
        Assert.Contains("omni-media-tile__fullscreen", children[1].ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void FullscreenButton_IsShownByDefault_NamedAndLocalized()
    {
        var button = RenderTile().Find("button.omni-media-tile__fullscreen");

        Assert.Equal("Plein écran", button.GetAttribute("aria-label"));
    }

    [Fact]
    public void ShowFullscreenFalse_HidesTheButton_AndWithoutActionsTheBarIsGone()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.ShowFullscreen, false));

        Assert.Empty(tile.FindAll(".omni-media-tile__fullscreen"));
        Assert.Empty(tile.FindAll(".omni-media-tile__actions"));
    }

    [Fact]
    public void ShowFullscreenFalse_KeepsTheActionBar_WhenTheHostGivesActions()
    {
        var tile = RenderTile(parameters => parameters
            .Add(t => t.ShowFullscreen, false)
            .Add(t => t.ActionsContent, (RenderFragment)(builder => builder.AddContent(0, "x"))));

        Assert.Single(tile.FindAll(".omni-media-tile__actions"));
        Assert.Empty(tile.FindAll(".omni-media-tile__fullscreen"));
    }

    [Fact]
    public async Task TheButton_AsksTheScriptToToggleFullscreen_AndTheLabelFollowsTheBrowserState()
    {
        _module.Setup<bool>("toggleFullscreen", _ => true).SetResult(true);
        var tile = RenderTile();

        await tile.Find("button.omni-media-tile__fullscreen").ClickAsync(new());

        Assert.Single(_module.Invocations["toggleFullscreen"]);
        Assert.Equal("Quitter le plein écran", tile.Find("button.omni-media-tile__fullscreen").GetAttribute("aria-label"));
        Assert.Contains("omni-media-tile--fullscreen", tile.Find("div.omni-media-tile").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ARefusedToggle_LeavesTheTileAsItWas()
    {
        _module.Setup<bool>("toggleFullscreen", _ => true).SetResult(false);
        var tile = RenderTile();

        await tile.Instance.ToggleFullscreenAsync();

        Assert.Equal("Plein écran", tile.Find("button.omni-media-tile__fullscreen").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task TheFullscreenState_FollowsTheBrowser_WhateverCausedTheChange()
    {
        var tile = RenderTile();

        await tile.InvokeAsync(() => tile.Instance.HandleFullscreenAsync(true));
        Assert.Equal("Quitter le plein écran", tile.Find("button.omni-media-tile__fullscreen").GetAttribute("aria-label"));

        await tile.InvokeAsync(() => tile.Instance.HandleFullscreenAsync(false));
        Assert.Equal("Plein écran", tile.Find("button.omni-media-tile__fullscreen").GetAttribute("aria-label"));
    }

    // ----- Script calls ------------------------------------------------------------------------------

    [Fact]
    public void FirstRender_AttachesTheScript_AndSilencesNothingElse()
    {
        RenderTile();

        Assert.Single(_module.Invocations["attach"]);
        Assert.Equal(true, Assert.Single(_module.Invocations["setMuted"]).Arguments[1]);
        Assert.Empty(_module.Invocations["setStream"]);
    }

    [Fact]
    public void AStream_IsHandedToTheScript_AReplacementReplacesIt_AndNullClearsIt()
    {
        var first = new FakeStream();
        var second = new FakeStream();
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, first));
        Assert.Same(first, Assert.Single(_module.Invocations["setStream"]).Arguments[1]);

        tile.Render(parameters => parameters.Add(t => t.Stream, second));
        Assert.Equal(2, _module.Invocations["setStream"].Count);
        Assert.Same(second, _module.Invocations["setStream"][1].Arguments[1]);

        tile.Render(parameters => parameters.Add(t => t.Stream, second).Add(t => t.Name, "Ada"));
        Assert.Equal(2, _module.Invocations["setStream"].Count);

        tile.Render(parameters => parameters.Add(t => t.Stream, null));
        Assert.Equal(3, _module.Invocations["setStream"].Count);
        Assert.Null(_module.Invocations["setStream"][2].Arguments[1]);
    }

    [Fact]
    public void TheSoundIsGivenToTheScript_OnlyWhenItChanges()
    {
        var tile = RenderTile();
        tile.Render(parameters => parameters.Add(t => t.Muted, true));
        Assert.Single(_module.Invocations["setMuted"]);

        tile.Render(parameters => parameters.Add(t => t.Muted, false));
        Assert.Equal(2, _module.Invocations["setMuted"].Count);
        Assert.Equal(false, _module.Invocations["setMuted"][1].Arguments[1]);
    }

    [Fact]
    public void FitAndDrawing_AskTheScriptToPlaceTheOverlayAgain_OnlyWhenTheyChange()
    {
        var tile = RenderTile();
        var first = _module.Invocations["refresh"].Count;

        tile.Render(parameters => parameters.Add(t => t.Name, "Ada"));
        Assert.Equal(first, _module.Invocations["refresh"].Count);

        tile.Render(parameters => parameters.Add(t => t.Fit, OmniMediaFit.Cover));
        Assert.Equal(first + 1, _module.Invocations["refresh"].Count);

        tile.Render(parameters => parameters.Add(t => t.Fit, OmniMediaFit.Cover).Add(t => t.Drawing, true));
        Assert.Equal(first + 2, _module.Invocations["refresh"].Count);
    }

    [Fact]
    public async Task Dispose_DetachesTheScript_WithoutStoppingAnyTrack_AndDisposesTheModule()
    {
        var stream = new FakeStream();
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, stream));

        await tile.Instance.DisposeAsync();

        Assert.Single(_module.Invocations["detach"]);
        Assert.Equal(0, stream.Calls);
    }

    [Fact]
    public async Task DisposingBeforeTheRenderHappened_DoesNothing() => await new OmniMediaTile().DisposeAsync();

    // ----- Drawing layer -----------------------------------------------------------------------------

    [Fact]
    public void WithoutDrawing_ThereIsNoOverlayAndNoToolbar()
    {
        var tile = RenderTile();

        Assert.Empty(tile.FindAll("[data-omni-media-picture]"));
        Assert.Empty(tile.FindAll(".omni-media-tile__toolbar"));
        Assert.Empty(tile.FindComponents<OmniInkCanvas>());
    }

    [Fact]
    public void Drawing_PutsAnInkCanvasInTheOverlay_WithoutItsOwnToolbar_AndTheToolbarAboveTheStage()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Drawing, true));

        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>());
        Assert.False(ink.Instance.ShowToolbar);
        Assert.NotNull(tile.Find("[data-omni-media-picture] .omni-ink-canvas"));
        Assert.Empty(tile.FindAll(".omni-media-tile__stage .omni-media-tile__toolbar"));
        var toolbar = tile.Find(".omni-media-tile__toolbar");
        Assert.Equal("toolbar", toolbar.GetAttribute("role"));
        Assert.Equal("Outils de dessin", toolbar.GetAttribute("aria-label"));
        Assert.Equal(["Crayon", "Gomme"], tile.FindAll(".omni-media-tile__tool").Select(button => button.GetAttribute("aria-label")).ToArray());
        Assert.Equal("Dessin sur la vidéo", tile.Find("canvas").GetAttribute("aria-label"));
    }

    [Fact]
    public void ShowDrawingToolbarFalse_HidesTheToolbar_AndKeepsTheOverlay()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.ShowDrawingToolbar, false));

        Assert.Empty(tile.FindAll(".omni-media-tile__toolbar"));
        Assert.Single(tile.FindComponents<OmniInkCanvas>());
    }

    [Fact]
    public void DrawingReadOnly_IsOffByDefault() => Assert.False(new OmniMediaTile().DrawingReadOnly);

    [Fact]
    public void DrawingReadOnly_ShowsTheStrokesOnALockedCanvas_WithoutAToolbar_AndLetsThePointerThrough()
    {
        var strokes = new[] { Stroke() };
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.DrawingReadOnly, true)
            .Add(t => t.Strokes, strokes));

        Assert.Empty(tile.FindAll(".omni-media-tile__toolbar"));
        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;
        Assert.True(ink.ReadOnly);
        Assert.Same(strokes, ink.Value);
        Assert.Contains("omni-media-tile__picture--readonly", tile.Find("[data-omni-media-picture]").ClassList);
        Assert.Equal("true", tile.Find("canvas").GetAttribute("data-omni-ink-locked"));
    }

    [Fact]
    public void DrawingReadOnly_WithoutDrawing_ShowsNothing()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.DrawingReadOnly, true).Add(t => t.Strokes, [Stroke()]));

        Assert.Empty(tile.FindAll("[data-omni-media-picture]"));
        Assert.Empty(tile.FindAll(".omni-media-tile__toolbar"));
    }

    [Fact]
    public void Editing_KeepsTheLayerWithoutTheReadOnlyClass()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Drawing, true));

        Assert.DoesNotContain("omni-media-tile__picture--readonly", tile.Find("[data-omni-media-picture]").ClassList);
        Assert.False(Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance.ReadOnly);
        Assert.Null(tile.Find("canvas").GetAttribute("data-omni-ink-locked"));
    }

    [Fact]
    public async Task DrawingReadOnly_RaisesNoStrokeEvent_FromAStrokeAnEraseOrAKey()
    {
        var events = 0;
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.DrawingReadOnly, true)
            .Add(t => t.Strokes, [Stroke("a"), Stroke("b")])
            .Add(t => t.StrokesChanged, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, _ => events++))
            .Add(t => t.StrokeAdded, EventCallback.Factory.Create<OmniInkStroke>(this, _ => events++))
            .Add(t => t.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, _ => events++)));
        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;

        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#123456", 4, [0.1, 0.2, -1, 0.3, 0.4, -1]));
        await tile.InvokeAsync(() => ink.HandleEraseAsync([0], 2));
        await tile.InvokeAsync(ink.HandleUndoKeyAsync);
        await tile.InvokeAsync(ink.HandleClearKeyAsync);

        Assert.Equal(0, events);
        Assert.Equal(2, ink.Value.Count);
    }

    [Fact]
    public async Task DrawingReadOnly_SwitchedOff_LetsTheUserDrawAgain()
    {
        var added = 0;
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.DrawingReadOnly, true)
            .Add(t => t.StrokeAdded, EventCallback.Factory.Create<OmniInkStroke>(this, _ => added++)));

        tile.Render(parameters => parameters.Add(t => t.DrawingReadOnly, false));
        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;
        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#123456", 4, [0.1, 0.2, -1, 0.3, 0.4, -1]));

        Assert.Equal(1, added);
        Assert.NotEmpty(tile.FindAll(".omni-media-tile__toolbar"));
    }

    [Fact]
    public async Task Export_InReadOnly_HoldsTheStrokesAsInEditing()
    {
        _module.Setup<byte[]?>("exportPng", _ => true).SetResult([1]);
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Stream, new FakeStream()).Add(t => t.Drawing, true).Add(t => t.DrawingReadOnly, true).Add(t => t.Strokes, [Stroke()]));

        await tile.Instance.ExportPngAsync(Xunit.TestContext.Current.CancellationToken);

        var arguments = Assert.Single(_module.Invocations["exportPng"]).Arguments;
        Assert.Equal(["#abcdef"], (string[])arguments[1]!);
        Assert.Equal(StrokeFlat, (double[][])arguments[3]!);
    }

    [Fact]
    public void TheParametersAreGivenToTheCanvas()
    {
        var strokes = new[] { Stroke() };
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.Strokes, strokes)
            .Add(t => t.DrawingTool, OmniInkTool.Eraser)
            .Add(t => t.StrokeColor, "#112233")
            .Add(t => t.StrokeWidth, 5));

        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;
        Assert.Same(strokes, ink.Value);
        Assert.Equal(OmniInkTool.Eraser, ink.Tool);
        Assert.Equal("#112233", ink.Color);
        Assert.Equal(5, ink.StrokeWidth);
        Assert.Equal("#ff3b30", new OmniMediaTile().StrokeColor);
    }

    [Fact]
    public async Task TheCanvasEvents_ArePassedOn()
    {
        var changed = new List<IReadOnlyList<OmniInkStroke>>();
        var added = new List<OmniInkStroke>();
        var removed = new List<IReadOnlyList<OmniInkStroke>>();
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.StrokesChanged, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, list => changed.Add(list)))
            .Add(t => t.StrokeAdded, EventCallback.Factory.Create<OmniInkStroke>(this, stroke => added.Add(stroke)))
            .Add(t => t.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, list => removed.Add(list))));
        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;

        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#123456", 4, [0.1, 0.2, -1, 0.3, 0.4, -1]));
        var drawn = Assert.Single(added);
        Assert.Equal(drawn, Assert.Single(Assert.Single(changed)));
        Assert.NotNull(drawn.Id);

        await tile.InvokeAsync(ink.UndoAsync);
        Assert.Equal(drawn, Assert.Single(Assert.Single(removed)));
        Assert.Equal(2, changed.Count);
        Assert.Empty(changed[1]);
    }

    [Fact]
    public async Task StrokesTheHostGives_AreNotReported()
    {
        var added = 0;
        var removed = 0;
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.StrokeAdded, EventCallback.Factory.Create<OmniInkStroke>(this, _ => added++))
            .Add(t => t.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, _ => removed++)));

        tile.Render(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.Strokes, [Stroke("a"), Stroke("b")]));
        tile.Render(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.Strokes, [Stroke("c")]));
        await Task.Yield();

        Assert.Equal(0, added);
        Assert.Equal(0, removed);
    }

    /// <summary>Renders a drawing tile that adds each tool chosen on its toolbar to <paramref name="chosen"/>.</summary>
    private IRenderedComponent<OmniMediaTile> RenderDrawingTile(List<OmniInkTool> chosen) =>
        RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.DrawingToolChanged, EventCallback.Factory.Create<OmniInkTool>(this, tool => chosen.Add(tool))));

    [Fact]
    public async Task TheToolbarToolChoice_IsRaised_AndStaysUntilTheParameterChanges()
    {
        var chosen = new List<OmniInkTool>();
        var tile = RenderDrawingTile(chosen);

        await tile.FindAll(".omni-media-tile__tool")[1].ClickAsync(new());

        Assert.Equal([OmniInkTool.Eraser], chosen);
        Assert.Equal(OmniInkTool.Eraser, Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance.Tool);

        // The choice stays through a render that changes nothing about the tool...
        tile.Render(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.DrawingTool, OmniInkTool.Pen).Add(t => t.StrokeWidth, 3));
        Assert.Equal(OmniInkTool.Eraser, Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance.Tool);

        // ...and the parameter wins again when it changes.
        tile.Render(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.DrawingTool, OmniInkTool.Eraser));
        tile.Render(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.DrawingTool, OmniInkTool.Pen));
        Assert.Equal(OmniInkTool.Pen, Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance.Tool);
    }

    [Fact]
    public async Task UndoAndClear_ActOnTheCanvas_AndAreDisabledWithoutStrokes()
    {
        var removed = new List<IReadOnlyList<OmniInkStroke>>();
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, list => removed.Add(list))));
        var buttons = () => tile.FindAll(".omni-media-tile__toolbar button").Where(b => b.GetAttribute("title") is "Annuler le dernier trait" or "Effacer le dessin").ToArray();
        Assert.All(buttons(), button => Assert.True(button.HasAttribute("disabled")));

        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;
        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#123456", 4, [0.1, 0.2, -1, 0.3, 0.4, -1]));
        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#654321", 4, [0.5, 0.6, -1, 0.7, 0.8, -1]));
        Assert.All(buttons(), button => Assert.False(button.HasAttribute("disabled")));

        await buttons()[1].ClickAsync(new());

        Assert.Equal(2, Assert.Single(removed).Count);
        Assert.All(buttons(), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public async Task ChoosingTheToolThatIsAlreadyCurrent_RaisesNothing()
    {
        var chosen = new List<OmniInkTool>();
        var tile = RenderDrawingTile(chosen);

        await tile.FindAll(".omni-media-tile__tool")[0].ClickAsync(new());

        Assert.Empty(chosen);
        Assert.Equal(OmniInkTool.Pen, Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance.Tool);
    }

    [Fact]
    public void TheToolbarColourAndWidthChoices_GoToTheCanvas_InPlaceOfTheParameters()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.StrokeColor, "#112233").Add(t => t.StrokeWidth, 5));
        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;
        Assert.Equal("#112233", ink.Color);
        Assert.Equal(5, ink.StrokeWidth);

        tile.Find(".omni-media-tile__color input").Input("#00ff00");
        tile.Find(".omni-media-tile__width input").Input("7");

        Assert.Equal("#00ff00", ink.Color, ignoreCase: true);
        Assert.Equal("#00ff00", tile.Find(".omni-media-tile__color input").GetAttribute("value"), ignoreCase: true);
        Assert.Equal(7, ink.StrokeWidth);
    }

    [Theory]
    [InlineData("#aabbcc", "#aabbcc")]
    [InlineData("red", "#000000")]
    [InlineData("#abc", "#000000")]
    [InlineData("0123456", "#000000")]
    [InlineData("#12345g", "#000000")]
    public void ThePickerShowsAHexColourAsItIs_AndBlackForAnyOtherTextTheCanvasStillReceives(string strokeColor, string expectedPicker)
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Drawing, true).Add(t => t.StrokeColor, strokeColor));

        Assert.Equal(expectedPicker, tile.Find(".omni-media-tile__color input").GetAttribute("value"));
        Assert.Equal(strokeColor, Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance.Color);
    }

    [Fact]
    public async Task Undo_TakesBackTheLastStroke_AndIsRaisedAsARemoval()
    {
        var removed = new List<IReadOnlyList<OmniInkStroke>>();
        var changed = new List<IReadOnlyList<OmniInkStroke>>();
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Drawing, true)
            .Add(t => t.StrokesChanged, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, list => changed.Add(list)))
            .Add(t => t.StrokesRemoved, EventCallback.Factory.Create<IReadOnlyList<OmniInkStroke>>(this, list => removed.Add(list))));
        var ink = Assert.Single(tile.FindComponents<OmniInkCanvas>()).Instance;
        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#123456", 4, [0.1, 0.2, -1, 0.3, 0.4, -1]));
        await tile.InvokeAsync(() => ink.HandleStrokeAsync("#654321", 4, [0.5, 0.6, -1, 0.7, 0.8, -1]));

        await tile.Find(".omni-media-tile__toolbar button[title='Annuler le dernier trait']").ClickAsync(new());

        Assert.Equal("#654321", Assert.Single(Assert.Single(removed)).Color);
        Assert.Equal("#123456", Assert.Single(changed[^1]).Color);
    }

    [Fact]
    public async Task TheFullscreenStateTheScriptReports_ThroughItsBridge_ReachesTheTile()
    {
        var tile = RenderTile();
        var bridge = ((DotNetObjectReference<MediaTileInteropBridge>)Assert.Single(_module.Invocations["attach"]).Arguments[1]!).Value;

        await tile.InvokeAsync(() => bridge.OnFullscreenChanged(true));
        Assert.Contains("omni-media-tile--fullscreen", tile.Find("div.omni-media-tile").ClassName, StringComparison.Ordinal);

        await tile.InvokeAsync(() => bridge.OnFullscreenChanged(false));
        Assert.DoesNotContain("omni-media-tile--fullscreen", tile.Find("div.omni-media-tile").ClassName, StringComparison.Ordinal);
    }

    // ----- A lost circuit ----------------------------------------------------------------------------

    [Fact]
    public void ALostCircuitWhileAttaching_IsIgnored_AndTheScriptIsNotCalledAgain()
    {
        _module.SetupVoid("attach", _ => true).SetException(new JSDisconnectedException("circuit perdu"));

        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()));

        Assert.Single(_module.Invocations["attach"]);
        Assert.Empty(_module.Invocations["setStream"]);
        Assert.Single(tile.FindAll("div.omni-media-tile"));
    }

    [Fact]
    public async Task ALostCircuitWhileDetaching_DoesNotFailTheDisposal()
    {
        _module.SetupVoid("detach", _ => true).SetException(new JSDisconnectedException("circuit perdu"));
        var tile = RenderTile();

        await tile.Instance.DisposeAsync();

        Assert.Single(_module.Invocations["detach"]);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void AStrokeWidthThatIsNotAboveZero_IsRefused(double width)
        => Assert.Throws<ArgumentOutOfRangeException>(() => RenderTile(parameters => parameters.Add(t => t.StrokeWidth, width)));

    // ----- Export ------------------------------------------------------------------------------------

    [Fact]
    public async Task Export_WithoutAStream_Throws_AndNeverReachesTheScript()
    {
        var tile = RenderTile();

        await Assert.ThrowsAsync<InvalidOperationException>(() => tile.Instance.ExportPngAsync(Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => tile.Instance.ExportDataUrlAsync(Xunit.TestContext.Current.CancellationToken));
        Assert.Empty(_module.Invocations["exportPng"]);
    }

    [Fact]
    public async Task Export_WithoutAFrameYet_Throws()
    {
        _module.Setup<byte[]?>("exportPng", _ => true).SetResult(null);
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => tile.Instance.ExportPngAsync(Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Export_AsksTheScriptForTheFrameWithTheStrokes_AndReturnsItsBytes()
    {
        byte[] png = [137, 80, 78, 71];
        _module.Setup<byte[]?>("exportPng", _ => true).SetResult(png);
        var tile = RenderTile(parameters => parameters
            .Add(t => t.Stream, new FakeStream()).Add(t => t.Drawing, true).Add(t => t.Strokes, [Stroke()]));

        var bytes = await tile.Instance.ExportPngAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(png, bytes);
        var arguments = Assert.Single(_module.Invocations["exportPng"]).Arguments;
        Assert.Equal(["#abcdef"], (string[])arguments[1]!);
        Assert.Equal([3d], (double[])arguments[2]!);
        Assert.Equal(StrokeFlat, (double[][])arguments[3]!);
    }

    [Fact]
    public async Task Export_WithDrawingOff_LeavesTheStrokesOut()
    {
        _module.Setup<byte[]?>("exportPng", _ => true).SetResult([1]);
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()).Add(t => t.Strokes, [Stroke()]));

        await tile.Instance.ExportPngAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Empty((string[])Assert.Single(_module.Invocations["exportPng"]).Arguments[1]!);
    }

    [Fact]
    public async Task ExportDataUrl_IsThePngAsABase64Address()
    {
        _module.Setup<byte[]?>("exportPng", _ => true).SetResult([1, 2, 3]);
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()));

        Assert.Equal("data:image/png;base64,AQID", await tile.Instance.ExportDataUrlAsync(Xunit.TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Export_WithACancelledToken_Throws_AndNeverReachesTheScript()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tile.Instance.ExportPngAsync(source.Token));

        Assert.Empty(_module.Invocations["exportPng"]);
    }

    [Fact]
    public async Task AfterDispose_ExportAndFullscreenThrowObjectDisposed()
    {
        var tile = RenderTile(parameters => parameters.Add(t => t.Stream, new FakeStream()));

        await tile.Instance.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => tile.Instance.ExportPngAsync(Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => tile.Instance.ExportDataUrlAsync(Xunit.TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => tile.Instance.ToggleFullscreenAsync());
    }

    [Fact]
    public async Task ADisposedTile_IgnoresTheScriptAndRenders()
    {
        var tile = RenderTile();
        await tile.Instance.DisposeAsync();

        await tile.InvokeAsync(() => tile.Instance.HandleFullscreenAsync(true));
        tile.Render(parameters => parameters.Add(t => t.Stream, new FakeStream()));

        Assert.Empty(_module.Invocations["setStream"]);
    }

    // A stand-in for a MediaStream reference the application made: the tile only passes it on.
    private sealed class FakeStream : IJSObjectReference
    {
        public int Calls { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls++;
            throw new NotSupportedException();
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls++;
            throw new NotSupportedException();
        }

        public ValueTask DisposeAsync()
        {
            Calls++;
            return ValueTask.CompletedTask;
        }
    }
}
