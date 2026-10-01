using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The copy button shared by <see cref="OmniCodeBlock"/> and <see cref="OmniCodeViewer"/>: the same
/// clipboard call, the same words on the button and in the live region, the same callback.
/// </summary>
public sealed class CodeCopyTests : OmniBunitContext
{
    private const string InteropModulePath = Internal.OmniModules.Interop;

    // The bound member a text box names in its ValueExpression.
    private readonly string _cloneUrl = string.Empty;

    [Theory]
    [InlineData(true, "Copié dans le presse-papiers")]
    [InlineData(false, "La copie a échoué")]
    public void CopyableTextBox_CopiesItsValue_KeepsItsWord_AndSaysHowItWent(bool accepted, string outcome)
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetResult(accepted);
        bool? reported = null;
        var field = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Id, "clone-url")
            .Add(component => component.Value, "https://git.example/repo.git")
            .Add(component => component.ValueExpression, () => _cloneUrl)
            .Add(component => component.ReadOnly, true)
            .Add(component => component.Copyable, true)
            .Add(component => component.OnCopy, copied => reported = copied));

        // The id stays on the input, the button is welded inside the same wrapper.
        Assert.Equal("clone-url", field.Find(".omni-text-box-field--copy > input").Id);
        Assert.Equal("Copier", field.Find(".omni-text-box-field__copy").TextContent.Trim());

        field.Find(".omni-text-box-field__copy").Click();

        Assert.Equal("https://git.example/repo.git", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
        Assert.Equal(accepted, reported);
        // The button keeps its word (same size); the live region says the outcome.
        Assert.Equal("Copier", field.Find(".omni-text-box-field__copy").TextContent.Trim());
        Assert.Equal(outcome, field.Find("[role=status]").TextContent);
    }

    [Fact]
    public void CopyableTextBox_IsDisabledWhileEmpty_AndPlainTextBoxIsUnchanged()
    {
        var empty = Render<OmniTextBox>(parameters => parameters.Add(component => component.Copyable, true).Add(component => component.ValueExpression, () => _cloneUrl));
        Assert.True(empty.Find(".omni-text-box-field__copy").HasAttribute("disabled"));

        var plain = Render<OmniTextBox>(parameters => parameters.Add(component => component.Value, "x").Add(component => component.ValueExpression, () => _cloneUrl));
        Assert.Empty(plain.FindAll(".omni-text-box-field"));
        Assert.Equal("INPUT", plain.Find(".omni-text-box").TagName);
    }

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
            .Add(component => component.OnCopy, copied => reported = copied));

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
            .Add(component => component.OnCopy, copied => reported = copied));

        Assert.Equal("Copier", viewer.Find(".omni-code-viewer__copy").TextContent.Trim());
        Assert.Equal(string.Empty, viewer.Find("[role=status]").TextContent);

        viewer.Find(".omni-code-viewer__copy").Click();

        Assert.Equal("var answer = 42;", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
        Assert.Equal(accepted, reported);
        Assert.Equal(outcome, viewer.Find(".omni-code-viewer__copy").TextContent.Trim());
        Assert.Equal(outcome, viewer.Find("[role=status]").TextContent);
    }
}
