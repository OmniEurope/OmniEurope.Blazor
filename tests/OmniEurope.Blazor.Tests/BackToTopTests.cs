using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The back-to-top button (a request of the Astranya application): a real, named button in a fixed frame that its
/// script shows past a scroll threshold, which asks the script to bring the content back to its top, and which
/// survives prerendering and a lost circuit. What the script does with the scroll and the focus is proved by
/// <c>tests/js/back-to-top.test.mjs</c>, and in a browser on the showcase page.
/// </summary>
public sealed class BackToTopTests : OmniBunitContext
{
    [Fact]
    public void ItIsARealButton_NamedByTheLocalizedDefault_InAFixedFrame()
    {
        JSInterop.SetupModule(Internal.OmniModules.BackToTop).SetupVoid("attach", _ => true);

        var component = Render<OmniBackToTop>();

        var frame = component.Find("div.omni-back-to-top");
        var button = frame.QuerySelector("button")!;
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("Retour en haut", button.GetAttribute("aria-label"));
        Assert.Contains("omni-button--primary", button.ClassList);
        Assert.Contains("omni-button--icon", button.ClassList);
        Assert.Null(button.GetAttribute("tabindex"));
        Assert.NotNull(button.QuerySelector("svg"));
        // The tooltip repeats the name for the pointer.
        Assert.Equal("Retour en haut", frame.QuerySelector("[role=tooltip]")!.TextContent);
    }

    [Theory]
    [InlineData("en-US", "Back to top")]
    [InlineData("de-DE", "Nach oben")]
    [InlineData("pl-PL", "Powrót na górę")]
    public void TheDefaultLabel_FollowsTheUiCulture(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var component = Render<OmniBackToTop>();

            Assert.Equal(expected, component.Find("button").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void TheLabel_ReplacesTheDefault_AndABlankOneKeepsIt()
    {
        var named = Render<OmniBackToTop>(parameters => parameters.Add(component => component.Label, "Haut de l'article"));
        Assert.Equal("Haut de l'article", named.Find("button").GetAttribute("aria-label"));
        Assert.Equal("Haut de l'article", named.Find("[role=tooltip]").TextContent);

        var blank = Render<OmniBackToTop>(parameters => parameters.Add(component => component.Label, "  "));
        Assert.Equal("Retour en haut", blank.Find("button").GetAttribute("aria-label"));
    }

    [Fact]
    public void IdAndAttributes_GoOnTheButton_AndClassOnTheFrame()
    {
        var component = Render<OmniBackToTop>(parameters => parameters
            .Add(component => component.Id, "top")
            .Add(component => component.Class, "app-top")
            .AddUnmatched("data-test", "back"));

        var frame = component.Find(".omni-back-to-top");
        Assert.Contains("app-top", frame.ClassList);
        var button = frame.QuerySelector("button")!;
        Assert.Equal("top", button.Id);
        Assert.Equal("back", button.GetAttribute("data-test"));
    }

    [Fact]
    public void ItIsRenderedHidden_TheScriptAloneShowsItPastTheThreshold()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.BackToTop);
        module.SetupVoid("attach", _ => true);

        var component = Render<OmniBackToTop>();

        // Hidden until the script marks the frame: the attribute is never rendered by the component.
        Assert.False(component.Find(".omni-back-to-top").HasAttribute("data-shown"));
        var attach = Assert.Single(module.Invocations["attach"]);
        Assert.Null(attach.Arguments[1]);
        Assert.Equal(400, attach.Arguments[2]);
    }

    [Fact]
    public void ANewTargetOrThreshold_IsPassedToTheScript_AnUnchangedRenderIsNot()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.BackToTop);
        module.SetupVoid("attach", _ => true);
        var component = Render<OmniBackToTop>(parameters => parameters.Add(component => component.Threshold, 100));

        component.Render(parameters => parameters.Add(component => component.Label, "Haut"));
        Assert.Single(module.Invocations["attach"]);

        component.Render(parameters => parameters
            .Add(component => component.Threshold, 0)
            .Add(component => component.TargetId, "article"));

        Assert.Equal(2, module.Invocations["attach"].Count);
        var latest = module.Invocations["attach"][1];
        Assert.Equal("article", latest.Arguments[1]);
        Assert.Equal(0, latest.Arguments[2]);
    }

    [Fact]
    public void ANegativeThreshold_IsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Render<OmniBackToTop>(parameters => parameters.Add(component => component.Threshold, -1)));

    [Fact]
    public async Task AClick_AsksTheScriptToScrollTheContentToItsTop()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.BackToTop);
        module.SetupVoid("attach", _ => true);
        module.Setup<bool>("scrollToTop", _ => true).SetResult(true);
        var component = Render<OmniBackToTop>();

        await component.Find("button").ClickAsync(new());

        var call = Assert.Single(module.Invocations["scrollToTop"]);
        Assert.IsType<ElementReference>(call.Arguments[0]);
    }

    [Fact]
    public async Task ALostCircuit_FailsNeitherTheAttachment_NorTheClick_NorTheDisposal()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.BackToTop);
        var lost = new JSDisconnectedException("circuit perdu");
        module.SetupVoid("attach", _ => true).SetException(lost);
        module.Setup<bool>("scrollToTop", _ => true).SetException(lost);
        module.SetupVoid("detach", _ => true).SetException(lost);
        var component = Render<OmniBackToTop>();

        await component.Find("button").ClickAsync(new());
        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["attach"]);
        Assert.Single(module.Invocations["scrollToTop"]);
        Assert.Single(module.Invocations["detach"]);
    }

    [Fact]
    public async Task AModuleThatCannotLoad_LeavesTheButtonHidden_AndItsClickDoesNothing()
    {
        var runtime = new LostCircuitRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var component = Render<OmniBackToTop>();

        await component.Find("button").ClickAsync(new());
        Assert.False(component.Find(".omni-back-to-top").HasAttribute("data-shown"));
        await DisposeComponentsAsync();

        // The import was tried and failed: render, click and disposal went through without throwing.
        Assert.Contains("import", runtime.Calls);
    }

    [Fact]
    public void UnderPrerendering_TheMarkupIsTheHiddenFrame_WithItsNamedButton()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var component = Render<OmniBackToTop>();

        var frame = component.Find(".omni-back-to-top");
        Assert.False(frame.HasAttribute("data-shown"));
        Assert.Equal("Retour en haut", frame.QuerySelector("button")!.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Disposal_DetachesTheScript()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.BackToTop);
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("detach", _ => true);
        Render<OmniBackToTop>();

        await DisposeComponentsAsync();

        Assert.Single(module.Invocations["detach"]);
    }

    /// <summary>A runtime whose circuit is gone: every call, the module import first, throws.</summary>
    private sealed class LostCircuitRuntime : IJSRuntime
    {
        internal List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add(identifier);
            throw new JSDisconnectedException("circuit perdu");
        }
    }
}
