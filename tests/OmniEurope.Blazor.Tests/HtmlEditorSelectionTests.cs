using System.Text.Json;
using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniHtmlEditor.SelectionChanged"/>: the surface reports the elements around the caret
/// only when the host listens, and the editor hands them over as <see cref="OmniHtmlEditorSelection"/>.
/// </summary>
public sealed class HtmlEditorSelectionTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";

    private const string CaretInANote =
        "{\"collapsed\":true,\"ancestors\":[" +
        "{\"tag\":\"sup\",\"classes\":[\"akn-noteref\"],\"data\":{\"data-eid\":\"ref_1\"}}," +
        "{\"tag\":\"aside\",\"classes\":[\"akn-authorial-note\",\"x\"],\"data\":{\"data-marker\":\"1\",\"data-eid\":\"n_1\"}}]}";

    [Fact]
    public void Parse_ReadsTheAncestorChain_InnermostFirst()
    {
        var selection = OmniHtmlEditorSelection.Parse(CaretInANote);

        Assert.True(selection.IsCollapsed);
        Assert.Equal(["sup", "aside"], selection.Ancestors.Select(node => node.TagName));
        Assert.Equal(["akn-authorial-note", "x"], selection.Ancestors[1].CssClasses);
        Assert.Equal("1", selection.Ancestors[1].DataAttributes["data-marker"]);
        Assert.Equal("n_1", selection.Closest("ASIDE")!.DataAttributes["data-eid"]);
        Assert.Equal("sup", selection.ClosestWithClass("akn-noteref")!.TagName);
        Assert.Null(selection.Closest("td"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"ancestors\":[{\"classes\":[]},42]}")]
    public void Parse_OfSomethingMalformed_IsAnEmptyCaret(string? json)
    {
        var selection = OmniHtmlEditorSelection.Parse(json);

        Assert.True(selection.IsCollapsed);
        Assert.Empty(selection.Ancestors);
    }

    [Fact]
    public void Parse_OfARangeSelection_IsNotCollapsed() =>
        Assert.False(OmniHtmlEditorSelection.Parse("{\"collapsed\":false,\"ancestors\":[]}").IsCollapsed);

    [Fact]
    public async Task SelectionChanged_AsksTheSurfaceToReport_AndHandsTheSelectionToTheHostAndToCommands()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";
        OmniHtmlEditorSelection? reported = null;
        OmniHtmlEditorSelection? seenByCommand = null;
        var inspect = OmniHtmlEditorCommand.Create("inspect", "Inspecter", context =>
        {
            seenByCommand = context.Selection;
            return Task.CompletedTask;
        });
        module.Setup<string?>("read", _ => true).SetResult(null);
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [inspect])
            .Add(component => component.SelectionChanged, selection => reported = selection));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.True(options.GetProperty("selection").GetBoolean());

        await editor.InvokeAsync(() => bridge.OnSelectionChanged(CaretInANote));

        Assert.NotNull(reported);
        Assert.Equal("aside", reported!.Ancestors[1].TagName);

        editor.Find("button[data-command=inspect]").Click();

        Assert.Same(reported, seenByCommand);
    }

    [Fact]
    public void WithoutAListener_TheSurfaceIsNotAskedToReport()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";

        Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.False(options.TryGetProperty("selection", out _));
    }

    [Fact]
    public void AddingAListener_LaterReconfiguresTheSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        editor.Render(parameters => parameters.Add(component => component.SelectionChanged, _ => { }));

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["configure"]).Arguments[1]);
        Assert.True(options.GetProperty("selection").GetBoolean());
    }

    [Fact]
    public async Task Pressed_MakesACommandAToggle_FollowingTheSelection()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.SetupVoid("dispose", _ => true).SetVoidResult();
        module.Setup<string?>("read", _ => true).SetResult(null);
        var value = "<p>A</p>";
        var note = OmniHtmlEditorCommand.Create("note", "Note", _ => Task.CompletedTask) with
        {
            Pressed = selection => selection?.Closest("aside") is not null
        };
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [note, OmniHtmlEditorCommands.Bold, OmniHtmlEditorCommands.ToggleSource]));

        // No SelectionChanged listener: the toggle alone asks the surface to report.
        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.True(options.GetProperty("selection").GetBoolean());
        Assert.Equal("false", editor.Find("button[data-command=note]").GetAttribute("aria-pressed"));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnSelectionChanged(CaretInANote));

        Assert.Equal("true", editor.Find("button[data-command=note]").GetAttribute("aria-pressed"));
        Assert.Equal("false", editor.Find("button[data-command=bold]").GetAttribute("aria-pressed"));

        await editor.Find("button[data-command=toggle-source]").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Equal("false", editor.Find("button[data-command=note]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void WithoutPressed_ACustomCommandHasNoPressedState()
    {
        JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommand.Create("plain", "Simple", _ => Task.CompletedTask)]));

        Assert.Null(editor.Find("button[data-command=plain]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task SourceFace_NeverRaisesSelectionChanged()
    {
        var value = "<p>A</p>";
        var raised = 0;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.SelectionChanged, _ => raised++));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnSelectionChanged(CaretInANote));

        Assert.Equal(0, raised);
    }
}
