using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniMindMap and its toolbar and panel at their edges: the host's labels whole or partial, no document,
/// the map's own document handed back, a map locked while linking, a selection a new document drops, the
/// canvas script failing, gone or answering nothing, and the panel and toolbar used without a selection
/// or outside a map.
/// </summary>
public sealed class DiagramEdgeTests : OmniBunitContext
{
    private static void Press(IRenderedComponent<OmniMindMap> map, string key) =>
        map.Find("svg.omni-mindmap__canvas").KeyDown(new KeyboardEventArgs { Key = key });

    [Fact]
    public void Labels_OfTheHost_ReplaceTheDefaults_AndMissingOnesFallBack()
    {
        var labels = new OmniMindMapLabels
        {
            Rename = "R", Duplicate = "D", AddLink = "L", LinkSelectSource = "S", LinkSelectTarget = "T", Center = "C", Delete = "X"
        };
        var full = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()).Add(component => component.ContextLabels, labels));
        var empty = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()).Add(component => component.ContextLabels, new OmniMindMapLabels()));

        Assert.Equal(["R", "D", "L", "S", "T", "C", "X"], [full.Instance.RenameLabel, full.Instance.DuplicateLabel, full.Instance.AddLinkLabel,
            full.Instance.LinkSelectSourceLabel, full.Instance.LinkSelectTargetLabel, full.Instance.CenterLabel, full.Instance.DeleteLabel]);
        Assert.All([empty.Instance.RenameLabel, empty.Instance.DuplicateLabel, empty.Instance.AddLinkLabel, empty.Instance.LinkSelectSourceLabel,
            empty.Instance.LinkSelectTargetLabel, empty.Instance.CenterLabel, empty.Instance.DeleteLabel], label => Assert.False(string.IsNullOrWhiteSpace(label)));
    }

    [Fact]
    public async Task MapWithoutDocument_IsEmpty_LockedCannotUndo_AndCentersNothing()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.Empty(map.FindAll(".omni-mindmap__node"));
        Assert.False(map.Instance.CanUndo);
        await map.InvokeAsync(map.Instance.CenterOnSelectionAsync);
    }

    [Fact]
    public void DocumentHandedBack_IsKeptAsItIs_AndANewOneWithoutTheSelectionReportsNoSelection()
    {
        OmniMindMapDocument? emitted = null;
        var selections = new List<OmniMindMapNode?>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.DocumentChanged, document => emitted = document)
            .Add(component => component.OnNodeSelect, node => selections.Add(node)));
        Press(map, "Home");
        Press(map, "n");
        Assert.NotNull(emitted);

        map.Render(parameters => parameters.Add(component => component.Document, emitted));
        map.Render(parameters => parameters.Add(component => component.Document, new OmniMindMapDocument { RootId = "x", Nodes = [new OmniMindMapNode { Id = "x", Label = "X" }] }));

        Assert.Null(selections[^1]);
    }

    [Fact]
    public void MapLockedWhileLinking_LeavesTheLinkMode()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.DocumentChanged, _ => { })
            .Add(component => component.ToolbarContent, builder =>
            {
                builder.OpenComponent<OmniMindMapToolbar>(0);
                builder.AddComponentParameter(1, nameof(OmniMindMapToolbar.Label), "Outils");
                builder.CloseComponent();
            }));
        Press(map, "Home");
        map.Find("[data-omni-mindmap-action=link]").Click();
        Assert.Equal("true", map.Find("[data-omni-mindmap-action=link]").GetAttribute("aria-pressed"));

        map.Render(parameters => parameters.Add(component => component.ReadOnly, true));

        Assert.Empty(map.FindAll(".omni-mindmap__node--link-source"));
    }

    [Fact]
    public void Toolbar_CentersAndFits()
    {
        var views = new List<OmniMindMapViewState>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.ViewStateChanged, view => views.Add(view))
            .Add(component => component.ToolbarContent, builder =>
            {
                builder.OpenComponent<OmniMindMapToolbar>(0);
                builder.CloseComponent();
            }));
        Press(map, "Home");

        map.Find("[data-omni-mindmap-action=center]").Click();
        map.FindAll("[data-omni-mindmap-action]").First(button => button.GetAttribute("data-omni-mindmap-action") is "fit" or "fit-view").Click();
        map.Render(parameters => parameters.Add(component => component.Label, "Carte"));

        Assert.NotNull(map.Find("[data-omni-mindmap-action=center]"));
    }

    [Fact]
    public void PanelAndToolbar_OutsideAMap_AreRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniMindMapNodeProperties>());
        Assert.Throws<InvalidOperationException>(() => Render<OmniMindMapToolbar>());
    }

    [Fact]
    public void Panel_StaysInPlaceWithoutSelection_SoPressingANodeDoesNotNarrowTheCanvas()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.PanelContent, builder =>
            {
                builder.OpenComponent<OmniMindMapNodeProperties>(0);
                builder.AddComponentParameter(1, nameof(OmniMindMapNodeProperties.Id), "proprietes");
                builder.CloseComponent();
            }));

        var empty = map.Find("section.omni-mindmap-properties");
        Assert.Contains("omni-mindmap-properties--empty", empty.ClassList);
        Assert.Equal("proprietes", empty.Id);
        Assert.Equal("Propriétés du nœud", map.Find($"#{empty.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Equal("Sélectionnez un nœud pour modifier ses propriétés.", empty.QuerySelector(".omni-mindmap-properties__hint")!.TextContent);
        Assert.Empty(map.FindAll(".omni-mindmap-properties input"));

        Press(map, "Home");

        var filled = map.Find("section.omni-mindmap-properties");
        Assert.DoesNotContain("omni-mindmap-properties--empty", filled.ClassList);
        Assert.NotEmpty(map.FindAll(".omni-mindmap-properties input"));
    }

    [Fact]
    public async Task Panel_WithoutSelection_ShowsTheRootGroup_EditsNothing_AndRefusesAnUnknownGroup()
    {
        var changes = new List<OmniMindMapDocument>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.DocumentChanged, document => changes.Add(document))
            .Add(component => component.PanelContent, builder =>
            {
                builder.OpenComponent<OmniMindMapNodeProperties>(0);
                builder.AddComponentParameter(1, nameof(OmniMindMapNodeProperties.Id), "proprietes");
                builder.CloseComponent();
            }));
        map.Render(parameters => parameters.Add(component => component.Label, "Carte"));
        Assert.Equal(OmniMindMapGroups.Root, OmniMindMapGroups.Resolve("inconnu"));
        Assert.Equal(OmniMindMapGroups.Root, OmniMindMapGroups.Resolve(null));

        Press(map, "Home");
        var select = map.FindAll("#proprietes-color, select").First();
        await select.ChangeAsync(new ChangeEventArgs { Value = "inconnu" });

        Assert.Empty(changes);
    }

    [Fact]
    public async Task Panel_WithASelection_RecoloursItAndFollowsANewPanelId()
    {
        OmniMindMapDocument current = MindMapComponentTests.Sample();
        RenderFragment Panel(string id) => builder =>
        {
            builder.OpenComponent<OmniMindMapNodeProperties>(0);
            builder.AddComponentParameter(1, nameof(OmniMindMapNodeProperties.Id), id);
            builder.CloseComponent();
        };
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, current)
            .Add(component => component.DocumentChanged, document => current = document)
            .Add(component => component.PanelContent, Panel("proprietes")));
        Press(map, "Home");

        map.Find(".omni-mindmap-properties .omni-mindmap__swatch--green").Click();
        Assert.Equal(OmniMindMapGroups.Green, current.Nodes.Single(node => node.Id == "root").Group);

        // The same map handed again with a new panel id: the panel stays subscribed and follows the map.
        map.Render(parameters => parameters.Add(component => component.PanelContent, Panel("autre")));
        await map.InvokeAsync(() => new MindMapInteropBridge(map.Instance).NodePressed("left"));
        Assert.Equal("autre", map.Find(".omni-mindmap-properties").Id);
        Assert.Equal("true", map.Find(".omni-mindmap-properties .omni-mindmap__swatch--green").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Toolbar_HandedANewLabel_StaysOnItsMap()
    {
        RenderFragment Toolbar(string label) => builder =>
        {
            builder.OpenComponent<OmniMindMapToolbar>(0);
            builder.AddComponentParameter(1, nameof(OmniMindMapToolbar.Label), label);
            builder.CloseComponent();
        };
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.DocumentChanged, _ => { })
            .Add(component => component.ToolbarContent, Toolbar("Outils")));

        map.Render(parameters => parameters.Add(component => component.ToolbarContent, Toolbar("Barre")));
        Press(map, "Home");

        Assert.Equal("Barre", map.Find(".omni-mindmap-toolbar").GetAttribute("aria-label"));
        Assert.Null(map.Find("[data-omni-mindmap-action=link]").GetAttribute("disabled"));
    }

    [Fact]
    public async Task DroppedUnknownNodes_ChangeNothing_AndALinkPressedAfterANode_ReportsNoNode()
    {
        var changes = new List<OmniMindMapDocument>();
        var selections = new List<OmniMindMapNode?>();
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.DocumentChanged, document => changes.Add(document))
            .Add(component => component.OnNodeSelect, node => selections.Add(node)));
        var bridge = new MindMapInteropBridge(map.Instance);

        await map.InvokeAsync(() => bridge.NodesMoved([new MindMapNodeMove("ghost", 1, 1), new MindMapNodeMove("left", double.NaN, 1)]));
        Assert.Empty(changes);

        Press(map, "Home");
        await map.InvokeAsync(() => bridge.EdgePressed(0));

        Assert.Equal("root", selections[0]!.Id);
        Assert.Null(selections[^1]);
    }

    [Fact]
    public async Task NewDocument_ForgetsALinkAndALinkSourceItNoLongerHas()
    {
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.DocumentChanged, _ => { }));
        var bridge = new MindMapInteropBridge(map.Instance);
        var smaller = MindMapComponentTests.Sample() with { Edges = [new OmniMindMapEdge { From = "root", To = "right" }] };

        await map.InvokeAsync(() => bridge.EdgePressed(2));
        map.Render(parameters => parameters.Add(component => component.Document, smaller));
        Assert.Empty(map.FindAll(".omni-mindmap__edge--selected"));

        await map.InvokeAsync(() => map.Instance.DispatchAsync(() => map.Instance.StartLinkModeAsync()));
        await map.InvokeAsync(() => bridge.NodePressed("left"));
        // A new document that still draws the source keeps it.
        map.Render(parameters => parameters.Add(component => component.Document, smaller with { }));
        Assert.Contains("Cliquez sur le nœud cible", map.Find(".omni-mindmap__banner").TextContent, StringComparison.Ordinal);
        map.Render(parameters => parameters.Add(component => component.Document,
            smaller with { Nodes = [.. smaller.Nodes.Where(node => node.Id != "left")] }));

        Assert.True(map.Instance.IsLinking);
        Assert.Contains("Cliquez sur le nœud source", map.Find(".omni-mindmap__banner").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CanvasResize_ToNothingOrToTheSameSize_KeepsTheView()
    {
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()));
        var bridge = new MindMapInteropBridge(map.Instance);
        await map.InvokeAsync(() => bridge.Resized(900, 700));
        var view = map.Instance.CurrentView;

        await map.InvokeAsync(() => bridge.Resized(0, 700));
        await map.InvokeAsync(() => bridge.Resized(900, -1));
        await map.InvokeAsync(() => bridge.Resized(900, 700));

        Assert.Equal(view, map.Instance.CurrentView);
    }

    [Fact]
    public void ViewStateTakenAway_KeepsTheViewItGave()
    {
        var given = new OmniMindMapViewState(12, 34, 1.5);
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.ViewState, given));

        map.Render(parameters => parameters.Add(component => component.ViewState, (OmniMindMapViewState?)null));

        Assert.Equal(given, map.Instance.CurrentView);
    }

    [Fact]
    public void CanvasScript_MeasuringNothing_AndFocusingTheLabel_Succeed()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.Answers["measure"] = Array.Empty<MindMapMeasurement>();
        Services.AddSingleton<IJSRuntime>(runtime);
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.PanelContent, builder =>
            {
                builder.OpenComponent<OmniMindMapNodeProperties>(0);
                builder.CloseComponent();
            }));

        Press(map, "Home");
        Press(map, "F2");

        Assert.Contains("measure", runtime.Module.Calls);
        Assert.Contains("focusById", runtime.Module.Calls);
        Assert.NotEmpty(map.FindAll(".omni-mindmap__node"));
    }

    [Fact]
    public void ImportFailing_LeavesTheMapDrawnWithoutItsScript()
    {
        Services.AddSingleton<IJSRuntime>(new ManualJSRuntime { ImportFailure = new JSDisconnectedException("perdu") });
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()));

        map.Render(parameters => parameters.Add(component => component.Label, "Carte"));

        Assert.NotEmpty(map.FindAll(".omni-mindmap__node"));
    }

    [Fact]
    public async Task CanvasScript_AnsweringNothingOrLost_IsQuiet_AndFocusAfterDisposalDoesNothing()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.CallFailures["focusById"] = new JSDisconnectedException("perdu");
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var map = Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, MindMapComponentTests.Sample())
            .Add(component => component.PanelContent, builder =>
            {
                builder.OpenComponent<OmniMindMapNodeProperties>(0);
                builder.CloseComponent();
            }));
        Press(map, "Home");
        Press(map, "F2");
        Assert.Contains("focusById", runtime.Module.Calls);

        await map.Instance.DisposeAsync();
        await map.InvokeAsync(() => map.Instance.FocusElementAsync("x"));
        map.Render();

        Assert.Single(runtime.Module.Calls, call => call == "focusById");
    }

    [Fact]
    public void CanvasScriptLostDuringItsFirstRender_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);

        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()));

        Assert.Equal(["attach"], runtime.Module.Calls);
        Assert.NotEmpty(map.FindAll(".omni-mindmap__node"));
    }

    [Fact]
    public async Task MapGoneBeforeItsScript_ReleasesNoBridge()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var map = Render<OmniMindMap>(parameters => parameters.Add(component => component.Document, MindMapComponentTests.Sample()));

        await map.Instance.DisposeAsync();

        Assert.Empty(runtime.Module.Calls);
        runtime.PendingImport.SetResult(runtime.Module);
    }
}
