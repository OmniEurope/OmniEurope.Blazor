using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniHtmlEditor at the edges its other suites leave: locked both ways, a value the host empties under the
/// history, inputs and choices the browser sends without a value, a file name of blanks, a context menu
/// asked for in the source face or closed by a face switch, and what a command learns of the editor.
/// </summary>
public sealed class HtmlEditorEdgeTests : OmniBunitContext
{
    private const string Start = "<p>Bonjour</p>";
    private readonly string _bound = Start;
    private BunitJSModuleInterop Modules()
    {
        var visual = JSInterop.SetupModule(OmniModules.HtmlEditor);
        visual.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(OmniModules.Interop).Mode = JSRuntimeMode.Loose;
        return visual;
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>>? extra = null, List<string>? changes = null) =>
        Render<OmniHtmlEditor>(parameters =>
        {
            parameters.Add(editor => editor.Value, Start).Add(editor => editor.ValueExpression, () => _bound);
            if (changes is not null)
            {
                parameters.Add(editor => editor.ValueChanged, html => changes.Add(html));
            }

            extra?.Invoke(parameters);
        });

    [Fact]
    public void DisabledAndReadOnly_IsMarkedDisabledOnly()
    {
        Modules();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Disabled, true).Add(editor => editor.ReadOnly, true));

        var classes = editor.Find(".omni-html-editor").ClassList;
        Assert.Contains("omni-html-editor--disabled", classes);
        Assert.DoesNotContain("omni-html-editor--readonly", classes);
    }

    [Fact]
    public async Task ValueEmptiedByTheHost_GoesIntoTheHistoryAsEmpty()
    {
        Modules();
        var changes = new List<string>();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source), changes);
        editor.Find("textarea").Input("<p>un</p>");

        editor.Render(parameters => parameters.Add(editor => editor.Value, null));
        Assert.Equal(string.Empty, editor.Find("textarea").TextContent);
        await editor.InvokeAsync(editor.Instance.UndoAsync);
        editor.Render(parameters => parameters.Add(editor => editor.Value, null));
        await editor.InvokeAsync(editor.Instance.RedoAsync);

        Assert.Equal(["<p>un</p>", Start, string.Empty], changes);
    }

    [Fact]
    public async Task NullValue_ExportsAsEmpty()
    {
        Modules();
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(editor => editor.Value, null)
            .Add(editor => editor.ValueExpression, () => _bound)
            .Add(editor => editor.Mode, OmniHtmlEditorMode.Source));

        Assert.Equal(string.Empty, await editor.InvokeAsync(editor.Instance.ExportTextAsync));
    }

    [Fact]
    public void InputAndChoiceWithoutAValue_AreReadAsEmpty()
    {
        Modules();
        var changes = new List<string>();
        var source = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source), changes);
        source.Find("textarea").Input((object?)null);
        Assert.Equal([string.Empty], changes);

        var visual = RenderEditor(parameters => parameters.Add(editor => editor.Commands, OmniHtmlEditorCommands.Document));
        visual.Find(".omni-html-editor__select").Change((object?)null);
        Assert.NotEmpty(visual.FindAll(".omni-html-editor__select"));
    }

    [Fact]
    public void FileNameOfBlanks_DownloadsTheDefaultName()
    {
        Modules();
        var download = JSInterop.SetupModule(OmniModules.DocumentEditor);
        download.SetupVoid("download", _ => true).SetVoidResult();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ShowStatusBar, true).Add(editor => editor.FileName, "  "));

        editor.Find("[data-export=text]").Click();

        Assert.Equal("document.txt", Assert.Single(download.Invocations["download"]).Arguments[0]);
    }

    [Fact]
    public async Task ContextMenu_AskedInTheSourceFace_OpensNothing_AndASwitchClosesAnOpenOne()
    {
        Modules();
        var extension = new MenuExtension();
        var source = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source).Add(editor => editor.Extensions, [extension]));
        await source.InvokeAsync(() => source.Instance.HandleContextMenuAsync(10, 10, null));
        Assert.Empty(source.FindAll("[role=menu]"));

        var visual = RenderEditor(parameters => parameters.Add(editor => editor.Extensions, [extension]));
        await visual.InvokeAsync(() => visual.Instance.HandleContextMenuAsync(10, 10, null));
        Assert.NotEmpty(visual.FindAll("[role=menu]"));
        await visual.InvokeAsync(() => visual.Instance.RunBuiltInAsync(OmniHtmlEditorAction.ToggleSource, null));
        visual.Render();

        Assert.Empty(visual.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task CommandContext_TellsTheFace_AndTheSurfaceOfTheVisualOne()
    {
        Modules();
        ElementReference? seen = null;
        OmniHtmlEditorMode? mode = null;
        var command = OmniHtmlEditorCommand.Create("probe", "Sonder", context =>
        {
            seen = context.SurfaceElement;
            mode = context.Mode;
            return Task.CompletedTask;
        });
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Commands, [command]));

        await editor.Find("[data-command=probe]").ClickAsync(new());

        Assert.NotNull(seen);
        Assert.Equal(OmniHtmlEditorMode.Visual, mode);
    }

    private sealed class MenuExtension : OmniHtmlEditorExtension
    {
        public override IReadOnlyList<OmniHtmlEditorCommand> ContextMenu { get; } =
            [OmniHtmlEditorCommand.Create("note", "Annoter", _ => Task.CompletedTask)];
    }
}
