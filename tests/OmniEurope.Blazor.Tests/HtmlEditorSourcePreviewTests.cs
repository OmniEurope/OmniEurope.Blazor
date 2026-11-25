using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The source face of <see cref="OmniHtmlEditor"/> on a long value. The preview under the text area shows the sanitised
/// value and sanitises it again only when the value or the policy changes (every render reads it, and a host that
/// re-renders often never caught up on a document of hundreds of kilobytes). The input events of a burst (a paste, a
/// write of several lines) are kept from the page by the script and committed once, so the editor does not parse the
/// text so far once per line.
/// </summary>
public sealed class HtmlEditorSourcePreviewTests : OmniBunitContext
{
    private static readonly OmniHtmlSanitizerPolicy NotePolicy = new() { AdditionalTags = ["aside"] };

    [Fact]
    public void Preview_ReadAgainWithTheSameValueAndPolicy_IsNotSanitisedAgain()
    {
        var calls = 0;
        var preview = new HtmlEditorPreview((value, _) => { calls++; return "clean:" + value; });

        Assert.Equal("clean:<p>a</p>", preview.Of("<p>a</p>", null, isSanitized: false));
        Assert.Equal("clean:<p>a</p>", preview.Of("<p>a</p>", null, isSanitized: false));
        Assert.Equal("clean:<p>a</p>", preview.Of("<p>a</p>", null, isSanitized: false));

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Preview_WithANewValueOrANewPolicy_IsSanitisedAgain()
    {
        var calls = 0;
        var preview = new HtmlEditorPreview((value, _) => { calls++; return "clean:" + value; });

        preview.Of("<p>a</p>", null, isSanitized: false);
        Assert.Equal("clean:<p>b</p>", preview.Of("<p>b</p>", null, isSanitized: false));
        Assert.Equal("clean:<p>b</p>", preview.Of("<p>b</p>", NotePolicy, isSanitized: false));

        Assert.Equal(3, calls);
    }

    [Fact]
    public void Preview_OfAValueTheEditorSanitisedItself_IsShownAsIsWithoutSanitising()
    {
        var calls = 0;
        var preview = new HtmlEditorPreview((value, _) => { calls++; return "clean:" + value; });

        Assert.Equal("<p>a</p>", preview.Of("<p>a</p>", null, isSanitized: true));
        Assert.Equal(string.Empty, preview.Of(null, null, isSanitized: true));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void SourceFace_RenderedAgainAndAgain_SanitisesOnlyWhatIsTyped()
    {
        // Its own policy instance gets its own sanitiser: every value it sanitises raises PostProcessDom once, whatever
        // else the other tests do at the same time.
        var policy = new OmniHtmlSanitizerPolicy { AdditionalTags = ["aside"] };
        var runs = 0;
        OmniHtmlSanitizer.For(policy).PostProcessDom += (_, _) => Interlocked.Increment(ref runs);
        var value = "<p>a</p><script>alert(1)</script>";
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .BindValue(() => value, updated => value = updated)
            .Add(component => component.Extensions, [new PolicyExtension(policy)])
            .Add(component => component.Mode, OmniHtmlEditorMode.Source));
        var afterFirstRender = Volatile.Read(ref runs);

        editor.Render();
        editor.Render();
        editor.Render();

        Assert.Equal(afterFirstRender, Volatile.Read(ref runs));

        editor.Find("textarea").Input("<p>b</p><iframe src=\"x\"></iframe>");
        var afterInput = Volatile.Read(ref runs);

        editor.Render();
        editor.Render();

        Assert.Equal(afterFirstRender + 1, afterInput);
        Assert.Equal(afterInput, Volatile.Read(ref runs));
    }

    [Fact]
    public void SourceFace_ShowsTheSanitisedValueInThePreview_AndFollowsWhatIsTyped()
    {
        var value = "<p>a</p><script>alert(1)</script>";
        var editor = RenderSourceFace(value, updated => value = updated);

        Assert.Equal("<p>a</p>", editor.Find(".omni-html-editor__preview").InnerHtml);

        editor.Find("textarea").Input("<p>b</p><iframe src=\"x\"></iframe>");

        Assert.Equal("<p>b</p>", value);
        Assert.Equal("<p>b</p>", editor.Find(".omni-html-editor__preview").InnerHtml);

        editor.Render();

        Assert.Equal("<p>b</p>", editor.Find(".omni-html-editor__preview").InnerHtml);
    }

    [Fact]
    public void SourceFace_WhenThePolicyChanges_ShowsThePreviewUnderTheNewPolicy()
    {
        var value = "<p>a</p><aside>note</aside>";
        var editor = RenderSourceFace(value, updated => value = updated);

        Assert.DoesNotContain("aside", editor.Find(".omni-html-editor__preview").InnerHtml, StringComparison.Ordinal);

        editor.Render(parameters => parameters
            .BindValue(() => value, updated => value = updated)
            .Add(component => component.Mode, OmniHtmlEditorMode.Source)
            .Add(component => component.Extensions, [new PolicyExtension(NotePolicy)]));

        Assert.Contains("<aside>note</aside>", editor.Find(".omni-html-editor__preview").InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceFace_AsksTheScriptToKeepBurstsOfInputFromThePage()
    {
        var module = JSInterop.SetupModule(OmniModules.Interop);
        var value = "<p>a</p>";
        RenderSourceFace(value, updated => value = updated);

        var call = Assert.Single(module.Invocations["holdInputBursts"]);

        Assert.Equal(150, call.Arguments[1]);
    }

    [Fact]
    public async Task SourceFace_WhenAnActionAsksForTheValue_CommitsTheTextTheScriptHeld_Sanitised()
    {
        var module = JSInterop.SetupModule(OmniModules.Interop);
        module.Setup<string?>("takeHeldInput", _ => true).SetResult("<p>held</p><script>alert(1)</script>");
        var value = "<p>a</p>";
        var editor = RenderSourceFace(value, updated => value = updated);

        var captured = await editor.InvokeAsync(() => editor.Instance.CaptureAsync());

        Assert.Equal("<p>held</p>", captured);
        Assert.Equal("<p>held</p>", value);
    }

    [Fact]
    public async Task SourceFace_WhenTheScriptHeldNothing_LeavesTheValueAlone()
    {
        JSInterop.SetupModule(OmniModules.Interop).Setup<string?>("takeHeldInput", _ => true).SetResult(null);
        var value = "<p>a</p>";
        var editor = RenderSourceFace(value, updated => value = updated);

        Assert.Equal("<p>a</p>", await editor.InvokeAsync(() => editor.Instance.CaptureAsync()));
    }

    /// <summary>Renders an editor on its source face holding <paramref name="value"/>, each change given to <paramref name="changed"/>.</summary>
    private IRenderedComponent<OmniHtmlEditor> RenderSourceFace(string value, Action<string> changed) =>
        this.RenderHtmlEditor(value, parameters => parameters.Add(component => component.Mode, OmniHtmlEditorMode.Source), changed);
}
