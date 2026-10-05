using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The table import panel on files it cannot use, the toolbar let free again after holding to its rows or
/// on a lost circuit, and an inline element that asks the editor around the caret then removes itself.
/// </summary>
public sealed class EditorComponentEdgeTests : OmniBunitContext
{
    private sealed class TextFile(string name, string content) : IBrowserFile
    {
        public string Name => name;

        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;

        public long Size => Encoding.UTF8.GetByteCount(content);

        public string ContentType => "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public async Task TableImport_WithoutAFile_ReadsNothing()
    {
        var panels = new HtmlEditorPanels();

        await panels.ReadTableFileAsync([], []);

        Assert.Null(panels.TableRows);
        Assert.Null(panels.TableErrorKey);
        Assert.Null(panels.TakeTable());
    }

    [Fact]
    public async Task TableImport_ReadsTabSeparatedFiles_AndASingleRowIsNoHeader()
    {
        var panels = new HtmlEditorPanels();

        await panels.ReadTableFileAsync([new TextFile("ventes.tsv", "Nom\tVille")], []);

        Assert.Equal(["Nom", "Ville"], panels.TableRows![0]);
        Assert.False(panels.TableHeader);
    }

    [Fact]
    public async Task TableImport_OfABlankFile_SaysItIsEmpty()
    {
        var panels = new HtmlEditorPanels();

        await panels.ReadTableFileAsync([new TextFile("vide.csv", " , \n\n")], []);

        Assert.Equal("HtmlEditorImportTableEmpty", panels.TableErrorKey);
        Assert.Null(panels.TableRows);
    }

    [Fact]
    public async Task ToolbarHeldToItsRows_IsLetFreeAgain_AndALostCircuitIsIgnored()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Mode = JSRuntimeMode.Loose;
        var value = "<p>a</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ToolbarRows, 1));
        Assert.Single(module.Invocations["fitToolbar"]);

        editor.Render(parameters => parameters.Add(component => component.ToolbarRows, null));
        Assert.Single(module.Invocations["unfitToolbar"]);
        editor.Render();
        Assert.Single(module.Invocations["unfitToolbar"]);
    }

    [Fact]
    public void ToolbarFit_OnALostCircuit_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["installPackageTooltips"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var value = "<p>a</p>";

        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ToolbarRows, 1));

        Assert.Contains("installPackageTooltips", runtime.Module.Calls);
        Assert.NotEmpty(editor.FindAll(".omni-html-editor"));
    }

    [Fact]
    public async Task InlineElement_AsksAroundTheCaret_ThenRemovesItself()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<string?>("aroundCaret", _ => true).SetResult(null);
        module.Setup<string?>("replaceActivated", _ => true).SetResult("<p>Texte </p>");
        OmniHtmlCaretSplit? split = null;
        var extension = new InlineExtension(new OmniHtmlEditorInlineElement(".note", async context =>
        {
            split = await context.Editor.GetHtmlAroundCaretAsync();
            await context.RemoveAsync();
        }));
        string? bound = "<p>Texte <span class=\"note\">Ancienne</span></p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, bound)
            .Add(component => component.ValueExpression, () => bound)
            .Add(component => component.ValueChanged, next => bound = next)
            .Add(component => component.Extensions, [extension]));

        await editor.InvokeAsync(() => new HtmlEditorInteropBridge(editor.Instance).OnElementActivated(0, "{\"tag\":\"span\",\"classes\":[\"note\"]}", "Ancienne"));

        Assert.NotNull(split);
        Assert.Equal(string.Empty, Assert.Single(module.Invocations["replaceActivated"]).Arguments[1]);
        Assert.Equal("<p>Texte </p>", bound);
    }

    private sealed class InlineExtension(OmniHtmlEditorInlineElement element) : OmniHtmlEditorExtension
    {
        public override IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements => [element];

        public override OmniHtmlSanitizerPolicy? SanitizerPolicy { get; } = new() { AdditionalCssClasses = ["note"] };
    }
}
