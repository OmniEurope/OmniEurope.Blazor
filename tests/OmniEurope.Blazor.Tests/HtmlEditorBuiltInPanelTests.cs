using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The built-in commands that used to live in applications: change of case, special characters, table
/// import and block outlines. The surface script is bUnit's module double; the table file is read here.
/// </summary>
public sealed class HtmlEditorBuiltInPanelTests : OmniBunitContext
{
    private const string ModulePath = Internal.OmniModules.HtmlEditor;

    [Fact]
    public async Task ShowBlocks_TogglesTheOutlineClassAndItsPressedState()
    {
        SetupSurface();
        var editor = RenderEditor([OmniHtmlEditorCommands.ShowBlocks]);
        Assert.Equal("false", editor.Find("[data-command=show-blocks]").GetAttribute("aria-pressed"));

        await editor.Find("[data-command=show-blocks]").ClickAsync(new MouseEventArgs());

        Assert.Contains("omni-html-editor--show-blocks", editor.Find("section").ClassList);
        Assert.Equal("true", editor.Find("[data-command=show-blocks]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task ChangeCase_IsAListOfThreeCases_ThatRunsOnTheSurface()
    {
        var module = SetupSurface();
        module.Setup<string?>("exec", _ => true).SetResult("<p>ABC</p>");
        var editor = RenderEditor([OmniHtmlEditorCommands.ChangeCase]);

        var list = editor.Find("select[data-command=change-case]");
        Assert.Equal(["", "upper", "lower", "title"], list.QuerySelectorAll("option").Select(option => option.GetAttribute("value")));

        await list.ChangeAsync(new ChangeEventArgs { Value = "upper" });

        var exec = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("changecase", exec.Arguments[1]);
        Assert.Equal("upper", exec.Arguments[2]);
    }

    [Fact]
    public void ChangeCaseAndShowBlocks_AreDisabledInTheSourceFace()
    {
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.ChangeCase, OmniHtmlEditorCommands.ShowBlocks]));

        Assert.True(editor.Find("[data-command=change-case]").HasAttribute("disabled"));
        Assert.True(editor.Find("[data-command=show-blocks]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task SpecialCharacter_OpensThePanel_FiltersByName_AndTypesTheChosenCharacter()
    {
        var module = SetupSurface();
        module.Setup<string?>("exec", _ => true).SetResult("<p>A€</p>");
        var editor = RenderEditor([OmniHtmlEditorCommands.InsertSpecialCharacter]);

        await editor.Find("[data-command=insert-special-character]").ClickAsync(new MouseEventArgs());

        Assert.Equal(24, editor.FindAll(".omni-html-editor__character").Count);
        editor.Find(".omni-html-editor__char-search").Input("euro");
        var euro = Assert.Single(editor.FindAll(".omni-html-editor__character"));
        Assert.Equal("20AC", euro.GetAttribute("data-character"));
        Assert.Equal("Euro", euro.GetAttribute("aria-label"));

        await euro.ClickAsync(new MouseEventArgs());

        var exec = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("inserttext", exec.Arguments[1]);
        Assert.Equal("€", exec.Arguments[2]);
        Assert.Empty(editor.FindAll(".omni-html-editor__panel"));
    }

    [Fact]
    public async Task SpecialCharacter_Categories_SwitchTheGrid_AndASearchWithoutMatchSaysSo()
    {
        SetupSurface();
        var editor = RenderEditor([OmniHtmlEditorCommands.InsertSpecialCharacter]);
        await editor.Find("[data-command=insert-special-character]").ClickAsync(new MouseEventArgs());

        await editor.FindAll(".omni-html-editor__categories button")[4].ClickAsync(new MouseEventArgs());

        Assert.Equal(23, editor.FindAll(".omni-html-editor__character").Count);
        Assert.Equal("true", editor.FindAll(".omni-html-editor__categories button")[4].GetAttribute("aria-pressed"));

        editor.Find(".omni-html-editor__char-search").Input("zzz");

        Assert.Empty(editor.FindAll(".omni-html-editor__character"));
        Assert.NotEmpty(editor.FindAll(".omni-html-editor__panel-note[role=status]"));
    }

    [Theory]
    [InlineData("a,b\n1,2", ',', "a|b/1|2")]
    [InlineData("\"x, y\",\"he said \"\"hi\"\"\"\r\n3,4", ',', "x, y|he said \"hi\"/3|4")]
    [InlineData("\"two\nlines\",b\nc", ',', "two\nlines|b/c|")]
    [InlineData("a\tb\n\n1\t2\n", '\t', "a|b/1|2")]
    public void ReadDelimited_HandlesQuotesLineBreaksBlankRowsAndPadding(string text, char delimiter, string expected)
    {
        var rows = HtmlEditorTableFile.ReadDelimited(text, delimiter);

        Assert.Equal(expected, string.Join('/', rows.Select(row => string.Join('|', row))));
    }

    [Fact]
    public void ReadDelimited_KeepsAtMostAHeaderAndAHundredRows_OfFiftyColumns()
    {
        var line = string.Join(',', Enumerable.Range(1, 60));
        var rows = HtmlEditorTableFile.ReadDelimited(string.Join('\n', Enumerable.Repeat(line, 150)), ',');

        Assert.Equal(HtmlEditorTableFile.MaxRows + 1, rows.Count);
        Assert.All(rows, row => Assert.Equal(HtmlEditorTableFile.MaxColumns, row.Count));
    }

    [Fact]
    public void ToHtml_EncodesTheCells_AndPutsTheFirstRowInAHeaderWhenAsked()
    {
        IReadOnlyList<IReadOnlyList<string>> rows = [["<b>Nom</b>", "Âge"], ["Ada & co", "36"]];

        Assert.Equal("<table><thead><tr><th>&lt;b&gt;Nom&lt;/b&gt;</th><th>&#194;ge</th></tr></thead><tbody><tr><td>Ada &amp; co</td><td>36</td></tr></tbody></table>",
            HtmlEditorTableFile.ToHtml(rows, header: true));
        Assert.StartsWith("<table><tbody><tr><td>&lt;b&gt;", HtmlEditorTableFile.ToHtml(rows, header: false), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportTable_ReadsACsv_PreviewsIt_AndInsertsTheTable()
    {
        var module = SetupSurface();
        module.Setup<string?>("exec", _ => true).SetResult("<table></table>");
        var editor = RenderEditor([OmniHtmlEditorCommands.ImportTable]);
        await editor.Find("[data-command=import-table]").ClickAsync(new MouseEventArgs());

        Assert.Equal(".csv,.tsv", editor.Find("input[type=file]").GetAttribute("accept"));
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("Nom,Âge\nAda,36\nAlan,41", "people.csv"));

        editor.WaitForElement(".omni-html-editor__table-preview");
        Assert.Equal(["Nom", "Âge"], editor.FindAll(".omni-html-editor__table-preview th").Select(cell => cell.TextContent));
        Assert.Equal(4, editor.FindAll(".omni-html-editor__table-preview td").Count);

        await editor.Find(".omni-html-editor__panel .omni-button--success").ClickAsync(new MouseEventArgs());

        var exec = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("inserthtml", exec.Arguments[1]);
        var inserted = (string)exec.Arguments[2]!;
        Assert.StartsWith("<table><thead><tr><th>Nom</th>", inserted, StringComparison.Ordinal);
        Assert.Contains("<tbody><tr><td>Ada</td><td>36</td></tr><tr><td>Alan</td><td>41</td></tr></tbody></table>", inserted, StringComparison.Ordinal);
        Assert.Empty(editor.FindAll(".omni-html-editor__panel"));
    }

    [Fact]
    public async Task ImportTable_UsesAnExtensionReader_ForItsFormats_AndReportsItsFailure()
    {
        SetupSurface();
        var fail = false;
        var extension = new ReaderExtension(new OmniHtmlEditorTableReader([".xlsx"], (name, _) => fail
            ? throw new HttpRequestException("down")
            : Task.FromResult<IReadOnlyList<IReadOnlyList<string>>>([["1", "2"], ["3"]])));
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.ImportTable])
            .Add(component => component.Extensions, [extension]));
        await editor.Find("[data-command=import-table]").ClickAsync(new MouseEventArgs());

        Assert.Equal(".csv,.tsv,.xlsx", editor.Find("input[type=file]").GetAttribute("accept"));
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "sheet.xlsx"));

