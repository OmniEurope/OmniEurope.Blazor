using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniHtmlEditorCommandContext.CommitDomAsync"/>: a host that edits the surface with its
/// own script hands the result back through the sanitiser, the history and <c>ValueChanged</c>.
/// </summary>
public sealed class HtmlEditorCommitDomTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";

    private static readonly OmniHtmlSanitizerPolicy NotePolicy = new() { AdditionalTags = ["aside"], AllowDataAttributes = true };

    [Fact]
    public void CommitDom_ReadsTheSurface_SanitisesWithThePolicy_CommitsAndRedrawsWhatWasRemoved()
    {
        var module = JSInterop.SetupModule(ModulePath);
        // The first read is the capture before the command runs; the one set up inside it follows the host script.
        module.Setup<string?>("read", _ => true).SetResult(null);
        var value = "<p>A</p>";
        ElementReference? surface = null;
        var command = OmniHtmlEditorCommand.Create("note", "Note", async context =>
        {
            surface = context.SurfaceElement;
            module.Setup<string?>("read", _ => true).SetResult("<p>A</p><aside data-marker=\"1\" onclick=\"x()\">Note</aside>");
            await context.CommitDomAsync();
        });
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.SanitizerPolicy, NotePolicy)
            .Add(component => component.Commands, [command, OmniHtmlEditorCommands.Undo]));

        editor.Find("button[data-command=note]").Click();

        Assert.NotNull(surface);
        Assert.Equal("<p>A</p><aside data-marker=\"1\">Note</aside>", value);
        editor.WaitForAssertion(() => Assert.Equal("<p>A</p><aside data-marker=\"1\">Note</aside>", Assert.Single(module.Invocations["setHtml"]).Arguments[1]));

        editor.Find("button[data-command=undo]").Click();

        Assert.Equal("<p>A</p>", value);
    }

    [Fact]
    public async Task EditorCommitDom_OutsideACommand_SanitisesCommitsAndRedraws()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult("<p>A</p><aside data-marker=\"1\" onclick=\"x()\">Note</aside>");
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.SanitizerPolicy, NotePolicy)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.Undo]));
        Assert.True(editor.Find("button[data-command=undo]").HasAttribute("disabled"));

        // Called by host code, not from an event of the editor: the editor dispatches it itself.
        await editor.Instance.CommitDomAsync();

        Assert.Equal("<p>A</p><aside data-marker=\"1\">Note</aside>", value);
        editor.WaitForAssertion(() => Assert.Equal("<p>A</p><aside data-marker=\"1\">Note</aside>", Assert.Single(module.Invocations["setHtml"]).Arguments[1]));
        Assert.False(editor.Find("button[data-command=undo]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task EditorCommitDom_InTheSourceFace_DoesNothing()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";
        var changed = false;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, _ => changed = true)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source));

        await editor.Instance.CommitDomAsync();

        Assert.False(changed);
        Assert.Empty(module.Invocations["read"]);
    }

    [Fact]
    public void CommitDom_OfACleanSurface_DoesNotRedrawIt()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        var value = "<p>A</p>";
        var command = OmniHtmlEditorCommand.Create("host", "Hôte", async context =>
        {
            module.Setup<string?>("read", _ => true).SetResult("<p>A</p><p>B</p>");
            await context.CommitDomAsync();
        });
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [command]));

        editor.Find("button[data-command=host]").Click();

        Assert.Equal("<p>A</p><p>B</p>", value);
        Assert.Empty(module.Invocations["setHtml"]);
    }

    [Fact]
    public void InTheSourceFace_ThereIsNoSurface_AndCommitDomDoesNothing()
    {
        var value = "<p>A</p>";
        ElementReference? surface = default(ElementReference);
        var changed = false;
        var command = OmniHtmlEditorCommand.Create("host", "Hôte", async context =>
        {
            surface = context.SurfaceElement;
            await context.CommitDomAsync();
        });
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, _ => changed = true)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Commands, [command]));

        editor.Find("button[data-command=host]").Click();

        Assert.Null(surface);
        Assert.False(changed);
    }
}
