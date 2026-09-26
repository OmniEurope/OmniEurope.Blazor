using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The clipboard, paragraph and table actions of <see cref="OmniHtmlEditor"/>: when .NET enables
/// them, what it asks the surface to do, and what it keeps of the result. The table editing itself
/// runs in <c>omni-html-editor.js</c> and is exercised in a browser.
/// </summary>
public sealed class HtmlEditorTableAndClipboardTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";

    private static readonly string[] TableCommandNames =
        ["add-row-above", "add-row-below", "delete-row", "add-column-before", "add-column-after", "delete-column", "merge-cell-right", "merge-cell-down", "split-cell"];

    [Fact]
    public async Task TableCommands_AreEnabledOnlyWithTheCaretInACell_AndRunInTheSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", invocation => Equals(invocation.Arguments[1], "addrowbelow"))
            .SetResult("<table><tbody><tr><td>A</td></tr><tr><td onclick=\"x()\"><br></td></tr></tbody></table>");
        var value = "<table><tbody><tr><td>A</td></tr></tbody></table>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, OmniHtmlEditorCommands.Table));

        Assert.All(TableCommandNames, name => Assert.True(editor.Find($"button[data-command={name}]").HasAttribute("disabled")));
        Assert.False(editor.Find("button[data-command=insert-table]").HasAttribute("disabled"));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnVisualState("intable|p|left|normal"));

        Assert.All(TableCommandNames, name => Assert.False(editor.Find($"button[data-command={name}]").HasAttribute("disabled")));
        Assert.Null(editor.Find("button[data-command=delete-row]").GetAttribute("aria-pressed"));

        editor.Find("button[data-command=add-row-below]").Click();

        Assert.Equal("addrowbelow", Assert.Single(module.Invocations["exec"]).Arguments[1]);
        Assert.Equal("<table><tbody><tr><td>A</td></tr><tr><td><br></td></tr></tbody></table>", value);
    }

    [Fact]
    public void ClipboardAndParagraphCommands_RunInTheVisualFace_AndAreDisabledInTheSourceFace()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", _ => true).SetResult(null);
        var value = "<p>A</p>";
        OmniHtmlEditorCommand[] commands = [.. OmniHtmlEditorCommands.Clipboard, OmniHtmlEditorCommands.InsertParagraph, OmniHtmlEditorCommands.ToggleSource];
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, commands));

        foreach (var name in new[] { "cut", "copy", "paste", "insert-paragraph" })
        {
            editor.Find($"button[data-command={name}]").Click();
        }

        Assert.Equal(["cut", "copy", "paste", "insertparagraph"], module.Invocations["exec"].Select(invocation => (string)invocation.Arguments[1]!));
        Assert.Equal("Coller", editor.Find("button[data-command=paste]").GetAttribute("aria-label"));
        Assert.Equal("Nouveau paragraphe", editor.Find("button[data-command=insert-paragraph]").GetAttribute("aria-label"));

        editor.Render(parameters => parameters.Add(component => component.Mode, OmniHtmlEditorMode.Source));

        foreach (var name in new[] { "cut", "copy", "paste", "insert-paragraph" })
        {
            Assert.True(editor.Find($"button[data-command={name}]").HasAttribute("disabled"));
        }
    }

    [Fact]
    public void SetCellSpan_PassesItsArgumentToTheSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", _ => true).SetResult("<table><tbody><tr><td colspan=\"2\">A B</td></tr></tbody></table>");
        var value = "<table><tbody><tr><td>A</td><td>B</td></tr></tbody></table>";
        var merge = OmniHtmlEditorCommand.Create("span", "Étendre", context => context.ExecuteAsync(OmniHtmlEditorAction.SetCellSpan, "1x2"));
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [merge]));

        editor.Find("button[data-command=span]").Click();

        var exec = module.Invocations["exec"].Single(invocation => Equals(invocation.Arguments[1], "setcellspan"));
        Assert.Equal("1x2", exec.Arguments[2]);
        Assert.Equal("<table><tbody><tr><td colspan=\"2\">A B</td></tr></tbody></table>", value);
    }

    [Fact]
    public void TheTableToolbar_ListsTheTableActionsInOrder()
    {
        Assert.Equal(
            [
                OmniHtmlEditorAction.InsertTable, OmniHtmlEditorAction.AddRowAbove, OmniHtmlEditorAction.AddRowBelow,
                OmniHtmlEditorAction.DeleteRow, OmniHtmlEditorAction.AddColumnBefore, OmniHtmlEditorAction.AddColumnAfter,
                OmniHtmlEditorAction.DeleteColumn, OmniHtmlEditorAction.MergeCellRight, OmniHtmlEditorAction.MergeCellDown,
                OmniHtmlEditorAction.SplitCell
            ],
            OmniHtmlEditorCommands.Table.Where(command => command.Action != OmniHtmlEditorAction.Separator).Select(command => command.Action));
        Assert.Equal([OmniHtmlEditorAction.Cut, OmniHtmlEditorAction.Copy, OmniHtmlEditorAction.Paste], OmniHtmlEditorCommands.Clipboard.Select(command => command.Action));
    }
}
