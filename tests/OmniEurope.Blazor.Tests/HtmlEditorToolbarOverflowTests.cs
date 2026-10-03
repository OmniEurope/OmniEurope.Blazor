using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The toolbar of the HTML editor (Astraia recette R-042 and R-043): the package tooltip on every
/// control with a command's description, the "more" menu that holds the commands placed there and those
/// a row limit moves out of the bar, and the names beside the icons.
/// </summary>
public sealed class HtmlEditorToolbarOverflowTests : OmniBunitContext
{
    private static readonly OmniHtmlEditorCommand Article = OmniHtmlEditorCommand.Create("article", "Article", _ => Task.CompletedTask, OmniIconName.FileDoc)
        with { Description = "Insère un article numéroté" };

    private IRenderedComponent<OmniHtmlEditor> Editor(IReadOnlyList<OmniHtmlEditorCommand> commands, int? rows = null, bool labels = false)
    {
        var value = "<p>Texte</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Commands, commands)
            .Add(component => component.ToolbarRows, rows)
            .Add(component => component.ToolbarLabels, labels));
    }

    [Fact]
    public void EveryControl_CarriesThePackageTooltip_WithTheDescriptionOnItsSecondLine()
    {
        var editor = Editor([OmniHtmlEditorCommands.Bold, Article, OmniHtmlEditorCommands.ChangeCase]);

        var article = editor.Find("button[data-command=article]");
        Assert.Equal("Article\nInsère un article numéroté", article.GetAttribute("data-omni-tip"));
        Assert.Equal("Insère un article numéroté", article.GetAttribute("aria-description"));
        Assert.Equal("Article", article.GetAttribute("aria-label"));
        var bold = editor.Find("button[data-command=bold]");
        Assert.Equal("Gras", bold.GetAttribute("data-omni-tip"));
        Assert.False(bold.HasAttribute("aria-description"));
        // No native title left: the browser's slow box would show beside the package one.
        Assert.Empty(editor.FindAll(".omni-html-editor__toolbar [data-command][title]"));
        Assert.NotNull(editor.Find("select[data-command=change-case]").GetAttribute("data-omni-tip"));
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "installPackageTooltips");
    }

    [Fact]
    public void ACommandPlacedInTheMenu_LeavesTheBar_AndRunsFromTheMenu()
    {
        var ran = 0;
        var placed = OmniHtmlEditorCommand.Create("placed", "Placé", _ => { ran++; return Task.CompletedTask; }) with { Overflow = true };
        var editor = Editor([OmniHtmlEditorCommands.Bold, placed]);

        Assert.Empty(editor.FindAll(".omni-html-editor__group [data-command=placed]"));
        Assert.Equal("1", editor.Find(".omni-html-editor__more").GetAttribute("data-omni-fixed"));
        editor.Find(".omni-html-editor__more .omni-overflow-menu__trigger").Click();
        editor.Find("[role=menuitem][data-command=placed]").Click();

        editor.WaitForAssertion(() => Assert.Equal(1, ran));
    }

    [Fact]
    public async Task ARowLimit_AsksTheScriptToFitTheBar_AndListsWhatItMovedInTheMenu()
    {
        var editor = Editor(OmniHtmlEditorCommands.Default, rows: 1);

        var fit = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "fitToolbar");
        Assert.Equal(1, fit.Arguments[1]);
        Assert.Equal("0", editor.Find(".omni-html-editor__more").GetAttribute("data-omni-fixed"));

        // The script reports the buttons it hid: the menu lists them, the bar keeps rendering them.
        var bridge = (DotNetObjectReference<HtmlEditorToolbarFit>)fit.Arguments[2]!;
        await editor.InvokeAsync(() => bridge.Value.OnToolbarOverflow(["undo", "redo"]));
        editor.Find(".omni-html-editor__more .omni-overflow-menu__trigger").Click();

        Assert.Equal(["undo", "redo"], editor.FindAll("[role=menuitem]").Select(item => item.GetAttribute("data-command")));
        Assert.NotNull(editor.Find(".omni-html-editor__group button[data-command=undo]"));
    }

    [Fact]
    public void WithoutALimit_NorACommandInTheMenu_TheBarHasNoMoreMenu_AndNamesShowOnlyWhenAsked()
    {
        var plain = Editor([OmniHtmlEditorCommands.Bold]);
        Assert.Empty(plain.FindAll(".omni-html-editor__more"));
        Assert.Empty(plain.FindAll(".omni-html-editor__tool-label"));
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "fitToolbar");

        var labelled = Editor([OmniHtmlEditorCommands.Bold], labels: true);
        Assert.Equal("Gras", labelled.Find(".omni-html-editor__tool-label").TextContent);
        Assert.Contains("omni-html-editor__toolbar--labels", labelled.Find(".omni-html-editor__toolbar").ClassList);
    }
}
