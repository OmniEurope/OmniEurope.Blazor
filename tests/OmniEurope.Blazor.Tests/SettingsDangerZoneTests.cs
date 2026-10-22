using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class SettingsDangerZoneTests : OmniBunitContext
{
    private readonly OmniOverlayService _overlays = new();

    [Fact]
    public void DangerZone_IsASectionNamedByItsLocalizedHeading()
    {
        var zone = Render<OmniSettingsDangerZone>(parameters => parameters
            .Add(component => component.Id, "danger")
            .Add(component => component.Description, "Ces actions sont définitives.")
            .AddChildContent("<div class=\"action\">Supprimer</div>"));

        var card = zone.Find("section.omni-card.omni-settings-section.omni-settings-danger-zone");
        Assert.Equal("danger", card.Id);
        Assert.Equal("danger-title", card.GetAttribute("aria-labelledby"));
        var heading = card.QuerySelector("h2#danger-title.omni-settings-section__title")!;
        Assert.Equal("Zone de danger", heading.TextContent);
        Assert.Equal("Ces actions sont définitives.", card.QuerySelector(".omni-settings-section__heading .omni-settings-hint")!.TextContent);
        Assert.NotNull(card.QuerySelector(".omni-card__body .action"));
        Assert.DoesNotContain("style=", zone.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DangerZone_TakesTheHostTitle_AndGeneratesItsHeadingIdWithoutAnId()
    {
        var zone = Render<OmniSettingsDangerZone>(parameters => parameters.Add(component => component.Title, "Projet"));

        var card = zone.Find("section.omni-settings-danger-zone");
        var labelledBy = card.GetAttribute("aria-labelledby")!;
        Assert.StartsWith("omni-settings-danger-zone-", labelledBy, StringComparison.Ordinal);
        Assert.Equal("Projet", zone.Find($"#{labelledBy}").TextContent);
        Assert.Empty(zone.FindAll(".omni-settings-hint"));
    }

    [Fact]
    public void DangerAction_IsATileWithADangerButtonDescribedByTheTile()
    {
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.Id), "delete-project")
            .Add(nameof(OmniSettingsDangerAction.Description), "Le projet et ses rapports sont effacés."));

        var tile = host.Find(".omni-settings-tile.omni-settings-danger-action");
        Assert.Equal("Supprimer le projet", tile.QuerySelector("#delete-project-title.omni-settings-tile__title")!.TextContent);
        Assert.Equal("Le projet et ses rapports sont effacés.", tile.QuerySelector("#delete-project-description.omni-settings-hint")!.TextContent);
        var button = tile.QuerySelector("button#delete-project")!;
        Assert.Contains("omni-button--danger", button.ClassList);
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("delete-project-title delete-project-description", button.GetAttribute("aria-describedby"));
        Assert.Equal("Supprimer", button.QuerySelector("span.omni-button__content > span")!.TextContent);
        Assert.NotNull(button.QuerySelector("svg.omni-icon"));
        Assert.Null(button.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task DangerAction_AsksWithThePackageConfirmation_AndCancellingDoesNothing()
    {
        var calls = 0;
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.ConfirmMessage), "Le projet « PRJ-042 » sera supprimé.")
            .Add(nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () => calls++)));

        host.Find(".omni-settings-danger-action button").Click();

        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__cancel")));
        Assert.Equal("Supprimer le projet", _overlays.Dialog!.Title);
        Assert.Equal(OmniTone.Warning, _overlays.Dialog.Intent);
        Assert.Equal("Le projet « PRJ-042 » sera supprimé.", host.Find(".omni-confirm__message").TextContent);
        var action = host.Find(".omni-confirm__action");
        Assert.Contains("omni-button--danger", action.ClassList);
        Assert.Equal("Supprimer", action.QuerySelector("span.omni-button__content > span")!.TextContent);

        await host.Find(".omni-confirm__cancel").ClickAsync(new());

        host.WaitForAssertion(() => Assert.Null(_overlays.Dialog));
        Assert.Equal(0, calls);
        Assert.Null(host.Find(".omni-settings-danger-action button").GetAttribute("aria-busy"));
    }

    [Fact]
    public void DangerAction_SecondClickWhileAsking_AsksOnce_AndConfirmingRunsOnce()
    {
        var calls = 0;
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () => calls++)));

        host.Find(".omni-settings-danger-action button").Click();
        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__action")));
        host.Find(".omni-settings-danger-action button").Click();

        Assert.Single(host.FindAll("[role=dialog]"));
        host.Find(".omni-confirm__action").Click();

        host.WaitForAssertion(() => Assert.Null(_overlays.Dialog));
        host.WaitForAssertion(() => Assert.Equal(1, calls));
    }

    [Fact]
    public void DangerAction_EscapeOnTheDialog_DoesNothing()
    {
        var calls = 0;
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () => calls++)));

        host.Find(".omni-settings-danger-action button").Click();
        host.WaitForAssertion(() => Assert.NotNull(host.Find("[role=dialog]")));
        host.Find("[role=dialog]").KeyDown("Escape");

        host.WaitForAssertion(() => Assert.Null(_overlays.Dialog));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DangerAction_Confirmed_RunsTheCallbackOnce_BusyUntilItCompletes()
    {
        var calls = 0;
        var completion = new TaskCompletionSource();
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.ConfirmTitle), "Supprimer « PRJ-042 » ?")
            .Add(nameof(OmniSettingsDangerAction.ConfirmText), "Supprimer définitivement")
            .Add(nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () =>
            {
                calls++;
                return completion.Task;
            })));

        host.Find(".omni-settings-danger-action button").Click();
        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__action")));
        Assert.Equal("Supprimer « PRJ-042 » ?", _overlays.Dialog!.Title);
        Assert.Equal("Supprimer définitivement", host.Find(".omni-confirm__action span.omni-button__content > span").TextContent);
        host.Find(".omni-confirm__action").Click();

        host.WaitForAssertion(() => Assert.Equal("true", host.Find(".omni-settings-danger-action button").GetAttribute("aria-busy")));
        var busy = host.Find(".omni-settings-danger-action button");
        Assert.Contains("omni-busy", busy.ClassList);
        Assert.False(busy.HasAttribute("disabled"));
        Assert.Equal(1, calls);

        // A click while busy is ignored: no second question, no second call.
        busy.Click();
        Assert.Null(_overlays.Dialog);

        completion.SetResult();
        host.WaitForAssertion(() => Assert.Null(host.Find(".omni-settings-danger-action button").GetAttribute("aria-busy")));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void DangerAction_FailingCallback_IsReportedToOnConfirmError_AndTheButtonComesBack()
    {
        Exception? reported = null;
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () => Task.FromException(new InvalidOperationException("refusé"))))
            .Add(nameof(OmniSettingsDangerAction.OnConfirmError), EventCallback.Factory.Create<Exception>(this, exception => reported = exception)));

        host.Find(".omni-settings-danger-action button").Click();
        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__action")));
        host.Find(".omni-confirm__action").Click();

        host.WaitForAssertion(() => Assert.Equal("refusé", Assert.IsType<InvalidOperationException>(reported).Message));
        host.WaitForAssertion(() => Assert.Null(host.Find(".omni-settings-danger-action button").GetAttribute("aria-busy")));
    }

    [Fact]
    public async Task DangerAction_FailingCallbackWithoutHandler_Propagates()
    {
        var host = RenderAction(action => action
            .Add(nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () => Task.FromException(new InvalidOperationException("refusé")))));

        // The failure surfaces after the question was answered, so the renderer reports it as an
        // unhandled exception of the click that asked, as for any other failing handler.
        host.Find(".omni-settings-danger-action button").Click();
        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__action")));
        host.Find(".omni-confirm__action").Click();

        var failure = Assert.IsType<InvalidOperationException>(await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken));
        Assert.Equal("refusé", failure.Message);
    }

    [Fact]
    public void DangerAction_Disabled_AsksNothing()
    {
        var host = RenderAction(action => action.Add(nameof(OmniSettingsDangerAction.Disabled), true));

        var button = host.Find(".omni-settings-danger-action button");
        Assert.True(button.HasAttribute("disabled"));
        button.Click();

        Assert.Null(_overlays.Dialog);
        Assert.Empty(host.FindAll("[role=dialog]"));
    }

    [Fact]
    public void DangerAction_TakesItsTextAndIcon_AndWithoutHostAsksTheBrowser()
    {
        var calls = 0;
        JSInterop.Setup<bool>("confirm", "Réinitialiser ?").SetResult(true);
        var action = Render<OmniSettingsDangerAction>(parameters => parameters
            .Add(component => component.Title, "Réinitialiser le compte")
            .Add(component => component.ActionText, "Réinitialiser")
            .Add(component => component.Icon, IconFragment(OmniIconName.Refresh))
            .Add(component => component.ConfirmMessage, "Réinitialiser ?")
            .Add(component => component.OnConfirm, EventCallback.Factory.Create(this, () => calls++)));

        var button = action.Find("button");
        Assert.Equal("Réinitialiser", button.QuerySelector("span.omni-button__content > span")!.TextContent);
        Assert.Equal(action.Find(".omni-settings-tile__title").Id, button.GetAttribute("aria-describedby"));

        button.Click();

        action.WaitForAssertion(() => Assert.Equal(1, calls));
    }

    [Fact]
    public void DangerAction_DrawsItsIconFragmentOnTheButtonAndOnTheDialogAction_DeleteWhenNoneIsGiven()
    {
        string PathOf(OmniIconName name) => Render<OmniIcon>(parameters => parameters.Add(icon => icon.Name, name)).Find("path").GetAttribute("d")!;

        var custom = RenderAction(action => action.Add(nameof(OmniSettingsDangerAction.Icon), IconFragment(OmniIconName.Refresh)));
        Assert.Equal(PathOf(OmniIconName.Refresh), custom.Find(".omni-settings-danger-action button svg path").GetAttribute("d"));
        custom.Find(".omni-settings-danger-action button").Click();
        custom.WaitForAssertion(() => Assert.NotNull(custom.Find(".omni-confirm__action")));
        Assert.Equal(PathOf(OmniIconName.Refresh), custom.Find(".omni-confirm__action svg path").GetAttribute("d"));
        custom.Find(".omni-confirm__cancel").Click();
        custom.WaitForAssertion(() => Assert.Null(_overlays.Dialog));

        var plain = RenderAction(_ => { });
        Assert.Equal(PathOf(OmniIconName.Delete), plain.Find(".omni-settings-danger-action button svg path").GetAttribute("d"));
        plain.Find(".omni-settings-danger-action button").Click();
        plain.WaitForAssertion(() => Assert.NotNull(plain.Find(".omni-confirm__action")));
        Assert.Equal(PathOf(OmniIconName.Delete), plain.Find(".omni-confirm__action svg path").GetAttribute("d"));
    }

    private static RenderFragment IconFragment(OmniIconName name) => builder =>
    {
        builder.OpenComponent<OmniIcon>(0);
        builder.AddComponentParameter(1, nameof(OmniIcon.Name), name);
        builder.CloseComponent();
    };
    private IRenderedComponent<OmniComponentsHost> RenderAction(Action<ActionParameters> configure)
    {
        var parameters = new ActionParameters().Add(nameof(OmniSettingsDangerAction.Title), "Supprimer le projet");
        configure(parameters);

        return Render<OmniComponentsHost>(host => host
            .Add(component => component.OverlayService, _overlays)
            .AddChildContent(builder => BuildZone(builder, parameters)));
    }

    private static void BuildZone(RenderTreeBuilder builder, Dictionary<string, object?> parameters)
    {
        builder.OpenComponent<OmniSettingsDangerZone>(0);
        builder.AddComponentParameter(1, nameof(OmniSettingsDangerZone.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<OmniSettingsDangerAction>(0);
            var sequence = 1;
            foreach (var (name, value) in parameters)
            {
                content.AddComponentParameter(sequence++, name, value);
            }

            content.CloseComponent();
        }));
        builder.CloseComponent();
    }

    /// <summary>The action's parameters, written one after the other like bUnit's own builder.</summary>
    private sealed class ActionParameters : Dictionary<string, object?>
    {
        public new ActionParameters Add(string name, object? value)
        {
            base.Add(name, value);
            return this;
        }
    }
}
