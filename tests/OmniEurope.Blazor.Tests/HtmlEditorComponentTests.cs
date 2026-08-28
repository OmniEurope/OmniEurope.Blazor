using Bunit;
using OmniEurope.Blazor.Components;
using System.Text.Json;

namespace OmniEurope.Blazor.Tests;

public sealed class HtmlEditorComponentTests : OmniBunitContext
{
    [Fact]
    public void Editor_SanitizesAnUntrustedInitialValueBeforePreviewRendering()
    {
        var value = "<p onclick=\"steal()\">Sain</p><script>alert(1)</script>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        var preview = editor.Find(".omni-html-editor__preview");
        Assert.Equal("<p>Sain</p>", preview.InnerHtml);
        Assert.DoesNotContain("script", preview.InnerHtml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", preview.InnerHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Editor_SanitizesMaliciousHtmlWithAnAllowList()
    {
        var editor = Render<HtmlEditorTestHost>();
        editor.Find("#editor").Input("<p onclick=\"steal()\">Sain</p><script>alert(1)</script><a href=\"javascript:bad()\">Lien</a><img src=x onerror=bad()>");

        Assert.Equal("<p>Sain</p><a>Lien</a>", editor.Instance.Model.Html);
        Assert.DoesNotContain("script", editor.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", editor.Instance.Model.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", editor.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", editor.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Editor_AdoptsMockedInteropResultsAndSupportsDeterministicUndoRedo()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omniInterop.js");
        var selection = JsonDocument.Parse("""{"value":"<p><strong>Bonjour</strong></p>","selectionStart":11,"selectionEnd":18}""").RootElement.Clone();
        module.Setup<JsonElement>("wrapTextSelection", _ => true).SetResult(selection);
        module.SetupVoid("restoreTextSelection", _ => true);
        var editor = Render<HtmlEditorTestHost>();

        editor.Find("button[aria-label=Gras]").Click();
        Assert.Equal("<p><strong>Bonjour</strong></p>", editor.Instance.Model.Html);

        editor.Find("button[aria-label=Annuler]").Click();
        Assert.Equal("<p>Bonjour</p>", editor.Instance.Model.Html);

        editor.Find("button[aria-label=Rétablir]").Click();
        Assert.Equal("<p><strong>Bonjour</strong></p>", editor.Instance.Model.Html);

        editor.Find("button[aria-label=Titre]").Click();
        Assert.StartsWith("<h2>", editor.Instance.Model.Html, StringComparison.Ordinal);
        Assert.Equal("toolbar", editor.Find(".omni-html-editor__toolbar").GetAttribute("role"));
        Assert.Single(module.Invocations["wrapTextSelection"]);
        Assert.Single(module.Invocations["restoreTextSelection"]);
    }

    [Fact]
    public void Editor_PreservesSafeLinksAndAddsRelProtection()
    {
        var editor = Render<HtmlEditorTestHost>();
        editor.Find("#editor").Input("<a href=\"https://example.test/path?a=1&amp;b=2\">Site</a>");

        Assert.Contains("href=\"https://example.test/path?a=1&amp;b=2\"", editor.Instance.Model.Html, StringComparison.Ordinal);
        Assert.Contains("rel=\"noopener noreferrer\"", editor.Instance.Model.Html, StringComparison.Ordinal);
    }

    [Fact]
    public void Editor_ValueIsStableAcrossSerializationAndInputRoundTrip()
    {
        var editor = Render<HtmlEditorTestHost>();
        editor.Find("#editor").Input("<h2>Titre &amp; suite</h2><p><strong>Texte</strong><br><a href=\"/fiche?id=42&amp;mode=read\">Fiche</a></p>");
        var canonical = editor.Instance.Model.Html;

        var serialized = JsonSerializer.Serialize(canonical);
        var restored = JsonSerializer.Deserialize<string>(serialized);
        editor.Find("#editor").Input(restored);

        Assert.Equal(canonical, restored);
        Assert.Equal(canonical, editor.Instance.Model.Html);
        Assert.Contains("rel=\"noopener noreferrer\"", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void Editor_DisabledBlocksEveryToolbarAndInputMutation()
    {
        var value = "<p>Bonjour</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, updated => value = updated)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Disabled, true)
            .Add(component => component.CustomTools,
                [new OmniHtmlEditorTool("heading", "Titre", current => $"<h2>{current}</h2>")]));

        Assert.All(editor.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
        Assert.True(editor.Find("textarea").HasAttribute("disabled"));

        editor.Find("button[aria-label=Titre]").Click();
        editor.Find("textarea").Input("<p>Changed</p>");

        Assert.Equal("<p>Bonjour</p>", value);
        Assert.Empty(JSInterop.Invocations);
    }

    [Theory]
    [InlineData("<SCRIPT SRC=//evil.test/x.js></SCRIPT><p>Sain</p>")]
    [InlineData("<a href=\"&#x6a;avascript:alert(1)\">Lien</a>")]
    [InlineData("<svg><script>alert(1)</script></svg><strong>Sain</strong>")]
    [InlineData("<!-- <img src=x onerror=alert(1)> --><em>Sain</em>")]
    [InlineData("<a href=\"java&#x0D;script:alert(1)\">Lien</a>")]
    [InlineData("<math><mtext><table><mglyph><style><!--</style><img title=\"--><img src=x onerror=alert(1)>\">")]
    [InlineData("<svg><g/onload=alert(1)//<p>Sain</p>")]
    [InlineData("<<script>alert(1)//<</script><p>Sain</p>")]
    public void Editor_RejectsAdditionalXssVectors(string payload)
    {
        var editor = Render<HtmlEditorTestHost>();

        editor.Find("#editor").Input(payload);

        Assert.DoesNotContain("script", editor.Instance.Model.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", editor.Instance.Model.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", editor.Instance.Model.Html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", editor.Instance.Model.Html, StringComparison.OrdinalIgnoreCase);
    }
}
