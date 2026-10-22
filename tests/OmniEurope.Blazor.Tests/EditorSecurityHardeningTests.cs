using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The hardenings of the editor family found by the 2026-10-22 audit: the Monaco location must stay on the
/// origin of the page (and still accept every legitimate same-origin path), the sanitizer holds every
/// address-carrying attribute to the allowed schemes, and a link command never writes a script address.
/// </summary>
public sealed class EditorSecurityHardeningTests : OmniBunitContext
{
    public static TheoryData<string> RefusedMonacoPaths() => new()
    {
        "data:text/javascript,alert(1)//",
        "DATA:text/javascript,alert(1)",
        "blob:https://example.test/6d1f",
        "javascript:alert(1)",
        "https://cdn.example.test/monaco/vs",
        "http://cdn.example.test/monaco/vs",
        "//cdn.example.test/monaco/vs",
        @"\\evil.example.test\vs",
        @"\evil.example.test\vs",
        @"/\evil.example.test/vs",
        "C:/monaco/vs",
        "file:///monaco/vs",
        "x:y",
        "vs\tfoo",
        "/\t/evil.example.test/vs",
        "/vs\n",
        "vs\u0000",
        " /vs",
        "/vs ",
        "",
        "   "
    };

    public static TheoryData<string> AcceptedMonacoPaths() => new()
    {
        "lib/monaco-editor/min/vs",
        "lib/monaco-editor/min/vs/",
        "/_content/x/vs",
        "assets/monaco/vs",
        "./vs",
        "../monaco/vs",
        "/",
        "vs/with:colon",
        "/segment:with:colons/vs",
        "vs?version=2#top"
    };

