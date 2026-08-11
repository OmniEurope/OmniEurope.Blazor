using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class OverlayComponentTests : BunitContext
{
    public OverlayComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

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
        var itemClicked = false;
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Enregistrer")
            .Add(component => component.OnClick, () => clicked = true)
            .AddChildContent<OmniSplitButtonItem>(item => item
                .Add(component => component.OnClick, () => itemClicked = true)
                .AddChildContent("Dupliquer")));

        split.Find(".omni-split-button__main").Click();
        split.Find(".omni-split-button").KeyDown("ArrowDown");

        Assert.True(clicked);
        Assert.NotNull(split.Find("[role=menu]"));
        Assert.Equal("true", split.Find(".omni-split-button__toggle").GetAttribute("aria-expanded"));

        split.Find("[role=menuitem]").Click();
        Assert.True(itemClicked);

        var disabledClicked = false;
        var disabledItem = Render<OmniSplitButtonItem>(parameters => parameters
            .Add(component => component.Disabled, true)
            .Add(component => component.OnClick, () => disabledClicked = true)
            .AddChildContent("Indisponible"));
        disabledItem.Find("button").Click();
        Assert.False(disabledClicked);
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
    public void ComponentsHost_ReplacesItsObservedOverlayService()
    {
        using var first = new OmniOverlayService();
        using var second = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, first));

        first.Notify("Premier", duration: TimeSpan.Zero);
        host.WaitForAssertion(() => Assert.Contains("Premier", host.Markup, StringComparison.Ordinal));

        host.Render(parameters => parameters.Add(component => component.OverlayService, second));
        first.Notify("Ancien", duration: TimeSpan.Zero);
        second.Notify("Nouveau", duration: TimeSpan.Zero);

        host.WaitForAssertion(() => Assert.Contains("Nouveau", host.Markup, StringComparison.Ordinal));
        Assert.DoesNotContain("Ancien", host.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void OverlayService_RendersAndInvokesAnExplicitDialogFooterAction()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));
        service.OpenDialog(new OmniDialogRequest("Confirmation", Content("Continuer ?"))
        {
            Footer = builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, service.CloseDialog));
                builder.AddContent(3, "Fermer");
                builder.CloseElement();
            }
        });

        host.WaitForAssertion(() => Assert.Equal("Fermer", host.Find(".omni-dialog__footer button").TextContent));
        host.Find(".omni-dialog__footer button").Click();

        Assert.Null(service.Dialog);
        Assert.Empty(host.FindAll(".omni-dialog"));
    }

    [Fact]
    public void OverlayService_BoundsAndExpiresNotifications()
    {
        using var service = new OmniOverlayService(notificationCapacity: 2, defaultNotificationDuration: TimeSpan.FromMilliseconds(20));
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service));

        service.Notify("Premier", duration: TimeSpan.Zero);
        service.Notify("Deuxième", duration: TimeSpan.Zero);
        service.Notify("Troisième", duration: TimeSpan.Zero);

        Assert.Equal(2, service.Notifications.Count);
        Assert.DoesNotContain(service.Notifications, notification => notification.Message == "Premier");

        service.Notify("Temporaire");
        host.WaitForAssertion(
            () => Assert.DoesNotContain(service.Notifications, notification => notification.Message == "Temporaire"),
            TimeSpan.FromSeconds(2));
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
        Assert.Null(tooltip.Find(".omni-tooltip__trigger").GetAttribute("tabindex"));

        var focusableTooltip = Render<OmniTooltip>(parameters => parameters
            .Add(component => component.Text, "Information")
            .Add(component => component.TabIndex, 0)
            .AddChildContent("Texte non interactif"));
        Assert.Equal("0", focusableTooltip.Find(".omni-tooltip__trigger").GetAttribute("tabindex"));

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

    [Fact]
    public void OverlayHost_StacksDialogsAndRestoresThePreviousOne()
    {
        var host = Render<OverlayPortalTestHost>();

        host.InvokeAsync(host.Instance.OpenNestedDialogs);
        host.WaitForAssertion(() =>
        {
            var dialogs = host.FindAll(".omni-dialog");
            Assert.Equal(2, dialogs.Count);
            Assert.Equal("true", dialogs[0].GetAttribute("aria-hidden"));
            Assert.True(dialogs[0].HasAttribute("inert"));
            Assert.Contains("Second", dialogs[1].TextContent, StringComparison.Ordinal);
            Assert.NotEqual(dialogs[0].GetAttribute("aria-labelledby"), dialogs[1].GetAttribute("aria-labelledby"));
        });
        host.FindAll(".omni-dialog__close")[1].Click();

        host.WaitForAssertion(() =>
        {
            Assert.Single(host.FindAll(".omni-dialog"));
            Assert.Contains("Premier", host.Find(".omni-dialog__title").TextContent, StringComparison.Ordinal);
            Assert.False(host.Find(".omni-dialog").HasAttribute("inert"));
        });
        host.Find(".omni-dialog__close").Click();
        host.WaitForAssertion(() => Assert.Empty(host.FindAll(".omni-dialog")));
    }

    [Fact]
    public void ContextMenu_UsesTheCentralPortalAndEscapeRemovesItsEntry()
    {
        var host = Render<OverlayPortalTestHost>();
        host.Find(".omni-context-menu").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs
        {
            Key = "F10",
            ShiftKey = true
        });

        host.WaitForAssertion(() =>
        {
            Assert.True(host.Instance.MenuOpen);
            Assert.Equal("1", host.Find(".omni-overlay-portal").GetAttribute("data-overlay-count"));
            Assert.Equal("menu", host.Find(".omni-overlay-portal .omni-context-menu__popup").GetAttribute("role"));
        });

        host.Find(".omni-overlay-portal .omni-context-menu__popup").KeyDown("Escape");
        host.WaitForAssertion(() =>
        {
            Assert.False(host.Instance.MenuOpen);
            Assert.Empty(host.FindAll(".omni-overlay-portal"));
        });
    }

    [Fact]
    public void MenuAndDialog_DelegateFocusMovementAndRestorationToTheStaticModule()
    {
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-focus.js");
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Actions")
            .AddChildContent<OmniSplitButtonItem>(item => item.AddChildContent("Dupliquer")));

        split.Find(".omni-split-button__toggle").Click();
        split.Find(".omni-split-button").KeyDown("End");

        split.WaitForAssertion(() =>
        {
            Assert.Single(module.Invocations["activateMenu"]);
            Assert.Single(module.Invocations["moveMenuFocus"]);
        });

        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Confirmation"));
        dialog.WaitForAssertion(() =>
        {
            Assert.Single(module.Invocations["activateDialog"]);
        });

        var sentinels = dialog.FindAll("[data-focus-sentinel]");
        sentinels[0].Focus();
        sentinels[1].Focus();
        dialog.WaitForAssertion(() =>
        {
            var boundaries = module.Invocations["focusBoundary"];
            Assert.Equal(2, boundaries.Count);
            Assert.Equal(true, boundaries[0].Arguments[1]);
            Assert.Equal(false, boundaries[1].Arguments[1]);
        });

        dialog.Render(parameters => parameters
            .Add(component => component.Open, false)
            .Add(component => component.Title, "Confirmation"));
        dialog.WaitForAssertion(() => Assert.Single(module.Invocations["restoreFocus"]));
    }

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);
}
