using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class OverlayComponentTests : BunitContext
{
    [Fact]
    public void ToggleAndSplitButtons_ExposeControlledStatesAndKeyboardMenu()
    {
        var pressed = false;
        var toggle = Render<OmniToggleButton>(parameters => parameters
            .Add(component => component.Value, pressed)
            .Add(component => component.ValueChanged, value => pressed = value)
            .AddChildContent("Activer"));

        toggle.Find("button").Click();
        Assert.True(pressed);

        var clicked = false;
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Enregistrer")
            .Add(component => component.OnClick, () => clicked = true)
            .AddChildContent<OmniSplitButtonItem>(item => item
                .Add(component => component.OnClick, () => { })
                .AddChildContent("Dupliquer")));

        split.Find(".omni-split-button__main").Click();
        split.Find(".omni-split-button").KeyDown("ArrowDown");

        Assert.True(clicked);
        Assert.NotNull(split.Find("[role=menu]"));
        Assert.Equal("true", split.Find(".omni-split-button__toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void OverlayService_DrivesDialogAndNotificationHost()
    {
        var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent("Application"));

        service.OpenDialog(new OmniDialogRequest("Confirmation", Content("Continuer ?")));
        service.Notify("Enregistré", OmniNotificationSeverity.Success, "Succès");

        host.WaitForAssertion(() => Assert.Contains("Confirmation", host.Markup, StringComparison.Ordinal));
        Assert.Contains("Enregistré", host.Markup, StringComparison.Ordinal);
        Assert.Equal("dialog", host.Find(".omni-dialog").GetAttribute("role"));
        Assert.Equal("status", host.Find(".omni-notification").GetAttribute("role"));

        host.Find(".omni-dialog__close").Click();
        host.Find(".omni-notification__dismiss").Click();

        Assert.Null(service.Dialog);
        Assert.Empty(service.Notifications);
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TooltipAndContextMenu_AreKeyboardAccessible()
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters
            .Add(component => component.Id, "help")
            .Add(component => component.Text, "Information")
            .AddChildContent("Aide"));

        Assert.Equal("help-content", tooltip.Find(".omni-tooltip__trigger").GetAttribute("aria-describedby"));
        Assert.Equal("tooltip", tooltip.Find("#help-content").GetAttribute("role"));

        var open = false;
        var menu = Render<OmniContextMenu>(parameters => parameters
            .Add(component => component.Open, open)
            .Add(component => component.OpenChanged, value => open = value)
            .Add(component => component.TriggerContent, Content("Cible"))
            .AddChildContent("Action"));

        menu.Find(".omni-context-menu").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
        {
            Key = "F10",
            ShiftKey = true
        });

        Assert.True(open);
    }

    [Fact]
    public void DialogAndUrgentNotification_ExposeAccessibleSemantics()
    {
        var open = true;
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Id, "confirm")
            .Add(component => component.Open, open)
            .Add(component => component.OpenChanged, value => open = value)
            .Add(component => component.Title, "Confirmation")
            .AddChildContent("Continuer ?"));

        var dialogElement = dialog.Find("[role=dialog]");
        Assert.Equal("true", dialogElement.GetAttribute("aria-modal"));
        Assert.Equal("confirm-title", dialogElement.GetAttribute("aria-labelledby"));
        Assert.Equal("Confirmation", dialog.Find("#confirm-title").TextContent);
        Assert.Equal(2, dialog.FindAll(".omni-visually-hidden").Count);
        dialogElement.KeyDown("Escape");
        Assert.False(open);

        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Échec")
            .Add(component => component.Severity, OmniNotificationSeverity.Error));
        Assert.Equal("alert", notification.Find("article").GetAttribute("role"));
        Assert.Equal("assertive", notification.Find("article").GetAttribute("aria-live"));
    }

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);
}
