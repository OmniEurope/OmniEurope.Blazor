using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>How <see cref="OmniCodeBlock"/> reads the ANSI sequences of a command output, on screen and in the copy.</summary>
public sealed class CodeBlockAnsiTests : OmniBunitContext
{
    private const string InteropModulePath = Internal.OmniModules.Interop;
    private const string Output = "\u001b[32m●\u001b[0m nginx.service \u001b[1mactive\u001b[0m";
    private const string Plain = "● nginx.service active";

    [Fact]
    public void Render_IsTheDefault_DrawsTheColours_AndShowsNoEscapeCharacter()
    {
        var block = Render<OmniCodeBlock>(parameters => parameters.Add(component => component.Code, Output));

        Assert.Equal(OmniAnsiMode.Render, block.Instance.AnsiMode);
        Assert.Equal("●", block.Find("pre code .omni-ansi-fg-2").TextContent);
        Assert.Equal("active", block.Find("pre code .omni-ansi-bold").TextContent);
        Assert.Equal(Plain, block.Find("pre code").TextContent);
        Assert.DoesNotContain('\u001b', block.Find("pre code").TextContent);
        Assert.DoesNotContain("[32m", block.Find("pre code").TextContent);
    }

    [Fact]
    public void Hide_RemovesTheSequences_AndDrawsNothing()
    {
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, Output)
            .Add(component => component.AnsiMode, OmniAnsiMode.Hide));

        Assert.Equal(Plain, block.Find("pre code").TextContent);
        Assert.Empty(block.FindAll("pre code span"));
    }

    [Fact]
    public void Raw_KeepsTheCodeAsWritten()
    {
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, Output)
            .Add(component => component.AnsiMode, OmniAnsiMode.Raw));

        Assert.Equal(Output, block.Find("pre code").TextContent);
        Assert.Empty(block.FindAll("pre code span"));
    }

    [Theory]
    [InlineData(OmniAnsiMode.Render)]
    [InlineData(OmniAnsiMode.Hide)]
    [InlineData(OmniAnsiMode.Raw)]
    public void CodeWithoutSequence_IsUnchanged_InEveryMode(OmniAnsiMode mode)
    {
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "dotnet test <all>")
            .Add(component => component.AnsiMode, mode));

        Assert.Equal("dotnet test <all>", block.Find("pre code").TextContent);
    }

    [Theory]
    [InlineData(OmniAnsiMode.Render, Plain)]
    [InlineData(OmniAnsiMode.Hide, Plain)]
    [InlineData(OmniAnsiMode.Raw, Output)]
    public void Copy_TakesTheCodeWithoutSequences_ExceptInRaw(OmniAnsiMode mode, string copied)
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, Output)
            .Add(component => component.AnsiMode, mode));

        block.Find(".omni-code-block__copy").Click();

        Assert.Equal(copied, Assert.Single(module.Invocations["copyText"]).Arguments[0]);
    }

    [Theory]
    [InlineData(OmniAnsiMode.Render)]
    [InlineData(OmniAnsiMode.Hide)]
    public void Secret_StaysMasked_ShowsNoSequence_AndCopiesTheFullValueWithoutThem(OmniAnsiMode mode)
    {
        var module = JSInterop.SetupModule(InteropModulePath);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "\u001b[31msk-1234567890abcdef\u001b[0m")
            .Add(component => component.Secret, true)
            .Add(component => component.AnsiMode, mode));

        Assert.Equal("sk-1••••••••cdef", block.Find("pre code").TextContent);
        Assert.Contains("omni-code-block__masked", block.Find("pre code").ClassList);

        block.Find(".omni-code-block__copy").Click();

        Assert.Equal("sk-1234567890abcdef", Assert.Single(module.Invocations["copyText"]).Arguments[0]);

        block.Find(".omni-code-block__reveal").Click();

        Assert.Equal("sk-1234567890abcdef", block.Find("pre code").TextContent);
    }

    [Fact]
    public void InlineAndWrap_KeepWorking_WithAnsiContent()
    {
        var block = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, Output)
            .Add(component => component.Inline, true)
            .Add(component => component.Wrap, true));

        Assert.Contains("omni-code-block--inline", block.Find("figure").ClassList);
        Assert.Contains("omni-code-block--wrap", block.Find("figure").ClassList);
        Assert.Equal("●", block.Find("pre code .omni-ansi-fg-2").TextContent);
    }
}
