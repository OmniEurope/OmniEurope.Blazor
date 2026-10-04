using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DialogWidthTests : OmniBunitContext
{
    private BunitJSModuleInterop DialogModule() => JSInterop.SetupModule(Internal.OmniModules.Dialog);

    [Fact]
    public void DialogWithoutWidth_KeepsItsClasses_AndLoadsNoDialogScript()
    {
        var module = DialogModule();
        var dialog = RenderDialog(width: null, size: OmniDialogSize.Large);

        var panel = dialog.Find("section.omni-dialog");
        Assert.DoesNotContain("omni-dialog--width", panel.ClassList);
        Assert.Contains("omni-dialog--large", panel.ClassList);
        Assert.Empty(module.Invocations);
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Arguments.Contains(Internal.OmniModules.Dialog));
    }

    [Fact]
    public void Width_TakesThePlaceOfTheSize_AndIsWrittenByTheScriptNotAStyleAttribute()
    {
        var module = DialogModule();
        var dialog = RenderDialog(width: "30rem", size: OmniDialogSize.Large);

        var panel = dialog.Find("section.omni-dialog");
        Assert.Contains("omni-dialog--width", panel.ClassList);
        Assert.DoesNotContain("omni-dialog--large", panel.ClassList);
        Assert.Null(panel.GetAttribute("style"));
        dialog.WaitForAssertion(() => Assert.Single(module.Invocations, call => call.Identifier == "setWidth"));
        Assert.Equal("30rem", module.Invocations.Single(call => call.Identifier == "setWidth").Arguments[1]);
    }

    [Fact]
    public void ChangingTheWidth_WritesItAgain_AndRemovingIt_ClearsIt()
    {
        var module = DialogModule();
        var dialog = RenderDialog(width: "30rem", size: null);
        dialog.WaitForAssertion(() => Assert.Single(module.Invocations, call => call.Identifier == "setWidth"));

        dialog.Render(parameters => parameters.Add(component => component.Width, "500px"));
        dialog.WaitForAssertion(() => Assert.Equal(2, module.Invocations.Count(call => call.Identifier == "setWidth")));
        Assert.Equal("500px", module.Invocations.Last(call => call.Identifier == "setWidth").Arguments[1]);

        dialog.Render(parameters => parameters.Add(component => component.Width, (string?)null));
        dialog.WaitForAssertion(() => Assert.Single(module.Invocations, call => call.Identifier == "clearWidth"));
        Assert.DoesNotContain("omni-dialog--width", dialog.Find("section.omni-dialog").ClassList);
    }

    [Theory]
    [InlineData("30")]
    [InlineData("30 rem")]
    [InlineData("30REM")]
    [InlineData("-5px")]
    [InlineData("0px")]
    [InlineData("")]
    [InlineData("calc(10px + 2rem)")]
    [InlineData("30rem;color:red")]
    [InlineData("30rem; --x: url(https://example.org)")]
    [InlineData("var(--omni-x)")]
    [InlineData("100vh")]
    public void Width_RefusesAnythingButANumberAndAUnit(string width)
    {
        Assert.Throws<ArgumentException>(() => RenderDialog(width, size: null));
    }

    [Theory]
    [InlineData("500px")]
    [InlineData("30rem")]
    [InlineData("42.5em")]
    [InlineData("60ch")]
    [InlineData("80vw")]
    [InlineData("90%")]
    [InlineData("0.5rem")]
    public void Width_AcceptsALengthInAnAllowedUnit(string width)
    {
        DialogModule();
        var dialog = RenderDialog(width, size: null);

        Assert.Contains("omni-dialog--width", dialog.Find("section.omni-dialog").ClassList);
    }

    [Fact]
    public void RequestWidth_ReachesTheDialogRenderedByTheHost()
    {
        var module = DialogModule();
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Client", Content("Fiche")) { Width = "30rem", CloseOnBackdrop = true });

        host.WaitForAssertion(() => Assert.Contains("omni-dialog--width", host.Find("section.omni-dialog").ClassList));
        host.WaitForAssertion(() => Assert.Equal("30rem", module.Invocations.Single(call => call.Identifier == "setWidth").Arguments[1]));
    }

    [Fact]
    public void RequestWithAnInvalidWidth_IsRefusedBeforeAnythingOpens()
    {
        using var service = new OmniOverlayService();

        Assert.Throws<ArgumentException>(() => service.OpenDialog(new OmniDialogRequest("Client", Content("Fiche")) { Width = "30rem;x" }));
        // Thrown at the call itself, not through the returned task: no caller is left waiting.
        Assert.Throws<ArgumentException>(() => { _ = service.OpenDialogAsync(new OmniDialogRequest("Client", Content("Fiche")) { Width = "wide" }); });
        Assert.Null(service.Dialog);
    }

    [Fact]
    public async Task ForComponent_OpensTheComponentWithItsParameters_AtTheRequestedWidth()
    {
        var module = DialogModule();
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        var request = OmniDialogRequest.ForComponent<OmniText>(
            "Honoraire",
            new Dictionary<string, object?>
            {
                [nameof(OmniText.ChildContent)] = (RenderFragment)(content => content.AddContent(0, "Montant")),
                [nameof(OmniText.Class)] = "width-probe",
            }) with
        { Width = "30rem", CloseOnBackdrop = false };
        var pending = service.OpenDialogAsync(request);

        host.WaitForAssertion(() => Assert.Equal("Montant", host.Find(".width-probe").TextContent));
        Assert.Contains("omni-dialog--width", host.Find("section.omni-dialog").ClassList);
        Assert.False(service.Dialog!.CloseOnBackdrop);
        // No close label given: the localized default names the close button.
        Assert.Equal("Fermer", host.Find(".omni-dialog__close").GetAttribute("aria-label"));
        host.WaitForAssertion(() => Assert.Single(module.Invocations, call => call.Identifier == "setWidth"));

        service.CloseDialog("enregistré");

        Assert.Equal("enregistré", await pending);
    }

    [Fact]
    public void Stylesheet_CapsTheFreeWidthByTheViewport_AndEndsTheWaitWithoutScript()
    {
        var css = StylesheetSource.Read();

        Assert.Contains(".omni-dialog--width { max-width: min(var(--omni-dialog-width, 40rem), calc(100vw - 2rem)); }", css, StringComparison.Ordinal);
        Assert.Contains(".omni-dialog--width:not([data-omni-dialog-width-ready]) { animation: omni-dialog-width-wait 1s step-end; }", css, StringComparison.Ordinal);
        Assert.Contains("@keyframes omni-dialog-width-wait { from { opacity: 0; } }", css, StringComparison.Ordinal);
    }

    private IRenderedComponent<OmniDialog> RenderDialog(string? width, OmniDialogSize? size) => Render<OmniDialog>(parameters =>
    {
        parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Dossier")
            .AddChildContent("Contenu");
        if (width is not null)
        {
            parameters.Add(component => component.Width, width);
        }

        if (size is { } value)
        {
            parameters.Add(component => component.Size, value);
        }
    });

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);
}
