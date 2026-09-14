using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The visual face of <see cref="OmniHtmlEditor"/> and its command model. The script is replaced by
/// bUnit's module double, so these tests cover what .NET decides: what reaches the surface, what is
/// kept from it, and which toolbar is drawn. The script itself is exercised in a browser.
/// </summary>
public sealed class HtmlEditorVisualTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";

    [Fact]
    public void Visual_IsTheDefaultFace_AndTheSurfaceOnlyEverReceivesSanitisedHtml()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p onclick=\"steal()\">Sain</p><script>alert(1)</script>";

        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var surface = editor.Find(".omni-html-editor__surface");
        Assert.Equal("true", surface.GetAttribute("contenteditable"));
        Assert.Equal("textbox", surface.GetAttribute("role"));
        Assert.Empty(editor.FindAll("textarea"));
        var mount = Assert.Single(module.Invocations["mount"]);
        Assert.Equal("<p>Sain</p>", mount.Arguments[2]);
        Assert.IsType<DotNetObjectReference<HtmlEditorInteropBridge>>(mount.Arguments[1]);
    }

    [Fact]
    public async Task Visual_SurfaceInput_IsSanitisedCommittedAndUndoneThroughTheSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnVisualInput("<p onclick=\"x()\">Nouveau <img src=x onerror=bad()></p><script>alert(1)</script>"));

        Assert.Equal("<p>Nouveau </p>", value);
        Assert.Empty(module.Invocations["setHtml"]);

        editor.Find("button[data-command=undo]").Click();

        Assert.Equal("<p>Bonjour</p>", value);
        editor.WaitForAssertion(() => Assert.Equal("<p>Bonjour</p>", Assert.Single(module.Invocations["setHtml"]).Arguments[1]));
    }

    [Fact]
    public void Visual_ToolbarCommand_RunsInTheSurfaceAndItsResultIsSanitisedBeforeBeingKept()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", invocation => Equals(invocation.Arguments[1], "bold"))
            .SetResult("<p><b>Bonjour</b><span onclick=\"x()\"></span></p>");
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value));

        editor.Find("button[data-command=bold]").Click();

        Assert.Equal("<p><b>Bonjour</b><span></span></p>", value);
        Assert.False(editor.Find("button[data-command=undo]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Visual_SelectionState_PressesTheMatchingControls()
    {
        JSInterop.SetupModule(ModulePath);
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        await editor.InvokeAsync(() => bridge.OnVisualState("bold link|h2|center|normal"));

        Assert.Equal("true", editor.Find("button[data-command=bold]").GetAttribute("aria-pressed"));
        Assert.Equal("false", editor.Find("button[data-command=italic]").GetAttribute("aria-pressed"));
        Assert.Equal("true", editor.Find("button[data-command=link]").GetAttribute("aria-pressed"));
        Assert.Equal("true", editor.Find("button[data-command=align-center]").GetAttribute("aria-pressed"));
        Assert.Equal("false", editor.Find("button[data-command=align-left]").GetAttribute("aria-pressed"));
        Assert.Equal("h2", editor.Find("select[data-command=block-format]").GetAttribute("value"));
        Assert.Null(editor.Find("button[data-command=undo]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Commands_ReplaceTheToolbar_InTheirOrder_AndACustomCommandInsertsSanitisedHtml()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", invocation => Equals(invocation.Arguments[1], "inserthtml"))
            .SetResult("<p>Bonjour</p><p>Signature</p>");
        var value = "<p>Bonjour</p>";
        var signature = OmniHtmlEditorCommand.Create(
            "signature",
            "Signature",
            context => context.InsertHtmlAsync("<p onclick=\"x()\">Signature</p>"),
            OmniIconName.Edit);
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.Italic, OmniHtmlEditorCommands.Separator, OmniHtmlEditorCommands.Bold, signature]));

        var names = editor.FindAll(".omni-html-editor__toolbar [data-command]").Select(control => control.GetAttribute("data-command") ?? string.Empty).ToArray();
        Assert.Equal(["italic", "bold", "signature"], names);
        Assert.Single(editor.FindAll(".omni-html-editor__separator"));
        Assert.Equal("Signature", editor.Find("button[data-command=signature]").GetAttribute("aria-label"));

        editor.Find("button[data-command=signature]").Click();

        var insert = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("<p>Signature</p>", insert.Arguments[2]);
        Assert.Equal("<p>Bonjour</p><p>Signature</p>", value);
    }

    [Fact]
    public void Separators_NeverLead_TrailOrRepeat()
    {
        JSInterop.SetupModule(ModulePath);
        var value = string.Empty;
        var separator = OmniHtmlEditorCommands.Separator;

        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.EnableItalic, false)
            .Add(component => component.Commands, [separator, OmniHtmlEditorCommands.Bold, separator, OmniHtmlEditorCommands.Italic, separator, separator, OmniHtmlEditorCommands.Underline, separator]));

        var toolbar = editor.Find(".omni-html-editor__toolbar");
        Assert.Equal(["bold", "underline"], toolbar.QuerySelectorAll("[data-command]").Select(control => control.GetAttribute("data-command") ?? string.Empty).ToArray());
        Assert.Single(toolbar.QuerySelectorAll(".omni-html-editor__separator"));
    }

    [Fact]
    public void DefaultToolbar_OffersTheExpectedCommands_AndTheLegacySwitchesStillHideTheirs()
    {
        JSInterop.SetupModule(ModulePath);
        var value = string.Empty;

        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.EnableSubscript, false)
            .Add(component => component.EnableOutdent, false));

        var names = editor.FindAll(".omni-html-editor__toolbar [data-command]").Select(control => control.GetAttribute("data-command")).ToHashSet();
        foreach (var expected in new[] { "block-format", "bold", "italic", "underline", "strikethrough", "sup", "inline-code", "bullet-list", "numbered-list", "indent", "quote", "code-block", "link", "unlink", "align-left", "align-center", "align-right", "align-justify", "clear-formatting", "undo", "redo", "toggle-source" })
        {
            Assert.Contains(expected, names);
        }

        Assert.DoesNotContain("sub", names);
        Assert.DoesNotContain("outdent", names);
        Assert.Equal("bold", editor.Find(".omni-html-editor__toolbar button").GetAttribute("data-command"));
        Assert.All(editor.FindAll(".omni-html-editor__toolbar button"), button => Assert.False(string.IsNullOrWhiteSpace(button.GetAttribute("aria-label"))));
    }

    [Fact]
    public void CustomCommand_WithoutAHandler_IsRejected()
    {
        var value = string.Empty;

        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Commands, [new OmniHtmlEditorCommand("empty", OmniHtmlEditorAction.Custom)])));

        Assert.Contains("'empty'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SourceButton_TakesInPendingTyping_SwitchesToTheTextarea_AndReportsTheMode()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("read", _ => true).SetResult("<p>Tapé à l'instant</p>");
        module.SetupVoid("dispose", _ => true).SetVoidResult();
        var value = "<p>Bonjour</p>";
        var mode = OmniHtmlEditorMode.Visual;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ModeChanged, updated => mode = updated));

        await editor.Find("button[data-command=toggle-source]").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Equal(OmniHtmlEditorMode.Source, mode);
        Assert.Equal("<p>Tapé à l'instant</p>", value);
        Assert.Single(module.Invocations["dispose"]);
        Assert.Single(module.Invocations["mount"]);
        Assert.Empty(editor.FindAll(".omni-html-editor__surface"));
        Assert.Equal("true", editor.Find("button[data-command=toggle-source]").GetAttribute("aria-pressed"));
        Assert.True(editor.Find("button[data-command=unlink]").HasAttribute("disabled"));
        Assert.True(editor.Find("button[data-command=clear-formatting]").HasAttribute("disabled"));
        Assert.False(editor.Find("button[data-command=bold]").HasAttribute("disabled"));
    }

    [Fact]
    public void ModeParameter_SwitchesTheFace_BothWays()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source));

        Assert.Single(editor.FindAll("textarea"));
        Assert.Empty(module.Invocations["mount"]);

        editor.Render(parameters => parameters.Add(component => component.Mode, OmniHtmlEditorMode.Visual));

        Assert.Empty(editor.FindAll("textarea"));
        Assert.Single(module.Invocations["mount"]);
    }

    [Fact]
    public void LinkField_RefusesAScriptAddress_AndLinksTheSelectionToASafeOne()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", invocation => Equals(invocation.Arguments[1], "link"))
            .SetResult("<p><a href=\"https://example.test/\" rel=\"noopener noreferrer\">Bonjour</a></p>");
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value));

        editor.Find("button[data-command=link]").Click();
        var field = editor.Find(".omni-html-editor__link-input");
        field.Input("javascript:alert(1)");
        field.KeyDown("Enter");

        Assert.NotNull(editor.Find(".omni-html-editor__link-error"));
        Assert.Empty(module.Invocations["exec"]);

        editor.Find(".omni-html-editor__link-input").Input("https://example.test/");
        editor.Find(".omni-html-editor__link button").Click();

        var link = Assert.Single(module.Invocations["exec"]);
        Assert.Equal("https://example.test/", link.Arguments[2]);
        Assert.Empty(editor.FindAll(".omni-html-editor__link"));
        Assert.Contains("href=\"https://example.test/\"", value, StringComparison.Ordinal);
    }

    [Fact]
    public void Disabled_Visual_LocksTheSurfaceAndIgnoresItsInput()
    {
        JSInterop.SetupModule(ModulePath);
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Disabled, true));

        Assert.Equal("false", editor.Find(".omni-html-editor__surface").GetAttribute("contenteditable"));
        Assert.All(editor.FindAll(".omni-html-editor__toolbar :is(button, select)"), control => Assert.True(control.HasAttribute("disabled")));

        editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnVisualInput("<p>Changé</p>"));

        Assert.Equal("<p>Bonjour</p>", value);
    }

    [Theory]
    [InlineData("<p class=\"omni-align-center evil\">Centré</p>", "<p class=\"omni-align-center\">Centré</p>")]
    [InlineData("<p>Un <span class=\"omni-font-size-large\">grand</span> <span class=\"x\">mot</span></p>", "<p>Un <span class=\"omni-font-size-large\">grand</span> <span>mot</span></p>")]
    [InlineData("<section><font color=\"red\">Texte <img src=x onerror=bad()></font><script>alert(1)</script></section>", "Texte ")]
    [InlineData("<table><tbody><tr><td colspan=\"2\" style=\"color:red\">A</td></tr></tbody></table>", "<table><tbody><tr><td colspan=\"2\">A</td></tr></tbody></table>")]
    [InlineData("<p><u>souligné</u> <s>barré</s></p><hr>", "<p><u>souligné</u> <s>barré</s></p><hr>")]
    public void Sanitizer_KeepsTheEditorsFormatting_AndNothingElse(string input, string expected) =>
        Assert.Equal(expected, OmniHtmlSanitizer.Sanitize(input));

    [Fact]
    public void Paste_OfPlainText_BecomesEscapedParagraphs()
    {
        Assert.Equal("un &lt;b&gt;mot&lt;/b&gt;", OmniHtmlSanitizer.SanitizePaste(string.Empty, "un <b>mot</b>"));
        Assert.Equal("<p>Ligne 1<br>Ligne 2</p><p>Autre &amp; suite</p>", OmniHtmlSanitizer.SanitizePaste(null, "Ligne 1\r\nLigne 2\r\n\r\nAutre & suite"));
        Assert.Equal("<p>Gras</p>", OmniHtmlSanitizer.SanitizePaste("<p style=\"font-weight:bold\" onclick=\"x()\">Gras</p><script>alert(1)</script>", "Gras"));
    }
}
