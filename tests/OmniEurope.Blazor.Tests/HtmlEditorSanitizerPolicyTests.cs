using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniHtmlSanitizerPolicy"/>: what a host adds to the allow-list survives every route
/// into the value, and nothing a policy names can reopen a script, a handler or a style.
/// </summary>
public sealed class HtmlEditorSanitizerPolicyTests : OmniBunitContext
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-html-editor.js";

    private const string HostMarkup =
        "<aside class=\"akn-authorial-note\" data-marker=\"1\" data-eid=\"n_1\" contenteditable=\"false\">Note</aside>" +
        "<p>Texte<sup class=\"akn-noteref\" data-eid=\"ref_1\">1</sup> <span class=\"akn-formula\" data-latex=\"x^2\">x²</span></p>" +
        "<table><colgroup><col></colgroup><tbody><tr><td><img src=\"https://example.test/a.png\" alt=\"Plan\"></td></tr></tbody></table>";

    private static readonly OmniHtmlSanitizerPolicy AknPolicy = new()
    {
        AdditionalTags = ["aside", "img", "colgroup", "col"],
        AdditionalAttributes = ["contenteditable"],
        AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["img"] = ["src", "alt"] },
        AdditionalCssClasses = ["akn-authorial-note", "akn-noteref", "akn-formula"],
        AllowDataAttributes = true
    };

    [Fact]
    public void Policy_KeepsTheHostMarkup_ThatTheBuiltInListDrops()
    {
        Assert.Equal(HostMarkup, OmniHtmlSanitizer.Sanitize(HostMarkup, AknPolicy));

        var builtIn = OmniHtmlSanitizer.Sanitize(HostMarkup);
        Assert.DoesNotContain("aside", builtIn, StringComparison.Ordinal);
        Assert.DoesNotContain("data-", builtIn, StringComparison.Ordinal);
        Assert.DoesNotContain("img", builtIn, StringComparison.Ordinal);
        Assert.DoesNotContain("akn-", builtIn, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<p onclick=\"x()\" style=\"color:red\" data-eid=\"p_1\">A</p>", "<p data-eid=\"p_1\">A</p>")]
    [InlineData("<img src=\"javascript:alert(1)\" alt=\"x\" onerror=\"bad()\">", "<img alt=\"x\">")]
    [InlineData("<img src=\"data:text/html;base64,PHNjcmlwdD4=\" alt=\"x\">", "<img alt=\"x\">")]
    [InlineData("<aside><script>alert(1)</script>Note</aside>", "<aside>Note</aside>")]
    [InlineData("<a href=\"javascript:alert(1)\" data-x=\"1\">lien</a>", "<a data-x=\"1\">lien</a>")]
    [InlineData("<p class=\"akn-formula evil\">A</p>", "<p class=\"akn-formula\">A</p>")]
    [InlineData("<p src=\"https://example.test/a.png\" alt=\"x\">A</p>", "<p>A</p>")]
    [InlineData("<iframe src=\"https://example.test/\"></iframe><style>p{}</style><p>B</p>", "<p>B</p>")]
    public void Policy_NeverReopensScriptsHandlersStylesOrScriptAddresses(string input, string expected) =>
        Assert.Equal(expected, OmniHtmlSanitizer.Sanitize(input, AknPolicy));

    [Fact]
    public void AllowAnyClass_KeepsEveryClass_AndTheBuiltInListStillDropsThem()
    {
        var policy = new OmniHtmlSanitizerPolicy { AllowAnyClass = true };

        Assert.Equal("<p class=\"anything else\">A</p>", OmniHtmlSanitizer.Sanitize("<p class=\"anything else\">A</p>", policy));
        Assert.Equal("<p>A</p>", OmniHtmlSanitizer.Sanitize("<p class=\"anything else\">A</p>"));
        Assert.Null(OmniHtmlSanitizer.ClassesOf(policy));
    }

    [Theory]
    [InlineData("script", null)]
    [InlineData("style", null)]
    [InlineData("iframe", null)]
    [InlineData("svg", null)]
    [InlineData(null, "onclick")]
    [InlineData(null, "ONLOAD")]
    [InlineData(null, "style")]
    [InlineData(null, "srcdoc")]
    [InlineData(null, "xlink:href")]
    public void Policy_NamingSomethingNeverAllowed_Throws(string? tag, string? attribute)
    {
        var policy = new OmniHtmlSanitizerPolicy
        {
            AdditionalTags = tag is null ? [] : [tag],
            AdditionalAttributes = attribute is null ? [] : [attribute]
        };

        var exception = Assert.Throws<ArgumentException>(() => OmniHtmlSanitizer.Sanitize("<p>A</p>", policy));
        Assert.Contains($"'{(tag ?? attribute)!.ToLowerInvariant()}'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Policy_WithAForbiddenPerTagAttribute_IsRejectedByTheEditorItself()
    {
        var value = string.Empty;
        var policy = new OmniHtmlSanitizerPolicy
        {
            AdditionalTags = ["img"],
            AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["img"] = ["onerror"] }
        };

        Assert.Throws<ArgumentException>(() => Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.SanitizerPolicy, policy)));
    }

    [Fact]
    public async Task Editor_AppliesThePolicy_ToTheBoundValueTypingPasteInsertAndCommandResults()
    {
        var module = JSInterop.SetupModule(ModulePath);
        module.Setup<string?>("exec", invocation => Equals(invocation.Arguments[1], "bold"))
            .SetResult("<p><b>A</b><span data-eid=\"s_1\" onclick=\"x()\">B</span></p>");
        module.Setup<string?>("exec", invocation => Equals(invocation.Arguments[1], "inserthtml"))
            .SetResult("<p>A</p><aside data-marker=\"2\">Nouvelle note</aside>");
        var value = HostMarkup + "<script>alert(1)</script>";
        var insert = OmniHtmlEditorCommand.Create(
            "note",
            "Note",
            context => context.InsertHtmlAsync("<aside data-marker=\"2\" onmouseover=\"x()\">Nouvelle note</aside>"));
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.SanitizerPolicy, AknPolicy)
            .Add(component => component.Commands, [OmniHtmlEditorCommands.Bold, insert]));
        var bridge = new HtmlEditorInteropBridge(editor.Instance);

        var mount = Assert.Single(module.Invocations["mount"]);
        Assert.Equal(HostMarkup, mount.Arguments[2]);
        var options = JsonSerializer.SerializeToElement(mount.Arguments[3]);
        Assert.True(options.GetProperty("policy").GetBoolean());
        Assert.Contains("akn-formula", options.GetProperty("classes").EnumerateArray().Select(entry => entry.GetString()));
        Assert.Contains("omni-align-center", options.GetProperty("classes").EnumerateArray().Select(entry => entry.GetString()));

        await editor.InvokeAsync(() => bridge.OnVisualInput("<aside data-eid=\"n_9\" onclick=\"x()\" style=\"color:red\">Tapé</aside>"));
        Assert.Equal("<aside data-eid=\"n_9\">Tapé</aside>", value);

        Assert.Equal("<aside data-marker=\"3\">Collé</aside>", bridge.SanitizePaste("<aside data-marker=\"3\"><script>x()</script>Collé</aside>", "Collé"));

        editor.Find("button[data-command=note]").Click();
        var inserted = module.Invocations["exec"].Single(invocation => Equals(invocation.Arguments[1], "inserthtml"));
        Assert.Equal("<aside data-marker=\"2\">Nouvelle note</aside>", inserted.Arguments[2]);
        Assert.Equal("<p>A</p><aside data-marker=\"2\">Nouvelle note</aside>", value);

        editor.Find("button[data-command=bold]").Click();
        Assert.Equal("<p><b>A</b><span data-eid=\"s_1\">B</span></p>", value);
    }

    [Fact]
    public void SourceFace_KeepsThePolicyMarkup_InTheValueAndThePreview()
    {
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.SanitizerPolicy, AknPolicy));

        editor.Find("textarea").Input("<aside class=\"akn-authorial-note\" data-marker=\"1\">Note</aside><script>x()</script>");

        Assert.Equal("<aside class=\"akn-authorial-note\" data-marker=\"1\">Note</aside>", value);
        Assert.NotNull(editor.Find(".omni-html-editor__preview aside.akn-authorial-note[data-marker='1']"));
    }

    [Fact]
    public void WithoutAPolicy_TheSurfaceReceivesOnlyItsRows()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";

        Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var options = JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);
        Assert.Equal(["rows"], options.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void ChangingThePolicy_ReconfiguresTheMountedSurface()
    {
        var module = JSInterop.SetupModule(ModulePath);
        var value = "<p>A</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        editor.Render(parameters => parameters.Add(component => component.SanitizerPolicy, new OmniHtmlSanitizerPolicy { AllowAnyClass = true }));

        var configure = Assert.Single(module.Invocations["configure"]);
        var options = JsonSerializer.SerializeToElement(configure.Arguments[1]);
        Assert.True(options.GetProperty("policy").GetBoolean());
        Assert.Equal(JsonValueKind.Null, options.GetProperty("classes").ValueKind);
    }
}