    [Theory]
    [MemberData(nameof(RefusedMonacoPaths))]
    public void CodeEditor_MonacoPath_NotOnTheOriginOfThePage_IsRefused(string path)
    {
        var value = string.Empty;

        var exception = Assert.Throws<ArgumentException>(() => Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.MonacoPath, path)));

        Assert.Equal(nameof(OmniCodeEditor.MonacoPath), exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(RefusedMonacoPaths))]
    public void DiffViewer_MonacoPath_NotOnTheOriginOfThePage_IsRefused(string path)
    {
        var exception = Assert.Throws<ArgumentException>(() => Render<OmniDiffViewer>(parameters => parameters
            .Add(component => component.MonacoPath, path)));

        Assert.Equal(nameof(OmniDiffViewer.MonacoPath), exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(AcceptedMonacoPaths))]
    public void CodeEditor_MonacoPath_OnTheOriginOfThePage_IsLoadedAsGiven(string path)
    {
        var module = JSInterop.SetupModule(OmniModules.CodeEditor);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("load", _ => true).SetResult(true);
        var value = string.Empty;

        Render<OmniCodeEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.MonacoPath, path));

        Assert.Equal(path, Assert.Single(module.Invocations["load"]).Arguments[0]);
    }

    [Theory]
    [MemberData(nameof(AcceptedMonacoPaths))]
    public void DiffViewer_MonacoPath_OnTheOriginOfThePage_IsLoadedAsGiven(string path)
    {
        var module = JSInterop.SetupModule(OmniModules.CodeEditor);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("load", _ => true).SetResult(true);

        Render<OmniDiffViewer>(parameters => parameters.Add(component => component.MonacoPath, path));

        Assert.Equal(path, Assert.Single(module.Invocations["load"]).Arguments[0]);
    }

    [Fact]
    public void MonacoPath_Default_IsAccepted()
    {
        Assert.Equal("lib/monaco-editor/min/vs", new OmniCodeEditor().MonacoPath);
        Assert.Equal("lib/monaco-editor/min/vs", new OmniDiffViewer().MonacoPath);
        Assert.True(OmniSameOriginPath.IsValid(new OmniCodeEditor().MonacoPath));
    }

    private static readonly OmniHtmlSanitizerPolicy ImagePolicy = new()
    {
        AdditionalTags = ["img"],
        AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>>
        {
            ["img"] = ["src", "srcset", "alt"],
            ["a"] = ["ping"],
            ["td"] = ["background"]
        }
    };

    // The sanitizer library already holds the entries of these two list attributes to the allowed schemes:
    // these tests pin it, so a library change that stopped doing so would be seen.
    [Theory]
    [InlineData("javascript:alert(1) 1x")]
    [InlineData("https://example.test/a.png 1x, javascript:alert(1) 2x")]
    [InlineData("https://example.test/a.png 1x, data:text/html;base64,PHNjcmlwdD4= 2x")]
    [InlineData("  JaVaScRiPt:alert(1) 1x")]
    public void Srcset_NeverKeepsAnUnsafeCandidate(string srcset)
    {
        var html = OmniHtmlSanitizer.Sanitize($"<img src=\"https://example.test/a.png\" srcset=\"{srcset}\">", ImagePolicy);

        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Srcset_OfSafeCandidates_IsKept()
    {
        const string markup = "<img src=\"https://example.test/a.png\" srcset=\"https://example.test/a.png 1x, /images/b.png 2x, c.png 3x\">";

        Assert.Equal(markup, OmniHtmlSanitizer.Sanitize(markup, ImagePolicy));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://example.test/p data:text/html,x")]
    [InlineData("https://example.test/p javascript:alert(1)")]
    public void Ping_NeverKeepsAnUnsafeAddress(string ping)
    {
        var html = OmniHtmlSanitizer.Sanitize($"<a href=\"https://example.test/\" ping=\"{ping}\">x</a>", ImagePolicy);

        Assert.DoesNotContain("javascript", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ping_OfSafeAddresses_IsKept()
    {
        const string markup = "<a href=\"https://example.test/\" ping=\"https://example.test/p /track\" rel=\"noopener noreferrer\">x</a>";

        Assert.Equal(markup, OmniHtmlSanitizer.Sanitize(markup, ImagePolicy));
    }

    [Theory]
    [InlineData("<table><tbody><tr><td background=\"javascript:alert(1)\">x</td></tr></tbody></table>", "<table><tbody><tr><td>x</td></tr></tbody></table>")]
    [InlineData("<table><tbody><tr><td background=\"data:text/html;base64,PHNjcmlwdD4=\">x</td></tr></tbody></table>", "<table><tbody><tr><td>x</td></tr></tbody></table>")]
    [InlineData("<table><tbody><tr><td background=\"https://example.test/b.png\">x</td></tr></tbody></table>", "<table><tbody><tr><td background=\"https://example.test/b.png\">x</td></tr></tbody></table>")]
    public void Background_IsHeldToTheAllowedSchemes(string input, string expected) =>
        Assert.Equal(expected, OmniHtmlSanitizer.Sanitize(input, ImagePolicy));

    [Theory]
    [InlineData("data")]
    [InlineData("manifest")]
    [InlineData("codebase")]
    [InlineData("icon")]
    [InlineData("lowsrc")]
    [InlineData("dynsrc")]
    public void EveryOtherAddressAttribute_IsHeldToTheAllowedSchemes(string attribute)
    {
        var policy = new OmniHtmlSanitizerPolicy { AdditionalAttributes = [attribute] };

        Assert.Equal("<p>x</p>", OmniHtmlSanitizer.Sanitize($"<p {attribute}=\"javascript:alert(1)\">x</p>", policy));
        Assert.Equal($"<p {attribute}=\"https://example.test/a\">x</p>", OmniHtmlSanitizer.Sanitize($"<p {attribute}=\"https://example.test/a\">x</p>", policy));
    }

    [Theory]
    [InlineData("xmp")]
    [InlineData("noembed")]
    [InlineData("noframes")]
    [InlineData("plaintext")]
    [InlineData("portal")]
    [InlineData("fencedframe")]
    [InlineData("isindex")]
    [InlineData("bgsound")]
    [InlineData("image")]
    [InlineData("keygen")]
    [InlineData("XMP")]
    public void DangerousElements_CanNeverBeAllowedByAPolicy(string tag)
    {
        var added = new OmniHtmlSanitizerPolicy { AdditionalTags = [tag] };
        var scoped = new OmniHtmlSanitizerPolicy
        {
            AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { [tag] = ["title"] }
        };

        Assert.Throws<ArgumentException>(() => OmniHtmlSanitizer.Validate(added));
        Assert.Throws<ArgumentException>(() => OmniHtmlSanitizer.Validate(scoped));
        // The built-in list never keeps them either: a plaintext element swallows the rest of the document.
        Assert.DoesNotContain($"<{tag}", OmniHtmlSanitizer.Sanitize($"<{tag}>x</{tag}><p>B</p>"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkAction_WithAScriptAddress_NeverReachesTheSurface()
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Mode = JSRuntimeMode.Loose;
        var value = "<p>Texte</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Link, "javascript:alert(1)")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Link, "  JaVaScRiPt:alert(1)")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Link, "data:text/html,x")));

        Assert.Empty(module.Invocations["exec"]);
    }

    [Theory]
    [InlineData("https://example.test/page")]
    [InlineData("mailto:a@example.test")]
    [InlineData("tel:+3212345678")]
    [InlineData("/relative/page")]
    public async Task LinkAction_WithASafeAddress_ReachesTheSurface(string address)
    {
        var module = JSInterop.SetupModule(OmniModules.HtmlEditor);
        module.Mode = JSRuntimeMode.Loose;
        var value = "<p>Texte</p>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        editor.WaitForAssertion(() => Assert.Single(module.Invocations["mount"]));

        await editor.InvokeAsync(() => editor.Instance.RunBuiltInAsync(OmniHtmlEditorAction.Link, address));

        var call = Assert.Single(module.Invocations["exec"]);
        Assert.Equal(address, call.Arguments[2]);
    }
}
