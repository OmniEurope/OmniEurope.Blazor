using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The page components at their edges: the breadcrumb service with nobody listening, the connection
/// overlay's texts and failing script, shells with the host's words, a page header without the service or
/// on a lost circuit, a stat tile's details, and a wizard whose steps change, refuse or validate twice.
/// </summary>
public sealed class PagesEdgeTests : OmniBunitContext
{
    [Fact]
    public void BreadcrumbService_WithNobodyListening_ChangesQuietly_AndDisposesOnce()
    {
        var service = new OmniBreadcrumbService(Services.GetRequiredService<NavigationManager>());
        service.Set();
        Assert.Null(service.Current);

        service.Push(new OmniBreadcrumbEntry("Accueil", "/"));
        service.Replace(0, new OmniBreadcrumbEntry("Début", "/"));
        Assert.Equal("Début", service.Current!.Text);

        service.Dispose();
        service.Dispose();
    }

    [Fact]
    public async Task ConnectionOverlay_UsesTheHostWords_AndSurvivesItsFocusScript()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.CallFailures["activateDialog"] = new JSException("refus");
        Services.AddSingleton<IJSRuntime>(runtime);
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.Id, "connexion")
            .Add(component => component.State, OmniConnectionState.Rejected)
            .Add(component => component.ReloadText, "Recharger maintenant"));

        Assert.Contains("id=\"connexion", overlay.Markup, StringComparison.Ordinal);
        Assert.Contains("Recharger maintenant", overlay.Markup, StringComparison.Ordinal);
        Assert.Contains("activateDialog", runtime.Module.Calls);

        await overlay.Instance.DisposeAsync();
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void Shells_SpeakTheHostWords()
    {
        var detail = Render<OmniDetailShell>(parameters => parameters
            .Add(component => component.State, OmniDetailState.NotFound)
            .Add(component => component.NotFoundTitle, "Facture introuvable")
            .Add(component => component.BackHref, "/factures")
            .Add(component => component.BackText, "Toutes les factures"));
        Assert.Contains("Facture introuvable", detail.Markup, StringComparison.Ordinal);
        Assert.Contains("Toutes les factures", detail.Markup, StringComparison.Ordinal);

        var login = Render<OmniLoginShell>(parameters => parameters
            .Add(component => component.Id, "connexion")
            .Add(component => component.Title, "Connexion")
            .Add(component => component.SignUpHref, "/inscription")
            .Add(component => component.SignUpText, "Créer un compte")
            .Add(component => component.SignUpPrompt, "Nouveau ici ?"));
        Assert.Equal("connexion-title", login.Find("h1").Id);
        Assert.Contains("Créer un compte", login.Markup, StringComparison.Ordinal);
        Assert.Contains("Nouveau ici ?", login.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void StatTile_ShowsItsDetailAndIcon()
    {
        RenderFragment icon = builder => builder.AddContent(0, "€");
        var tile = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "12")
            .Add(component => component.Detail, "+3 cette semaine")
            .Add(component => component.Icon, icon));
        var button = Render<OmniStatTile>(parameters => parameters
            .Add(component => component.Value, "12")
            .Add(component => component.Detail, "+3 cette semaine")
            .Add(component => component.OnClick, () => { }));

        Assert.Equal("+3 cette semaine", tile.Find(".omni-stat-tile__detail").TextContent);
        Assert.Equal("+3 cette semaine", button.Find(".omni-stat-tile__detail").TextContent);
        Assert.Contains("€", tile.Markup, StringComparison.Ordinal);
    }

    // ---- page header ------------------------------------------------------------------------------

    [Fact]
    public void PageHeader_WithoutTheBreadcrumbService_IsTitledByItsTitle_AndShowsItsActions()
    {
        Services.RemoveAll<OmniBreadcrumbService>();
        RenderFragment actions = builder => builder.AddContent(0, "Exporter");

        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Factures")
            .Add(component => component.Id, "entete")
            .Add(component => component.Actions, actions)
            .Add(component => component.ShowBack, true));

        Assert.Equal("Factures", header.Find("h1").TextContent.Trim());
        Assert.Equal("Exporter", header.Find(".omni-page-header__actions").TextContent);
        header.Find(".omni-page-header__back, [data-omni-back], button").Click();
    }

    [Fact]
    public async Task PageHeaderGoneWhileItsScriptLoads_ReleasesItOnArrival()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var header = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Factures"));

        await header.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);

        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task PageHeader_OnALostCircuit_AttachesAndLeavesQuietly()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var header = Render<OmniPageHeader>(parameters => parameters.Add(component => component.Title, "Factures"));

        await header.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
    }

    // ---- wizard -----------------------------------------------------------------------------------

    private static RenderFragment Steps(params (string Title, bool CanContinue, Func<Task<bool>>? Validate)[] steps) => builder =>
    {
        for (var index = 0; index < steps.Length; index++)
        {
            builder.OpenComponent<OmniWizardStep>(index * 10);
            builder.SetKey(steps[index].Title);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniWizardStep.Title), steps[index].Title);
            builder.AddComponentParameter(index * 10 + 2, nameof(OmniWizardStep.CanContinue), steps[index].CanContinue);
            builder.AddComponentParameter(index * 10 + 3, nameof(OmniWizardStep.Validate), steps[index].Validate);
            builder.CloseComponent();
        }
    };

    [Fact]
    public void WizardWhoseLastStepLeaves_StaysOnTheLastStepLeft()
    {
        var wizard = Render<OmniWizard>(parameters => parameters
            .Add(component => component.Value, 2)
            .AddChildContent(Steps(("Un", true, null), ("Deux", true, null), ("Trois", true, null))));

        wizard.Render(parameters => parameters.AddChildContent(Steps(("Un", true, null), ("Deux", true, null))));

        wizard.WaitForAssertion(() => Assert.Equal("2", wizard.Find(".omni-steps__item--selected .omni-steps__number").TextContent));
    }

    [Fact]
    public void WizardStep_ThatRefuses_KeepsTheStepListFromGoingForward_AndTheCurrentStepIsNoMove()
    {
        var changes = new List<int>();
        var wizard = Render<OmniWizard>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.ValueChanged, value => changes.Add(value))
            .AddChildContent(Steps(("Un", true, null), ("Deux", true, null), ("Trois", true, null))));
        wizard.FindAll(".omni-steps__button")[0].Click();
        Assert.Equal([0], changes);

        wizard.Render(parameters => parameters.AddChildContent(Steps(("Un", false, null), ("Deux", true, null), ("Trois", true, null))));
        wizard.FindAll(".omni-steps__button")[1].Click();
        wizard.FindAll(".omni-steps__button")[0].Click();

        Assert.Equal([0], changes);
    }

    [Fact]
    public async Task WizardValidatingAlready_RefusesASecondNext()
    {
        var pending = new TaskCompletionSource<bool>();
        var validations = 0;
        var wizard = Render<OmniWizard>(parameters => parameters
            .AddChildContent(Steps(("Un", true, () => { validations++; return pending.Task; }), ("Deux", true, null))));
        var steps = wizard.FindAll(".omni-steps__button");

        var first = wizard.Find(".omni-wizard__next").ClickAsync(new());
        await wizard.InvokeAsync(() => wizard.FindAll(".omni-steps__button")[1].ClickAsync(new()));
        pending.SetResult(true);
        await first;

        Assert.Equal(1, validations);
        Assert.NotEmpty(steps);
    }

    [Fact]
    public async Task WizardValidating_RefusesAStepAlreadyReached_UntilTheAnswerComes()
    {
        var pending = new TaskCompletionSource<bool>();
        var validations = 0;
        var wizard = Render<OmniWizard>(parameters => parameters
            .AddChildContent(Steps(("Un", true, () => ++validations == 1 ? Task.FromResult(true) : pending.Task), ("Deux", true, null))));
        wizard.Find(".omni-wizard__next").Click();
        wizard.FindAll(".omni-steps__button")[0].Click();

        // Step two was reached once, so the list offers it; while step one validates again it refuses.
        var next = wizard.Find(".omni-wizard__next").ClickAsync(new());
        await wizard.InvokeAsync(() => wizard.FindAll(".omni-steps__button")[1].ClickAsync(new()));
        Assert.Equal(2, validations);
        pending.SetResult(true);
        await next;

        Assert.Equal(2, validations);
        Assert.Equal("step", wizard.FindAll(".omni-steps__button")[1].GetAttribute("aria-current"));
    }

    [Fact]
    public void Wizard_GivenANewId_NamesItsBodyAfterIt()
    {
        var wizard = Render<OmniWizard>(parameters => parameters
            .Add(component => component.Id, "commande")
            .AddChildContent(Steps(("Un", true, null), ("Deux", true, null))));

        wizard.Render(parameters => parameters.Add(component => component.Id, "facture"));

        Assert.NotNull(wizard.Find("#facture-body"));
    }

    [Fact]
    public void WizardWithoutSteps_FinishesAtOnce_AndAStepOutsideAWizardDrawsNothing()
    {
        var finished = 0;
        var wizard = Render<OmniWizard>(parameters => parameters.Add(component => component.OnFinish, () => finished++));
        wizard.Find(".omni-wizard__finish").Click();
        Assert.Equal(1, finished);

        var alone = Render<OmniWizardStep>(parameters => parameters.Add(component => component.Title, "Seule").AddChildContent("contenu"));
        alone.Render(parameters => parameters.Add(component => component.Title, "Seule encore"));
        Assert.DoesNotContain("contenu", alone.Markup, StringComparison.Ordinal);
        alone.Instance.Dispose();
    }

    [Fact]
    public async Task WizardStep_DisposedTwice_LeavesTheWizardOnce()
    {
        var wizard = Render<OmniWizard>(parameters => parameters.AddChildContent(Steps(("Un", true, null), ("Deux", true, null))));
        var step = wizard.FindComponents<OmniWizardStep>()[1];

        await wizard.InvokeAsync(() =>
        {
            step.Instance.Dispose();
            step.Instance.Dispose();
        });

        Assert.Single(wizard.FindAll(".omni-steps__button"));
    }
}
