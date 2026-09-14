using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class WizardTests : OmniBunitContext
{
    [Fact]
    public void Wizard_ShowsOnlyTheCurrentStepWithItsPositionAndTheWayForward()
    {
        var host = Render<WizardTestHost>();

        Assert.Equal("Contenu 1", host.Find(".omni-wizard__body").TextContent.Trim());
        Assert.Empty(host.FindAll(".host-step-2"));
        Assert.Equal(["Nom", "Options", "Fin"], host.FindAll(".omni-steps__button > span:last-child").Select(title => title.TextContent));
        Assert.Equal("Étape 1 sur 3", host.Find(".omni-wizard__progress").GetAttribute("aria-valuetext"));
        Assert.Equal("0", host.Find(".omni-wizard__progress").GetAttribute("aria-valuenow"));
        Assert.Equal("Étape 1 sur 3 : Nom", host.Find(".omni-wizard [role=status]").TextContent);
        Assert.Empty(host.FindAll(".omni-wizard__previous"));
        Assert.Single(host.FindAll(".omni-wizard__next"));
        Assert.Contains("omni-button--danger", host.Find(".omni-wizard__cancel").ClassName, StringComparison.Ordinal);

        // One body shows every step: the step buttons control it and the current one names it.
        var body = host.Find(".omni-wizard__body");
        Assert.All(host.FindAll(".omni-steps__button"), button => Assert.Equal(body.Id, button.GetAttribute("aria-controls")));
        Assert.Equal(host.FindAll(".omni-steps__button")[0].Id, body.GetAttribute("aria-labelledby"));
        Assert.Empty(host.FindAll(".omni-steps__panel"));
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Step_ThatCannotContinue_DisablesNextUntilItCan()
    {
        var host = Render<WizardTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.NameReady = false));
        Assert.True(host.Find(".omni-wizard__next").HasAttribute("disabled"));
        host.Find(".omni-wizard__next").Click();
        Assert.Equal(0, host.Instance.Step);

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.NameReady = true));
        Assert.False(host.Find(".omni-wizard__next").HasAttribute("disabled"));
        host.Find(".omni-wizard__next").Click();
        Assert.Equal(1, host.Instance.Step);
        Assert.Equal("Contenu 2", host.Find(".omni-wizard__body").TextContent.Trim());
        Assert.Equal("Étape 2 sur 3 : Options", host.Find(".omni-wizard [role=status]").TextContent);
        Assert.Equal("50", host.Find(".omni-wizard__progress").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public async Task Validate_RefusingKeepsTheStep_AcceptingMovesOnAndFinishRaisesOnFinish()
    {
        var host = Render<WizardTestHost>();
        host.Find(".omni-wizard__next").Click();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Accept = false));
        host.Find(".omni-wizard__next").Click();
        Assert.Equal(1, host.Instance.Validations);
        Assert.Equal(1, host.Instance.Step);

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Accept = true));
        host.Find(".omni-wizard__next").Click();
        Assert.Equal(2, host.Instance.Validations);
        Assert.Equal(2, host.Instance.Step);

        Assert.Empty(host.FindAll(".omni-wizard__next"));
        var finish = host.Find(".omni-wizard__finish");
        Assert.Contains("Créer", finish.TextContent, StringComparison.Ordinal);
        finish.Click();
        Assert.Equal(1, host.Instance.Finished);
    }

    [Fact]
    public void Previous_AndTheStepList_GoBackFreely_AndForwardOnlyToReachedStepsAfterAsking()
    {
        var host = Render<WizardTestHost>();

        // Not reached yet: the later steps cannot be clicked.
        Assert.True(host.FindAll(".omni-steps__button")[2].HasAttribute("disabled"));

        host.Find(".omni-wizard__next").Click();
        host.Find(".omni-wizard__next").Click();
        Assert.Equal(2, host.Instance.Step);

        host.Find(".omni-wizard__previous").Click();
        Assert.Equal(1, host.Instance.Step);
        host.FindAll(".omni-steps__button")[0].Click();
        Assert.Equal(0, host.Instance.Step);
        var validationsBefore = host.Instance.Validations;

        // Forward through the list, the current step is asked first: step 1 has no Validate.
        host.FindAll(".omni-steps__button")[2].Click();
        Assert.Equal(2, host.Instance.Step);
        Assert.Equal(validationsBefore, host.Instance.Validations);

        host.FindAll(".omni-steps__button")[1].Click();
        host.Instance.Accept = false;
        host.FindAll(".omni-steps__button")[2].Click();
        Assert.Equal(1, host.Instance.Step);
    }

    [Fact]
    public async Task BoundValue_MovesTheWizard_AndARenamedStepRedrawsTheList()
    {
        var host = Render<WizardTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Step = 2));
        Assert.Equal("Contenu 3", host.Find(".omni-wizard__body").TextContent.Trim());
        Assert.Equal("step", host.FindAll(".omni-steps__button")[2].GetAttribute("aria-current"));

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.OptionsTitle = "Réglages"));
        Assert.Equal("Réglages", host.FindAll(".omni-steps__button > span:last-child")[1].TextContent);
    }

    [Fact]
    public void Cancel_RaisesOnCancel_AndAWizardWithoutHandlerHasNoCancel()
    {
        var host = Render<WizardTestHost>();
        host.Find(".omni-wizard__cancel").Click();
        Assert.Equal(1, host.Instance.Cancelled);

        var bare = Render<OmniWizard>(parameters => parameters
            .AddChildContent<OmniWizardStep>(step => step.Add(component => component.Title, "Seule")));
        Assert.Empty(bare.FindAll(".omni-wizard__cancel"));
        Assert.Single(bare.FindAll(".omni-wizard__finish"));
        Assert.Equal("100", bare.Find(".omni-wizard__progress").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Wizard_LivesInsideADialog()
    {
        var host = Render<WizardTestHost>(parameters => parameters.Add(component => component.InDialog, true));

        var dialog = host.Find("[role=dialog]");
        Assert.Equal("Étapes de l'invitation", dialog.QuerySelector(".omni-steps__list")!.GetAttribute("aria-label"));
        Assert.Single(dialog.QuerySelectorAll(".host-dialog-step"));
        Assert.NotNull(dialog.QuerySelector(".omni-wizard__next"));
        Assert.Null(dialog.QuerySelector(".omni-wizard__cancel"));
    }

    [Fact]
    public void MovingForward_TakesTheFocusToTheNewStep()
    {
        var host = Render<WizardTestHost>();
        var before = JSInterop.Invocations.Count(invocation => invocation.Identifier.EndsWith(".focus", StringComparison.Ordinal));

        host.Find(".omni-wizard__next").Click();

        Assert.Equal(before + 1, JSInterop.Invocations.Count(invocation => invocation.Identifier.EndsWith(".focus", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("fr-FR", "Suivant", "Précédent", "Terminer", "Étapes de l'assistant")]
    [InlineData("en-US", "Next", "Previous", "Finish", "Wizard steps")]
    public void Wizard_DefaultsFollowTheUiCulture(string cultureName, string next, string previous, string finish, string label)
    {
        var former = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var wizard = Render<OmniWizard>(parameters => parameters
                .AddChildContent<OmniWizardStep>(step => step.Add(component => component.Title, "Un"))
                .AddChildContent<OmniWizardStep>(step => step.Add(component => component.Title, "Deux")));

            Assert.Equal(label, wizard.Find(".omni-steps__list").GetAttribute("aria-label"));
            Assert.Contains(next, wizard.Find(".omni-wizard__next").TextContent, StringComparison.Ordinal);
            wizard.Find(".omni-wizard__next").Click();
            Assert.Contains(previous, wizard.Find(".omni-wizard__previous").TextContent, StringComparison.Ordinal);
            Assert.Contains(finish, wizard.Find(".omni-wizard__finish").TextContent, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = former;
        }
    }
}