        editor.WaitForElement(".omni-html-editor__table-preview");
        Assert.Equal(4, editor.FindAll(".omni-html-editor__table-preview td").Count);

        fail = true;
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1], "sheet.xlsx"));

        editor.WaitForElement(".omni-html-editor__panel [role=alert]");
        Assert.Empty(editor.FindAll(".omni-html-editor__table-preview"));
    }

    [Fact]
    public async Task ImportTable_OfAnUnreadFormat_SaysItIsNotSupported()
    {
        SetupSurface();
        var editor = RenderEditor([OmniHtmlEditorCommands.ImportTable]);
        await editor.Find("[data-command=import-table]").ClickAsync(new MouseEventArgs());

        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "notes.txt"));

        editor.WaitForElement(".omni-html-editor__panel [role=alert]");
    }

    private BunitJSModuleInterop SetupSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult(null);
        return module;
    }

    private IRenderedComponent<OmniHtmlEditor> RenderEditor(IReadOnlyList<OmniHtmlEditorCommand> commands)
    {
        var value = "<p>A</p>";
        return Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, commands));
    }

    private sealed class ReaderExtension(OmniHtmlEditorTableReader reader) : OmniHtmlEditorExtension
    {
        public override IReadOnlyList<OmniHtmlEditorTableReader> TableReaders => [reader];
    }
}
