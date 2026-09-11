using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniMindMapToolbar"/> and <see cref="OmniMindMapNodeProperties"/> placed in a map, as a
/// host places them.
/// </summary>
public sealed class MindMapToolbarAndPropertiesTests : OmniBunitContext
{
    [Fact]
    public void Toolbar_OffersTheActionsOfTheOriginalPageInItsOrder()
    {
        var map = RenderMap(MindMapComponentTests.Sample());

        var toolbar = map.Find(".omni-mindmap-toolbar");
        Assert.Equal("group", toolbar.GetAttribute("role"));
        Assert.Equal("Actions de la carte mentale", toolbar.GetAttribute("aria-label"));
        Assert.Equal(map.Find("svg").Id, toolbar.GetAttribute("aria-controls"));
        Assert.Equal(
            ["Nœud", "Supprimer", "Dupliquer", "Lien", "Centrer", "Réorganiser", "Ajuster"],
            toolbar.QuerySelectorAll("button").Select(button => button.TextContent.Trim()));
        Assert.All(toolbar.QuerySelectorAll("button"), button => Assert.Equal("button", button.GetAttribute("type")));
    }

    [Fact]
    public void Toolbar_DisablesWhatNeedsASelectionUntilThereIsOne()
    {
        var map = RenderMap(MindMapComponentTests.Sample());

        Assert.True(Action(map, "delete").HasAttribute("disabled"));
        Assert.True(Action(map, "duplicate").HasAttribute("disabled"));
        Assert.True(Action(map, "center").HasAttribute("disabled"));

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.False(Action(map, "delete").HasAttribute("disabled"));
        Assert.False(Action(map, "duplicate").HasAttribute("disabled"));
        Assert.False(Action(map, "center").HasAttribute("disabled"));
    }

    [Fact]
    public void ToolbarActions_ChangeTheMap()
    {
        OmniMindMapDocument current = MindMapComponentTests.Sample();
        var map = RenderMap(current, changed: value => current = value);

        Action(map, "add").Click();
        Assert.Contains(current.Nodes, node => node is { Id: "node_1", Label: "Nouveau" });

        Action(map, "duplicate").Click();
        Assert.Contains(current.Nodes, node => node.Id == "node_2");

        Action(map, "delete").Click();
        Assert.DoesNotContain(current.Nodes, node => node.Id == "node_2");

        Action(map, "link").Click();
        Assert.Equal("true", Action(map, "link").GetAttribute("aria-pressed"));
        Assert.True(map.Instance.IsLinking);

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal("false", Action(map, "link").GetAttribute("aria-pressed"));

        Action(map, "layout").Click();
        Assert.Equal("Carte réorganisée.", map.Instance.Announcement);
    }

    [Fact]
    public void ToolbarOfAReadOnlyMap_KeepsOnlyCentringAndFitting()
    {
        var map = RenderMap(MindMapComponentTests.Sample(), readOnly: true);

        Assert.Equal(["Centrer", "Ajuster"], map.FindAll(".omni-mindmap-toolbar button").Select(button => button.TextContent.Trim()));
    }

    [Fact]
    public void ToolbarLabels_FollowTheMapContextLabels()
    {
        var map = RenderMap(MindMapComponentTests.Sample(), labels: new OmniMindMapLabels { AutoLayout = "Ranger", FitView = "Tout voir" });

        Assert.Equal("Ranger", Action(map, "layout").TextContent.Trim());
        Assert.Equal("Tout voir", Action(map, "fit").TextContent.Trim());
    }

