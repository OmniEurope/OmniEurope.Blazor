using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The copy button shared by <see cref="OmniCodeBlock"/> and <see cref="OmniCodeViewer"/>: the same
/// clipboard call, the same words on the button and in the live region, the same callback.
/// </summary>
public sealed class CodeCopyTests : OmniBunitContext
{
    private const string InteropModulePath = "./_content/OmniEurope.Blazor/omniInterop.js";

    [Theory]
    [InlineData(true, "Copié dans le presse-papiers")]
    [InlineData(false, "La copie a échoué")]
    public void CodeBlock_CopiesTheFullSecret_AndSaysHowItWent(bool accepted, string outcome)
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetResult(accepted);
        bool? reported = null;
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "sk-1234567890abcdef")
            .Add(component => component.Secret, true)
            .Add(component => component.OnCopied, copied => reported = copied));

        Assert.Equal("Copier", block.Find(".omni-code-block__copy").GetAttribute("aria-label"));
        Assert.Equal(string.Empty, block.Find("[role=status]").TextContent);

        block.Find(".omni-code-block__copy").Click();

        Assert.Equal("sk-1234567890abcdef", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
        Assert.Equal(accepted, reported);
        Assert.Equal(outcome, block.Find(".omni-code-block__copy").GetAttribute("aria-label"));
        Assert.Equal(outcome, block.Find("[role=status]").TextContent);
    }

    [Theory]
    [InlineData(true, "Copié dans le presse-papiers")]
    [InlineData(false, "La copie a échoué")]
    public void CodeViewer_CopiesTheCode_SaysHowItWent_AndReportsIt(bool accepted, string outcome)
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetResult(accepted);
        bool? reported = null;
        var viewer = Render<OmniCodeViewer>(parameters => parameters
            .Add(component => component.Code, "var answer = 42;")
            .Add(component => component.OnCopied, copied => reported = copied));

        Assert.Equal("Copier", viewer.Find(".omni-code-viewer__copy").TextContent.Trim());
        Assert.Equal(string.Empty, viewer.Find("[role=status]").TextContent);

        viewer.Find(".omni-code-viewer__copy").Click();

        Assert.Equal("var answer = 42;", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
        Assert.Equal(accepted, reported);
        Assert.Equal(outcome, viewer.Find(".omni-code-viewer__copy").TextContent.Trim());
        Assert.Equal(outcome, viewer.Find("[role=status]").TextContent);
    }
}
