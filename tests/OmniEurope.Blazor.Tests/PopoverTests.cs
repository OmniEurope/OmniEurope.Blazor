using Bunit;
using Microsoft.AspNetCore.Components;
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
        // Nothing to point at while closed: aria-controls only names a panel that is there.
        Assert.False(trigger.HasAttribute("aria-controls"));
        Assert.DoesNotContain("style=", popover.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PopupLabel_NamesThePanel_ElseTheLabel_ElseTheTrigger()
    {
        var named = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Tâches")
            .Add(component => component.PopupLabel, "Tâches en cours")
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "3")));
        Assert.Equal("Tâches en cours", named.Find("[role='dialog']").GetAttribute("aria-label"));
        Assert.Equal("Tâches", named.Find("button").GetAttribute("aria-label"));

        var bare = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Id, "filtre")
            .Add(component => component.Open, true)
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "Filtrer")));
        var panel = bare.Find("[role='dialog']");
        Assert.False(panel.HasAttribute("aria-label"));
        Assert.Equal("filtre", panel.GetAttribute("aria-labelledby"));
        Assert.Equal("filtre", bare.Find("button").GetAttribute("id"));
    }

    [Fact]
    public void DisabledTrigger_OpensNothing_AndTakesTheButtonSize()
    {
        var popover = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtre")
            .Add(component => component.Disabled, true)
            .Add(component => component.Size, OmniControlSize.Small)
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "Filtrer")));

        var trigger = popover.Find("button");
        Assert.True(trigger.HasAttribute("disabled"));
        Assert.Contains("omni-button--small", trigger.ClassList);
        trigger.Click();
        Assert.Empty(popover.FindAll("[role='dialog']"));
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
            .Add(component => component.PopupLabel, "Filtre")
            .Add(component => component.Open, true)
            .Add(component => component.Placement, OmniPopoverPlacement.BottomStart)
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "Filtrer"))
            .Add(component => component.ChildContent, builder => builder.AddContent(0, "Options")));

        Assert.Single(popover.FindAll("[role='dialog']"));
        Assert.Contains("omni-popover--bottom-start", popover.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TriggerTitle_DefaultsToTheTriggerLabel_AndAnExplicitTitleWins()
    {
        var popover = RenderPopover();
        Assert.Equal("Tâches en cours", popover.Find("button").GetAttribute("title"));

        popover.Render(parameters => parameters.Add(component => component.TriggerTitle, "Voir les tâches"));
        Assert.Equal("Voir les tâches", popover.Find("button").GetAttribute("title"));
    }

    [Fact]
    public void TriggerIcon_IsTheTriggerButtonsIcon_AndAloneMakesItAnIconOnlySquare()
    {
        RenderFragment icon = builder =>
        {
            builder.OpenComponent<OmniIcon>(0);
            builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Columns);
            builder.CloseComponent();
        };
        var alone = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Choisir les colonnes")
            .Add(component => component.TriggerIcon, icon));
        var withText = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Tâches en cours")
            .Add(component => component.TriggerIcon, icon)
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "3")));

        Assert.Contains("omni-button--icon", alone.Find("button").ClassList);
        Assert.Equal("Choisir les colonnes", alone.Find("button").GetAttribute("aria-label"));
        Assert.DoesNotContain("omni-button--icon", withText.Find("button").ClassList);
        var content = withText.Find(".omni-button__content");
        Assert.True(content.FirstElementChild!.ClassList.Contains("omni-icon"));
        Assert.Equal("3", content.TextContent.Trim());
    }

    private IRenderedComponent<OmniPopover> RenderPopover(Action<bool>? onChanged = null) =>
        Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Tâches en cours")
            .Add(component => component.TriggerContent, builder => builder.AddContent(0, "3"))
            .Add(component => component.ChildContent, builder => builder.AddContent(0, "Contenu du panneau"))
            .Add(component => component.OpenChanged, value => onChanged?.Invoke(value)));
}
