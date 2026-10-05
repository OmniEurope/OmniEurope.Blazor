using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The actions OmniHtmlEditor runs itself (history, faces, panels, the link field), each refused while
/// the editor is locked, and its exports and disposal at their edges.
/// </summary>
public sealed class HtmlEditorActionsTests : OmniBunitContext
{
    private const string Start = "<p>Bonjour</p>";
    private readonly string _bound = Start;
    private readonly BunitJSModuleInterop _visual;
    private readonly BunitJSModuleInterop _interop;

    public HtmlEditorActionsTests()
    {
        _visual = JSInterop.SetupModule(OmniModules.HtmlEditor);
        _visual.Mode = JSRuntimeMode.Loose;
        _interop = JSInterop.SetupModule(OmniModules.Interop);
        _interop.SetupVoid("restoreTextSelection", _ => true).SetVoidResult();
        _interop.Setup<JsonElement>("wrapTextSelection", _ => true)
            .SetResult(JsonDocument.Parse("""{"value":"<p>x</p>","selectionStart":0,"selectionEnd":0}""").RootElement.Clone());
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

    // Called the way the toolbar calls it, then rendered as an event handler would be.
    private static async Task Run(IRenderedComponent<OmniHtmlEditor> editor, OmniHtmlEditorAction action, string? argument = null)
    {
        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(action, argument));
        editor.Render();
    }

