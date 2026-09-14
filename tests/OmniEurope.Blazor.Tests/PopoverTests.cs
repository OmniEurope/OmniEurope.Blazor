using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class PopoverTests : OmniBunitContext
{
    [Fact]
    public void ClosedPopover_RendersOnlyItsTrigger()
    {
        var popover = RenderPopover();

        var trigger = popover.Find("button");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal("dialog", trigger.GetAttribute("aria-haspopup"));
        Assert.Empty(popover.FindAll("[role='dialog']"));
        Assert.DoesNotContain("style=", popover.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Trigger_OpensANamedPanelItControls_AndTheSecondClickClosesIt()
    {
        var popover = RenderPopover();

        popover.Find("button").Click();

        var panel = popover.Find("[role='dialog']");
        Assert.Equal("Tâches en cours", panel.GetAttribute("aria-label"));
        Assert.Equal(panel.GetAttribute("id"), popover.Find("button").GetAttribute("aria-controls"));
        Assert.Equal("true", popover.Find("button").GetAttribute("aria-expanded"));
        Assert.Contains("Contenu du panneau", panel.TextContent, StringComparison.Ordinal);

        popover.Find("button").Click();
        Assert.Empty(popover.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task DismissRequest_ClosesThePanelAndReportsIt()
    {
        var reported = new List<bool>();
        var popover = RenderPopover(value => reported.Add(value));
        popover.Find("button").Click();

        await popover.InvokeAsync(() => popover.Instance.OnDismissRequestedAsync(fromKeyboard: true));

        Assert.Empty(popover.FindAll("[role='dialog']"));
        Assert.Equal([true, false], reported);
    }

    [Fact]
    public void BoundOpen_IsFollowedAndPlacementPicksTheAlignment()
    {
        var popover = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtre")
            .Add(component => component.Open, true)
            .Add(component => component.Placement, OmniPopoverPlacement.BottomStart)
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "Filtrer"))
            .Add(component => component.ChildContent, builder => builder.AddContent(0, "Options")));

        Assert.Single(popover.FindAll("[role='dialog']"));
        Assert.Contains("omni-popover--bottom-start", popover.Markup, StringComparison.Ordinal);
    }

    private IRenderedComponent<OmniPopover> RenderPopover(Action<bool>? onChanged = null) =>
        Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Tâches en cours")
            .Add(component => component.TriggerLabel, "Tâches en cours")
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "3"))
            .Add(component => component.ChildContent, builder => builder.AddContent(0, "Contenu du panneau"))
            .Add(component => component.OpenChanged, value => onChanged?.Invoke(value)));
}
