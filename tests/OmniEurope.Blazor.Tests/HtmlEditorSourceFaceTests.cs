using System.Text.Json;
using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The source face of the HTML editor: the tags each built-in action writes around the selection of the
/// text area, the actions it transforms itself and those it cannot write.
/// </summary>
public sealed class HtmlEditorSourceFaceTests : OmniBunitContext
{
    private readonly BunitJSModuleInterop _module;
    private readonly string _bound = "<p>texte</p>";

    public HtmlEditorSourceFaceTests()
    {
        _module = JSInterop.SetupModule(OmniModules.Interop);
        _module.SetupVoid("restoreTextSelection", _ => true).SetVoidResult();
        Answer("<p>texte</p>");
    }

    private void Answer(string? value)
    {
        var json = JsonSerializer.Serialize(new { value, selectionStart = 0, selectionEnd = 0 });
        _module.Setup<JsonElement>("wrapTextSelection", _ => true).SetResult(JsonDocument.Parse(json).RootElement.Clone());
    }

    private IRenderedComponent<OmniHtmlEditor> RenderSource(string value = "<p>texte</p>") =>
        Render<OmniHtmlEditor>(parameters => parameters
            .Add(editor => editor.Value, value)
            .Add(editor => editor.Mode, OmniHtmlEditorMode.Source)
            .Add(editor => editor.ValueExpression, () => value));

    private (string Prefix, string Suffix)? LastWrap()
    {
        var calls = _module.Invocations["wrapTextSelection"];
        return calls.Count == 0 ? null : ((string)calls[^1].Arguments[1]!, (string)calls[^1].Arguments[2]!);
    }

    [Theory]
    [InlineData(OmniHtmlEditorAction.Bold, "<strong>", "</strong>")]
    [InlineData(OmniHtmlEditorAction.Italic, "<em>", "</em>")]
    [InlineData(OmniHtmlEditorAction.Underline, "<u>", "</u>")]
    [InlineData(OmniHtmlEditorAction.Strikethrough, "<s>", "</s>")]
    [InlineData(OmniHtmlEditorAction.Highlight, "<mark>", "</mark>")]
    [InlineData(OmniHtmlEditorAction.Subscript, "<sub>", "</sub>")]
    [InlineData(OmniHtmlEditorAction.Superscript, "<sup>", "</sup>")]
    [InlineData(OmniHtmlEditorAction.InlineCode, "<code>", "</code>")]
    [InlineData(OmniHtmlEditorAction.CodeBlock, "<pre>", "</pre>")]
    [InlineData(OmniHtmlEditorAction.Quote, "<blockquote>", "</blockquote>")]
    [InlineData(OmniHtmlEditorAction.Indent, "<blockquote>", "</blockquote>")]
    [InlineData(OmniHtmlEditorAction.BulletList, "<ul><li>", "</li></ul>")]
    [InlineData(OmniHtmlEditorAction.NumberedList, "<ol><li>", "</li></ol>")]
    [InlineData(OmniHtmlEditorAction.AlignLeft, "<p>", "</p>")]
    [InlineData(OmniHtmlEditorAction.AlignCenter, "<p class=\"omni-align-center\">", "</p>")]
    [InlineData(OmniHtmlEditorAction.AlignRight, "<p class=\"omni-align-end\">", "</p>")]
    [InlineData(OmniHtmlEditorAction.AlignJustify, "<p class=\"omni-align-justify\">", "</p>")]
    public async Task PlainAction_WrapsTheSelectionInItsTags(OmniHtmlEditorAction action, string prefix, string suffix)
    {
        var editor = RenderSource();

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(action, null));

        Assert.Equal((prefix, suffix), LastWrap());
        Assert.Single(_module.Invocations["restoreTextSelection"]);
    }

    [Fact]
    public async Task InsertTable_WritesAThreeByThreeTableAtTheCaret()
    {
        var editor = RenderSource();

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.InsertTable, null));

        var (prefix, suffix) = LastWrap()!.Value;
        Assert.Equal(9, prefix.Split("<td>").Length - 1);
        Assert.Equal(string.Empty, suffix);
    }

    [Theory]
    [InlineData("h2", "<h2>", "</h2>")]
    [InlineData("script", "<p>", "</p>")]
    [InlineData(null, "<p>", "</p>")]
    public async Task BlockFormat_WritesAnOfferedFormat_ElseAParagraph(string? argument, string prefix, string suffix)
    {
        var editor = RenderSource();

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.BlockFormat, argument));

        Assert.Equal((prefix, suffix), LastWrap());
    }

    [Theory]
    [InlineData("small")]
    [InlineData("large")]
    [InlineData("xlarge")]
    public async Task FontSize_WritesItsSpan(string size)
    {
        var editor = RenderSource();

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.FontSize, size));

        Assert.Equal(($"<span class=\"omni-font-size-{size}\">", "</span>"), LastWrap());
    }

    [Theory]
    [InlineData(OmniHtmlEditorAction.FontSize, "huge")]
    [InlineData(OmniHtmlEditorAction.FontSize, null)]
    [InlineData(OmniHtmlEditorAction.Link, "")]
    [InlineData(OmniHtmlEditorAction.Unlink, null)]
    [InlineData(OmniHtmlEditorAction.ClearFormatting, null)]
    public async Task ActionTheSourceFaceCannotWrite_DoesNothing(OmniHtmlEditorAction action, string? argument)
    {
        var editor = RenderSource();

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(action, argument));

        Assert.Null(LastWrap());
        Assert.Equal("<p>texte</p>", editor.Instance.Value);
    }

    [Fact]
    public async Task Link_WritesAnEncodedHref()
    {
        var editor = RenderSource();

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Link, "https://exemple.test/?a=1&b=\"2\""));

        Assert.Equal(("<a href=\"https://exemple.test/?a=1&amp;b=&quot;2&quot;\">", "</a>"), LastWrap());
    }

    [Theory]
    [InlineData("<blockquote><p>cité</p></blockquote>", "<p>cité</p>")]
    [InlineData("<BLOCKQUOTE><p>cité</p></BLOCKQUOTE>", "<p>cité</p>")]
    [InlineData("<p>libre</p>", "<p>libre</p>")]
    [InlineData("<blockquote><p>ouvert</p>", "<blockquote><p>ouvert</p></blockquote>")]
    public async Task Outdent_RemovesTheQuoteAroundTheWholeText_Only(string value, string expected)
    {
        string? changed = null;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.ValueChanged, html => changed = html)
            .Add(component => component.ValueExpression, () => value));

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Outdent, null));

        Assert.Equal(expected, changed ?? editor.Instance.Value);
        Assert.Null(LastWrap());
    }

    [Fact]
    public async Task Wrap_CommitsTheTextTheScriptReturns_AndAnEmptyAnswerEmptiesIt()
    {
        string? changed = null;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, "<p>texte</p>")
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.ValueChanged, html => changed = html)
            .Add(component => component.ValueExpression, () => _bound));

        Answer("<p><strong>texte</strong></p>");
        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Bold, null));
        Assert.Equal("<p><strong>texte</strong></p>", changed);

        Answer(null);
        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Italic, null));
        Assert.Equal(string.Empty, changed);
    }
}
