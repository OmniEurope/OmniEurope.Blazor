using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DialogSizeTests : OmniBunitContext
{
    private static readonly string[] SizeModifiers =
        ["omni-dialog--small", "omni-dialog--large", "omni-dialog--xlarge", "omni-dialog--full"];

    [Fact]
    public void DialogWithoutSize_RendersNoSizeModifier()
    {
        var dialog = RenderDialog(size: null);

        var panel = dialog.Find("section.omni-dialog");
        Assert.DoesNotContain(panel.ClassList, SizeModifiers.Contains);
        Assert.Null(panel.GetAttribute("style"));
    }

    [Theory]
    [InlineData(OmniDialogSize.Medium, null)]
    [InlineData(OmniDialogSize.Small, "omni-dialog--small")]
    [InlineData(OmniDialogSize.Large, "omni-dialog--large")]
    [InlineData(OmniDialogSize.ExtraLarge, "omni-dialog--xlarge")]
    [InlineData(OmniDialogSize.FullWidth, "omni-dialog--full")]
    public void EachSize_RendersItsOwnModifierOnly(OmniDialogSize size, string? expected)
    {
        var dialog = RenderDialog(size);

        var panel = dialog.Find("section.omni-dialog");
        var modifiers = panel.ClassList.Where(SizeModifiers.Contains).ToList();
        if (expected is null)
        {
            Assert.Empty(modifiers);
        }
        else
        {
            Assert.Equal([expected], modifiers);
        }

        Assert.Null(panel.GetAttribute("style"));
    }

    [Fact]
    public void EverySizeValue_HasAStylesheetRuleOrIsTheBaseWidth()
    {
        var css = StylesheetSource.Read();
        foreach (var modifier in SizeModifiers)
        {
            Assert.Contains($".{modifier} {{", css, StringComparison.Ordinal);
        }

        Assert.Equal(5, Enum.GetValues<OmniDialogSize>().Length);
        Assert.Equal(OmniDialogSize.Medium, default(OmniDialogSize));
    }

    [Theory]
    [InlineData(".omni-dialog", "2rem")]
    [InlineData(".omni-dialog--full", "1rem")]
    public void EverySize_IsCappedByTheDynamicWindowHeight_AfterAStaticFallback(string selector, string margin)
    {
        // Client application acceptance review R-60: on a phone 100vh counts the room under the address
        // bar, and a centred dialog lost its title and its buttons past both edges.
        var body = ShippedLookTests.Body(selector);
        Assert.Contains($"max-height: calc(100vh - {margin}); max-height: calc(100dvh - {margin})", body, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyTheContentScrolls_TheHeaderAndTheFooterStayWhole()
    {
        var dialog = ShippedLookTests.Body(".omni-dialog");
        Assert.Equal("flex", ShippedLookTests.Value(dialog, "display"));
        Assert.Equal("column", ShippedLookTests.Value(dialog, "flex-direction"));
        Assert.Equal("hidden", ShippedLookTests.Value(dialog, "overflow"));

        var content = ShippedLookTests.Body(".omni-dialog__content");
        Assert.Contains("flex: 1 1 auto", content, StringComparison.Ordinal);
        Assert.Equal("0", ShippedLookTests.Value(content, "min-block-size"));
        Assert.Equal("auto", ShippedLookTests.Value(content, "overflow"));

        var bands = Assert.Single(ShippedLookTests.Rules(), rule => rule.Selector.Contains(".omni-dialog__header", StringComparison.Ordinal) && rule.Selector.Contains(".omni-dialog__footer", StringComparison.Ordinal));
        Assert.Equal("none", ShippedLookTests.Value(bands.Body, "flex"));
    }

    [Fact]
    public void RequestSize_ReachesTheDialogRenderedByTheHost()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Comparer", Content("Deux versions")) { Size = OmniDialogSize.ExtraLarge });

        host.WaitForAssertion(() => Assert.Contains("omni-dialog--xlarge", host.Find("section.omni-dialog").ClassList));
    }

    [Fact]
    public void TwoEqualRequests_OpenTwoDialogs_WithoutADuplicateKey()
    {
        // Requests are records: the same dialog opened twice (a button pressed twice) gives two equal
        // values, which must still be two dialogs, not a duplicate render key that breaks the host.
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));
        var content = Content("Deux fois");

        service.OpenDialog(new OmniDialogRequest("Même dialogue", content));
        service.OpenDialog(new OmniDialogRequest("Même dialogue", content));

        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("section.omni-dialog").Count));
        service.CloseDialog();
        host.WaitForAssertion(() => Assert.Single(host.FindAll("section.omni-dialog")));
    }

    [Fact]
    public void RequestWithoutSize_RendersTheHostDialogAtTheBaseWidth()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Confirmer", Content("Continuer ?")));

        host.WaitForAssertion(() => Assert.NotNull(host.Find("section.omni-dialog")));
        Assert.DoesNotContain(host.Find("section.omni-dialog").ClassList, SizeModifiers.Contains);
        Assert.Equal(OmniDialogSize.Medium, service.Dialog!.Size);
    }

    [Fact]
    public async Task OpenDialogAsyncOfComponentWithSize_PassesTheSizeAndTheParameters_AndAnswersItsResult()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        var pending = service.OpenDialogAsync<OmniText>(
            "Journal",
            new Dictionary<string, object?>
            {
                [nameof(OmniText.ChildContent)] = (RenderFragment)(content => content.AddContent(0, "Toutes les entrées")),
                [nameof(OmniText.Class)] = "size-probe",
            },
            OmniDialogSize.FullWidth,
            "Fermer le journal");

        host.WaitForAssertion(() => Assert.Equal("Toutes les entrées", host.Find(".size-probe").TextContent));
        Assert.Contains("omni-dialog--full", host.Find("section.omni-dialog").ClassList);
        Assert.Equal("Fermer le journal", host.Find(".omni-dialog__close").GetAttribute("aria-label"));
        Assert.Equal(OmniDialogSize.FullWidth, service.Dialog!.Size);

        service.CloseDialog("lu");

        Assert.Equal("lu", await pending);
    }

    [Fact]
    public void OpenDialogAsyncOfComponentWithoutSize_StaysAtTheBaseWidth()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        _ = service.OpenDialogAsync<OmniText>("Renommer");

        host.WaitForAssertion(() => Assert.NotNull(host.Find("section.omni-dialog")));
        Assert.DoesNotContain(host.Find("section.omni-dialog").ClassList, SizeModifiers.Contains);
        Assert.Equal(OmniDialogSize.Medium, service.Dialog!.Size);
    }

    private IRenderedComponent<OmniDialog> RenderDialog(OmniDialogSize? size) => Render<OmniDialog>(parameters =>
    {
        parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Dossier")
            .AddChildContent("Contenu");
        if (size is { } value)
        {
            parameters.Add(component => component.Size, value);
        }
    });

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);
}
