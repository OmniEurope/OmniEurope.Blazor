using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What a confirmation asks of the user before the action: the details under the question and the
/// validity that gates the action, through <see cref="OmniConfirmRequest"/> and through the danger action
/// of a settings page (an account deletion that needs the current password).
/// </summary>
public sealed class ConfirmDetailsTests : OmniBunitContext
{
    private readonly OmniOverlayService _overlays = new();

    [Fact]
    public async Task ConfirmRequest_WithoutDetailsOrGate_IsUnchanged()
    {
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, _overlays));

        var answer = _overlays.ConfirmAsync(new OmniConfirmRequest("Titre", "Question ?"));

        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__action")));
        Assert.Empty(host.FindAll(".omni-confirm__details"));
        Assert.False(host.Find(".omni-confirm__action").HasAttribute("disabled"));
        Assert.Equal(OmniDialogInitialFocus.CloseButton, _overlays.Dialog!.InitialFocus);
        host.Find(".omni-confirm__action").Click();
        Assert.True(await answer);
    }

    [Fact]
    public async Task ConfirmRequest_ShowsItsDetails_GatesTheAction_AndRefreshRedrawsBoth()
    {
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, _overlays));
        var allowed = false;
        var request = new OmniConfirmRequest("Titre", "Question ?")
        {
            Details = builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddAttribute(1, "class", "extra");
                builder.AddContent(2, allowed ? "valide" : "invalide");
                builder.CloseElement();
            },
            CanConfirm = () => allowed
        };

        var answer = _overlays.ConfirmAsync(request);

        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__details .extra")));
        Assert.Equal("invalide", host.Find(".omni-confirm__details .extra").TextContent);
        Assert.Equal(OmniDialogInitialFocus.FirstFocusable, _overlays.Dialog!.InitialFocus);
        var action = host.Find(".omni-confirm__action");
        Assert.True(action.HasAttribute("disabled"));

        // A click on the disabled action answers nothing.
        action.Click();
        Assert.False(answer.IsCompleted);
        Assert.NotNull(_overlays.Dialog);

        allowed = true;
        request.Refresh();

        host.WaitForAssertion(() => Assert.Equal("valide", host.Find(".omni-confirm__details .extra").TextContent));
        host.WaitForAssertion(() => Assert.False(host.Find(".omni-confirm__action").HasAttribute("disabled")));
        host.Find(".omni-confirm__action").Click();
        Assert.True(await answer);
    }

    [Fact]
    public async Task ConfirmRequest_CancellingStaysPossible_WhileTheGateIsClosed()
    {
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, _overlays));
        var answer = _overlays.ConfirmAsync(new OmniConfirmRequest("Titre", "Question ?") { CanConfirm = () => false });

        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__cancel")));
        Assert.False(host.Find(".omni-confirm__cancel").HasAttribute("disabled"));
        host.Find(".omni-confirm__cancel").Click();

        Assert.False(await answer);
    }

    [Fact]
    public void DangerAction_AccountDeletion_ConfirmStaysDisabledUntilThePasswordIsValid_ThenRunsOnce()
    {
        var page = RenderPage();

        page.Find(".omni-settings-danger-action button").Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".omni-confirm__details input#current-password")));
        var field = page.Find(".omni-confirm__details input#current-password");
        Assert.Equal("password", field.GetAttribute("type"));
        Assert.True(page.Find(".omni-confirm__action").HasAttribute("disabled"));
        Assert.Equal(OmniDialogInitialFocus.FirstFocusable, _overlays.Dialog!.InitialFocus);

        // Invalid entry: still disabled, and a click on the disabled action runs nothing.
        field.Input("abc");
        page.WaitForAssertion(() => Assert.True(page.Find(".omni-confirm__action").HasAttribute("disabled")));
        page.Find(".omni-confirm__action").Click();
        Assert.Equal(0, PageOf(page).Confirmed);
        Assert.NotNull(_overlays.Dialog);

        // Valid entry: the action comes back and the host receives the value through its own field.
        page.Find(".omni-confirm__details input#current-password").Input("correct horse");
        page.WaitForAssertion(() => Assert.False(page.Find(".omni-confirm__action").HasAttribute("disabled")));
        page.Find(".omni-confirm__action").Click();

        page.WaitForAssertion(() => Assert.Null(_overlays.Dialog));
        page.WaitForAssertion(() => Assert.Equal(1, PageOf(page).Confirmed));
        Assert.Equal("correct horse", PageOf(page).ConfirmedWith);
        Assert.Equal(0, PageOf(page).Cancelled);
    }

    [Fact]
    public void DangerAction_AccountDeletion_InvalidAgainAfterBeingValid_DisablesTheActionAgain()
    {
        var page = RenderPage();

        page.Find(".omni-settings-danger-action button").Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find("input#current-password")));
        page.Find("input#current-password").Input("correct horse");
        page.WaitForAssertion(() => Assert.False(page.Find(".omni-confirm__action").HasAttribute("disabled")));

        page.Find("input#current-password").Input("");

        page.WaitForAssertion(() => Assert.True(page.Find(".omni-confirm__action").HasAttribute("disabled")));
        Assert.Equal(0, PageOf(page).Confirmed);
    }

    [Fact]
    public void DangerAction_AccountDeletion_CancellingOrEscapeRunsNothing_AndTheEntryIsNotKeptByTheComponent()
    {
        var page = RenderPage();

        page.Find(".omni-settings-danger-action button").Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find("input#current-password")));
        page.Find("input#current-password").Input("correct horse");
        page.Find(".omni-confirm__cancel").Click();

        page.WaitForAssertion(() => Assert.Null(_overlays.Dialog));
        Assert.Equal(0, PageOf(page).Confirmed);
        page.WaitForAssertion(() => Assert.Equal(1, PageOf(page).Cancelled));
        // The typed value belongs to the host's field only: nothing of it is on the tile or in the closed dialog.
        Assert.DoesNotContain("correct horse", page.Markup, StringComparison.Ordinal);

        page.Find(".omni-settings-danger-action button").Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find("[role=dialog]")));
        page.Find("[role=dialog]").KeyDown("Escape");
        page.WaitForAssertion(() => Assert.Null(_overlays.Dialog));
        Assert.Equal(0, PageOf(page).Confirmed);
    }

    [Fact]
    public void DangerAction_CanConfirmFalseWithoutDetails_StillGatesTheDialogAction()
    {
        var calls = 0;
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, _overlays)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniSettingsDangerAction>(0);
                builder.AddComponentParameter(1, nameof(OmniSettingsDangerAction.Title), "Réinitialiser");
                builder.AddComponentParameter(2, nameof(OmniSettingsDangerAction.CanConfirm), false);
                builder.AddComponentParameter(3, nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () => calls++));
                builder.CloseComponent();
            }));

        host.Find(".omni-settings-danger-action button").Click();
        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-confirm__action")));

        Assert.True(host.Find(".omni-confirm__action").HasAttribute("disabled"));
        Assert.Empty(host.FindAll(".omni-confirm__details"));
        Assert.Equal(OmniDialogInitialFocus.CloseButton, _overlays.Dialog!.InitialFocus);
        host.Find(".omni-confirm__action").Click();
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DangerAction_DetailsWithoutAHost_ThrowsInsteadOfAskingWithoutTheField()
    {
        var calls = 0;
        var action = Render<OmniSettingsDangerAction>(parameters => parameters
            .Add(component => component.Title, "Supprimer le compte")
            .Add(component => component.ConfirmDetails, (RenderFragment)(builder => builder.AddContent(0, "champ")))
            .Add(component => component.OnConfirm, EventCallback.Factory.Create(this, () => calls++)));

        // The handler fails before its first await, so the click itself reports it.
        var failure = Assert.ThrowsAny<InvalidOperationException>(() => action.Find("button").Click());
        Assert.Contains(nameof(OmniSettingsDangerAction.ConfirmDetails), failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DangerAction_CanConfirmFalseWithoutAHost_NeverAsksTheBrowserNorRunsTheAction()
    {
        var calls = 0;
        var action = Render<OmniSettingsDangerAction>(parameters => parameters
            .Add(component => component.Title, "Supprimer le compte")
            .Add(component => component.CanConfirm, false)
            .Add(component => component.OnConfirm, EventCallback.Factory.Create(this, () => calls++)));

        action.Find("button").Click();

        Assert.Empty(JSInterop.Invocations);
        Assert.Equal(0, calls);
    }

    private IRenderedComponent<PasswordPage> RenderPage() =>
        Render<PasswordPage>(parameters => parameters.Add(component => component.Overlays, _overlays));

    private static PasswordPage PageOf(IRenderedComponent<PasswordPage> page) => page.Instance;

    /// <summary>A settings page as a client application writes it: its own field in the details, its own validity.</summary>
    private sealed class PasswordPage : ComponentBase
    {
        private string _password = string.Empty;

        [Parameter]
        public OmniOverlayService Overlays { get; set; } = default!;

        public int Confirmed { get; private set; }

        public int Cancelled { get; private set; }

        public string? ConfirmedWith { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<OmniComponentsHost>(0);
            builder.AddComponentParameter(1, nameof(OmniComponentsHost.OverlayService), Overlays);
            builder.AddComponentParameter(2, nameof(OmniComponentsHost.ChildContent), (RenderFragment)(host =>
            {
                host.OpenComponent<OmniSettingsDangerAction>(0);
                host.AddComponentParameter(1, nameof(OmniSettingsDangerAction.Title), "Supprimer le compte");
                host.AddComponentParameter(2, nameof(OmniSettingsDangerAction.CanConfirm), _password.Length >= 8);
                host.AddComponentParameter(3, nameof(OmniSettingsDangerAction.ConfirmDetails), (RenderFragment)(details =>
                {
                    details.OpenElement(0, "input");
                    details.AddAttribute(1, "type", "password");
                    details.AddAttribute(2, "id", "current-password");
                    details.AddAttribute(3, "autocomplete", "current-password");
                    details.AddAttribute(4, "oninput", EventCallback.Factory.Create<ChangeEventArgs>(this, change => _password = change.Value?.ToString() ?? string.Empty));
                    details.CloseElement();
                }));
                host.AddComponentParameter(4, nameof(OmniSettingsDangerAction.OnConfirm), EventCallback.Factory.Create(this, () =>
                {
                    Confirmed++;
                    ConfirmedWith = _password;
                }));
                host.AddComponentParameter(5, nameof(OmniSettingsDangerAction.OnCancel), EventCallback.Factory.Create(this, () =>
                {
                    Cancelled++;
                    _password = string.Empty;
                }));
                host.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