    [Theory]
    [InlineData(OmniHtmlEditorAction.Bold)]
    [InlineData(OmniHtmlEditorAction.Undo)]
    [InlineData(OmniHtmlEditorAction.ToggleSource)]
    public async Task LockedEditor_RunsNoAction(OmniHtmlEditorAction action)
    {
        var modes = new List<OmniHtmlEditorMode>();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ReadOnly, true).Add(editor => editor.ModeChanged, mode => modes.Add(mode)));

        await Run(editor, action);

        Assert.Empty(_visual.Invocations["exec"]);
        Assert.Empty(modes);
    }

    [Theory]
    [InlineData(OmniHtmlEditorAction.Custom, null)]
    [InlineData(OmniHtmlEditorAction.Separator, null)]
    [InlineData(OmniHtmlEditorAction.ChangeCase, "shout")]
    [InlineData(OmniHtmlEditorAction.ChangeCase, null)]
    public async Task ActionWithoutWork_DoesNothing(OmniHtmlEditorAction action, string? argument)
    {
        var editor = RenderEditor();

        await Run(editor, action, argument);

        Assert.Empty(_visual.Invocations["exec"]);
    }

    [Theory]
    [InlineData("upper")]
    [InlineData("lower")]
    [InlineData("title")]
    public async Task ChangeCase_WithAKnownCase_RunsInTheSurface(string argument)
    {
        var editor = RenderEditor();

        await Run(editor, OmniHtmlEditorAction.ChangeCase, argument);

        Assert.Equal(argument, Assert.Single(_visual.Invocations["exec"]).Arguments[2]);
    }

    [Fact]
    public async Task ToggleSource_SwitchesEachWay_AndAParentCatchingUpChangesNothing()
    {
        var modes = new List<OmniHtmlEditorMode>();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ModeChanged, mode => modes.Add(mode)));

        await Run(editor, OmniHtmlEditorAction.ToggleSource);
        Assert.Contains("omni-html-editor--source", editor.Find(".omni-html-editor").ClassList);

        // The parent passes the face it was told of: already shown, nothing switches back.
        editor.Render(parameters => parameters.Add(component => component.Mode, OmniHtmlEditorMode.Source));
        Assert.Contains("omni-html-editor--source", editor.Find(".omni-html-editor").ClassList);

        await Run(editor, OmniHtmlEditorAction.ToggleSource);
        Assert.Equal([OmniHtmlEditorMode.Source, OmniHtmlEditorMode.Visual], modes);
        Assert.Contains("omni-html-editor--visual", editor.Find(".omni-html-editor").ClassList);
    }

    [Fact]
    public async Task ShowBlocks_OutlinesTheBlocks_OnlyInTheVisualFace()
    {
        var editor = RenderEditor();

        await Run(editor, OmniHtmlEditorAction.ShowBlocks);
        Assert.Contains("omni-html-editor--show-blocks", editor.Find(".omni-html-editor").ClassList);

        await Run(editor, OmniHtmlEditorAction.ToggleSource);
        Assert.DoesNotContain("omni-html-editor--show-blocks", editor.Find(".omni-html-editor").ClassList);
        await Run(editor, OmniHtmlEditorAction.ToggleSource);
        await Run(editor, OmniHtmlEditorAction.ShowBlocks);
        Assert.DoesNotContain("omni-html-editor--show-blocks", editor.Find(".omni-html-editor").ClassList);
    }

    [Fact]
    public async Task Panels_OpenForTheCharactersAndTheTableImport()
    {
        var editor = RenderEditor();

        await Run(editor, OmniHtmlEditorAction.InsertSpecialCharacter, string.Empty);
        Assert.NotEmpty(editor.FindAll(".omni-html-editor__characters"));

        await Run(editor, OmniHtmlEditorAction.ImportTable);
        Assert.Empty(editor.FindAll(".omni-html-editor__characters"));
    }

    [Fact]
    public async Task CharacterInTheSourceFace_IsWrittenEncodedAtTheSelection()
    {
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source));

        await Run(editor, OmniHtmlEditorAction.InsertSpecialCharacter, "<");

        Assert.Equal("&lt;", Assert.Single(_interop.Invocations["wrapTextSelection"]).Arguments[1]);
    }

    [Fact]
    public async Task InsertHtml_IsRefusedLockedOrEmpty_AndWrittenAtTheSelectionInTheSourceFace()
    {
        var locked = RenderEditor(parameters => parameters.Add(editor => editor.Disabled, true));
        await locked.InvokeAsync(() => locked.Instance.InsertHtmlAsync("<b>x</b>"));
        var empty = RenderEditor();
        await empty.InvokeAsync(() => empty.Instance.InsertHtmlAsync(string.Empty));
        Assert.Empty(_visual.Invocations["exec"]);

        var source = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source));
        await source.InvokeAsync(() => source.Instance.InsertHtmlAsync("<strong>x</strong><script>y</script>"));

        Assert.Equal("<strong>x</strong>", Assert.Single(_interop.Invocations["wrapTextSelection"]).Arguments[1]);
    }

    [Fact]
    public async Task LockedEditor_RefusesHistoryAndReplacement()
    {
        var changes = new List<string>();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.ReadOnly, true), changes);

        await editor.InvokeAsync(editor.Instance.UndoAsync);
        await editor.InvokeAsync(editor.Instance.RedoAsync);
        await editor.InvokeAsync(() => editor.Instance.ReplaceHtmlAsync("<p>autre</p>"));
        await editor.InvokeAsync(editor.Instance.OpenLinkAsync);
        editor.Render();

        Assert.Empty(changes);
        Assert.Empty(editor.FindAll(".omni-html-editor__link"));
    }

    [Fact]
    public async Task HistoryOfAnEmptyValue_UndoesBackToEmpty()
    {
        var changes = new List<string>();
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(editor => editor.Value, null)
            .Add(editor => editor.ValueExpression, () => _bound)
            .Add(editor => editor.Mode, OmniHtmlEditorMode.Source)
            .Add(editor => editor.ValueChanged, html => changes.Add(html)));

        editor.Find("textarea").Input("<p>un</p>");
        await editor.InvokeAsync(editor.Instance.UndoAsync);
        await editor.InvokeAsync(editor.Instance.RedoAsync);

        Assert.Equal(["<p>un</p>", string.Empty, "<p>un</p>"], changes);
    }

    [Fact]
    public void InputOfALockedSourceFace_IsIgnored()
    {
        var changes = new List<string>();
        var editor = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source).Add(editor => editor.ReadOnly, true), changes);

        editor.Find("textarea").Input("<p>autre</p>");

        Assert.Empty(changes);
    }

    [Fact]
    public async Task LinkField_EmptyClosesIt_EscapeClosesIt_OtherKeysLeaveIt()
    {
        var editor = RenderEditor();
        await editor.InvokeAsync(editor.Instance.OpenLinkAsync);
        editor.Render();

        editor.Find(".omni-html-editor__link-input").KeyDown(new KeyboardEventArgs { Key = "a" });
        Assert.Single(editor.FindAll(".omni-html-editor__link"));
        editor.Find(".omni-html-editor__link-input").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(editor.FindAll(".omni-html-editor__link"));

        await editor.InvokeAsync(editor.Instance.OpenLinkAsync);
        editor.Render();
        editor.Find(".omni-html-editor__link-input").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Empty(editor.FindAll(".omni-html-editor__link"));
        Assert.Empty(_visual.Invocations["exec"]);
    }

    [Fact]
    public async Task Shortcut_OutOfRangeOrOutsideTheVisualFace_RunsNothing()
    {
        var editor = RenderEditor();
        await editor.InvokeAsync(() => editor.Instance.HandleShortcutAsync(-1));
        await editor.InvokeAsync(() => editor.Instance.HandleShortcutAsync(5));

        var source = RenderEditor(parameters => parameters.Add(editor => editor.Mode, OmniHtmlEditorMode.Source));
        await source.InvokeAsync(() => source.Instance.HandleShortcutAsync(0));

        Assert.Empty(_visual.Invocations["exec"]);
    }

    [Fact]
    public async Task HtmlExport_WithoutTitleNorHeading_TakesTheDefaultTitle_AndTheFileItsDefaultName()
    {
        var download = JSInterop.SetupModule(OmniModules.DocumentEditor);
        download.SetupVoid("download", _ => true).SetVoidResult();
        var editor = RenderEditor(parameters => parameters
            .Add(editor => editor.Sheet, true)
            .Add(editor => editor.ShowStatusBar, true)
            .Add(editor => editor.Commands, OmniHtmlEditorCommands.Document));

        var html = await editor.InvokeAsync(editor.Instance.ExportHtmlAsync);
        editor.Find("[data-export=html]").Click();

        Assert.Contains("<title>Document</title>", html, StringComparison.Ordinal);
        Assert.Equal("document.html", Assert.Single(download.Invocations["download"]).Arguments[0]);
    }

    [Fact]
    public async Task Dispose_ReleasesTheDownloadScript_EvenOnALostCircuit_AndARenderAfterIsIgnored()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var editor = RenderEditor(parameters => parameters
            .Add(editor => editor.Sheet, true)
            .Add(editor => editor.ShowStatusBar, true)
            .Add(editor => editor.Commands, OmniHtmlEditorCommands.Document));
        editor.Find("[data-export=text]").Click();
        Assert.Contains("download", runtime.Module.Calls);

        await editor.Instance.DisposeAsync();
        var calls = runtime.Module.Calls.Count;
        editor.Render();

        Assert.Equal(calls, runtime.Module.Calls.Count);
    }

    [Fact]
    public void TextParsing_SanitisesAndNeverFails()
    {
        var editor = Render<ParsingEditor>(parameters => parameters
            .Add(component => component.Value, Start)
            .Add(component => component.ValueExpression, () => _bound));

        Assert.True(editor.Instance.Parse("<p onclick=\"x()\">a</p>", out var result, out var message));
        Assert.Equal("<p>a</p>", result);
        Assert.Null(message);
    }

    /// <summary>Opens the text parsing of the editor, which its own input does not go through.</summary>
    public sealed class ParsingEditor : OmniHtmlEditor
    {
        public bool Parse(string? text, out string result, out string message) => TryParseValueFromString(text, out result, out message);
    }
}
