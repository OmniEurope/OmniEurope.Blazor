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
        var css = File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));
        foreach (var modifier in SizeModifiers)
        {
            Assert.Contains($".{modifier} {{", css, StringComparison.Ordinal);
        }

        Assert.Equal(5, Enum.GetValues<OmniDialogSize>().Length);
        Assert.Equal(OmniDialogSize.Medium, default(OmniDialogSize));
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