    [Fact]
    public void ToolbarOutsideAMap_FailsWithAnExplicitMessage()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniMindMapToolbar>());

        Assert.Contains("ToolbarContent", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Properties_AppearOnlyForASelectedNode()
    {
        var map = RenderMap(MindMapComponentTests.Sample());
        Assert.Empty(map.FindAll(".omni-mindmap-properties"));

        await map.InvokeAsync(() => new MindMapInteropBridge(map.Instance).NodePressed("root"));

        var panel = map.Find(".omni-mindmap-properties");
        Assert.Equal("Propriétés du nœud", panel.QuerySelector(".omni-mindmap-properties__title")!.TextContent);
        Assert.Equal("Centre", map.Find("#props-label").GetAttribute("value"));
        Assert.Equal("20", map.Find("#props-font-size").GetAttribute("value"));
        Assert.Equal("8", map.Find("#props-font-size").GetAttribute("min"));
        Assert.Equal("48", map.Find("#props-font-size").GetAttribute("max"));
        Assert.Equal("500", map.Find("#props-width").GetAttribute("max"));
        Assert.Equal("300", map.Find("#props-height").GetAttribute("max"));
        Assert.Equal(OmniMindMapGroups.Palette.Count, panel.QuerySelectorAll(".omni-mindmap__swatch").Length);
        Assert.Equal("true", panel.QuerySelector(".omni-mindmap__swatch--root")!.GetAttribute("aria-pressed"));
        Assert.Equal("true", panel.QuerySelectorAll(".omni-toggle-button")[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", panel.QuerySelectorAll(".omni-toggle-button")[1].GetAttribute("aria-pressed"));
        Assert.Equal("0 = taille automatique", panel.QuerySelector(".omni-mindmap-properties__hint")!.TextContent);
        Assert.DoesNotMatch(MindMapComponentTests.InlineStyle, map.Markup);

        await map.InvokeAsync(() => new MindMapInteropBridge(map.Instance).BackgroundPressed());
        Assert.Empty(map.FindAll(".omni-mindmap-properties"));
    }

    [Fact]
    public async Task PropertyEdits_AreMapChanges()
    {
        OmniMindMapDocument current = MindMapComponentTests.Sample();
        var map = RenderMap(current, changed: value => current = value);
        await map.InvokeAsync(() => new MindMapInteropBridge(map.Instance).NodePressed("left"));
        OmniMindMapNode Left() => current.Nodes.Single(node => node.Id == "left");

        map.Find("#props-label").Input("Gauche renommée");
        Assert.Equal("Gauche renommée", Left().Label);
        Assert.Equal("Gauche renommée", MindMapComponentTests.Node(map, "left").QuerySelector("text")!.TextContent);

        map.Find("#props-color").Change(OmniMindMapGroups.Palette.ToList().IndexOf(OmniMindMapGroups.Indigo).ToString(CultureInfo.InvariantCulture));
        Assert.Equal(OmniMindMapGroups.Indigo, Left().Group);

        map.Find(".omni-mindmap-properties .omni-mindmap__swatch--teal").Click();
        Assert.Equal(OmniMindMapGroups.Teal, Left().Group);

        map.Find("#props-font-size").Change("99");
        Assert.Equal(48, Left().FontSize);

        map.FindAll(".omni-mindmap-properties .omni-toggle-button")[0].Click();
        map.FindAll(".omni-mindmap-properties .omni-toggle-button")[1].Click();
        Assert.True(Left().Bold);
        Assert.True(Left().Italic);

        map.Find("#props-width").Change("120");
        map.Find("#props-height").Change("-4");
        Assert.Equal((120, 0), (Left().Width, Left().Height));
        Assert.Equal("120", MindMapComponentTests.Node(map, "left").QuerySelector("rect.omni-mindmap__node-shape")!.GetAttribute("width"));

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "z", CtrlKey = true });
        Assert.Equal(0, Left().Width);
    }

    [Fact]
    public async Task PropertiesOfAReadOnlyMap_AreShownButDisabled()
    {
        var map = RenderMap(MindMapComponentTests.Sample(), readOnly: true);
        await map.InvokeAsync(() => new MindMapInteropBridge(map.Instance).NodePressed("left"));

        Assert.True(map.Find("#props-label").HasAttribute("disabled"));
        Assert.True(map.Find("#props-color").HasAttribute("disabled"));
        Assert.All(map.FindAll(".omni-mindmap-properties button"), button => Assert.True(button.HasAttribute("disabled")));
    }

    [Fact]
    public async Task RenameRequest_WithoutAHostHandler_FocusesThePanelTextField()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-mindmap.js");
        module.SetupVoid("focusById", "props-label");
        var map = RenderMap(MindMapComponentTests.Sample());
        await map.InvokeAsync(() => new MindMapInteropBridge(map.Instance).NodePressed("left"));

        map.Find("svg").KeyDown(new KeyboardEventArgs { Key = "F2" });

        Assert.Single(module.Invocations["focusById"]);
    }

    private IRenderedComponent<OmniMindMap> RenderMap(
        OmniMindMapDocument document,
        Action<OmniMindMapDocument>? changed = null,
        bool readOnly = false,
        OmniMindMapLabels? labels = null) =>
        Render<OmniMindMap>(parameters => parameters
            .Add(component => component.Document, document)
            .Add(component => component.ReadOnly, readOnly)
            .Add(component => component.ContextLabels, labels)
            .Add(component => component.DocumentChanged, value => changed?.Invoke(value))
            .Add(component => component.ToolbarContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniMindMapToolbar>(0);
                builder.CloseComponent();
            }))
            .Add(component => component.PanelContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniMindMapNodeProperties>(0);
                builder.AddComponentParameter(1, nameof(OmniMindMapNodeProperties.Id), "props");
                builder.CloseComponent();
            })));

    private static AngleSharp.Dom.IElement Action(IRenderedComponent<OmniMindMap> map, string action) =>
        map.Find($".omni-mindmap-toolbar [data-omni-mindmap-action='{action}']");
}
