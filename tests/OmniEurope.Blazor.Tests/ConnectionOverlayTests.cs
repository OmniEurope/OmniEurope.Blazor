using System.Globalization;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class ConnectionOverlayTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    private readonly BunitJSModuleInterop _focus;

    public ConnectionOverlayTests()
    {
        _focus = JSInterop.SetupModule(FocusModule);
        _focus.SetupVoid("activateDialog", _ => true).SetVoidResult();
        _focus.SetupVoid("restoreFocus", _ => true).SetVoidResult();
    }

    [Fact]
    public void Connected_RendersNothing()
    {
        var overlay = Render<OmniConnectionOverlay>();

        Assert.Equal(string.Empty, overlay.Markup.Trim());
        _focus.VerifyNotInvoke("activateDialog");
    }

    [Fact]
    public void FirstRender_LoadsTheFocusScript_WhileTheConnectionIsUp()
    {
        // The overlay shows when the server may be unreachable; the script is fetched before that.
        Render<OmniConnectionOverlay>();

        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier == "import"
            && Equals(invocation.Arguments[0], FocusModule));
    }

    [Fact]
    public void Reconnecting_SaysWhatHappenedWhenTheNextAttemptStartsAndWhyTheLastFailed()
    {
        var reconnects = 0;
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting)
            .Add(component => component.SecondsUntilRetry, 5)
            .Add(component => component.Reason, "délai dépassé")
            .Add(component => component.OnReconnect, () => reconnects++));

        var card = overlay.Find("[role=alertdialog]");
        Assert.Equal("true", card.GetAttribute("aria-modal"));
        Assert.Equal("Connexion perdue", overlay.Find($"#{card.GetAttribute("aria-labelledby")}").TextContent);
        Assert.Contains("tente de la rétablir", overlay.Find($"#{card.GetAttribute("aria-describedby")}").TextContent, StringComparison.Ordinal);
        Assert.Equal("Nouvelle tentative dans 5 s", overlay.Find(".omni-connection-overlay__countdown").TextContent);
        Assert.Equal("Cause : délai dépassé", overlay.Find(".omni-connection-overlay__reason").TextContent);
        // The countdown ticks every second: it stays out of the live region.
        Assert.Null(overlay.Find(".omni-connection-overlay__countdown").Closest("[aria-live]"));
        Assert.Single(overlay.FindAll("[role=progressbar]"));

        var action = overlay.Find(".omni-connection-overlay__action");
        Assert.Contains("Se reconnecter maintenant", action.TextContent, StringComparison.Ordinal);
        action.Click();
        Assert.Equal(1, reconnects);
        _focus.VerifyInvoke("activateDialog");
        Assert.DoesNotContain("style=", overlay.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Busy_MarksTheActionAndDropsTheSpinner_AndNoCountdownAtZero()
    {
        var reconnects = 0;
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting)
            .Add(component => component.Busy, true)
            .Add(component => component.OnReconnect, () => reconnects++));

        Assert.Equal("true", overlay.Find(".omni-connection-overlay__action").GetAttribute("aria-busy"));
        Assert.Empty(overlay.FindAll("[role=progressbar]"));
        // Recette R-129: the slots stay, empty and hidden from assistive technology, so nothing moves.
        AssertReservedAndEmpty(overlay, ".omni-connection-overlay__countdown");
        AssertReservedAndEmpty(overlay, ".omni-connection-overlay__reason");
        Assert.Single(overlay.FindAll(".omni-connection-overlay__spinner--idle"));
        overlay.Find(".omni-connection-overlay__action").Click();
        Assert.Equal(0, reconnects);
    }

    [Fact]
    public void Failed_LeavesOnlyTheManualAttempt()
    {
        var reconnects = 0;
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Failed)
            .Add(component => component.SecondsUntilRetry, 5)
            .Add(component => component.OnReconnect, () => reconnects++));

        Assert.Equal("Connexion impossible", overlay.Find(".omni-connection-overlay__title").TextContent);
        AssertReservedAndEmpty(overlay, ".omni-connection-overlay__countdown");
        Assert.Empty(overlay.FindAll("[role=progressbar]"));
        overlay.Find(".omni-connection-overlay__action").Click();
        Assert.Equal(1, reconnects);
    }

    [Fact]
    public void Rejected_OffersAReload_TheHostsOrTheBrowsersOwn()
    {
        var reloads = 0;
        var handled = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Rejected)
            .Add(component => component.OnReload, () => reloads++));
        Assert.Equal("Session interrompue", handled.Find(".omni-connection-overlay__title").TextContent);
        Assert.Contains("Recharger la page", handled.Find(".omni-connection-overlay__action").TextContent, StringComparison.Ordinal);
        handled.Find(".omni-connection-overlay__action").Click();
        Assert.Equal(1, reloads);

        var navigation = (BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        var unhandled = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Rejected));
        unhandled.Find(".omni-connection-overlay__action").Click();
        var reload = Assert.Single(navigation.History);
        Assert.True(reload.Options.ForceLoad);
    }

    [Fact]
    public void BackToConnected_RemovesTheOverlayAndGivesTheFocusBack()
    {
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting)
            .Add(component => component.Title, "Hors ligne")
            .Add(component => component.Description, "Le réseau est coupé."));
        Assert.Equal("Hors ligne", overlay.Find(".omni-connection-overlay__title").TextContent);
        Assert.Equal("Le réseau est coupé.", overlay.Find(".omni-connection-overlay__description").TextContent);

        overlay.Render(parameters => parameters.Add(component => component.State, OmniConnectionState.Connected));

        Assert.Empty(overlay.FindAll("[role=alertdialog]"));
        _focus.VerifyInvoke("restoreFocus");
    }

    [Theory]
    [InlineData("en-US", OmniConnectionState.Reconnecting, "Connection lost", "Reconnect now")]
    [InlineData("en-US", OmniConnectionState.Failed, "Unable to connect", "Reconnect now")]
    [InlineData("en-US", OmniConnectionState.Rejected, "Session ended", "Reload the page")]
    [InlineData("fr-FR", OmniConnectionState.Failed, "Connexion impossible", "Se reconnecter maintenant")]
    public void Texts_FollowTheUiCulture(string cultureName, OmniConnectionState state, string title, string action)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var overlay = Render<OmniConnectionOverlay>(parameters => parameters.Add(component => component.State, state));

            Assert.Equal(title, overlay.Find(".omni-connection-overlay__title").TextContent);
            Assert.Contains(action, overlay.Find(".omni-connection-overlay__action").TextContent, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void ActionLabel_ComesFromTheHost_WhenGiven()
    {
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting)
            .Add(component => component.ReconnectText, "Se reconnecter"));

        Assert.Equal("Se reconnecter", overlay.Find(".omni-connection-overlay__action span:not(.omni-icon)").TextContent.Trim());
    }

    private static void AssertReservedAndEmpty(IRenderedComponent<OmniConnectionOverlay> overlay, string selector)
    {
        var slot = overlay.Find(selector);
        Assert.Equal("true", slot.GetAttribute("aria-hidden"));
        Assert.Equal("\u00a0", slot.TextContent);
    }
}

/// <summary>
/// The focus script cannot be fetched (the static files are down with the rest of the server): the
/// overlay still shows and acts, and nothing escapes as an unhandled error (the host's fatal bar).
/// </summary>
public sealed class ConnectionOverlayUnreachableScriptTests : OmniBunitContext
{
    [Fact]
    public void ImportFailure_StillShowsTheOverlay_WithoutThrowing()
    {
        Services.AddSingleton<Microsoft.JSInterop.IJSRuntime>(new UnreachableScriptRuntime());

        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting));

        Assert.NotEmpty(overlay.FindAll(".omni-connection-overlay__card--reconnecting"));
        overlay.Render(parameters => parameters.Add(component => component.SecondsUntilRetry, 5));
        Assert.NotEmpty(overlay.FindAll(".omni-connection-overlay__card--reconnecting"));
    }

    private sealed class UnreachableScriptRuntime : Microsoft.JSInterop.IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new Microsoft.JSInterop.JSException("Failed to fetch dynamically imported module");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new Microsoft.JSInterop.JSException("Failed to fetch dynamically imported module");
    }
}
