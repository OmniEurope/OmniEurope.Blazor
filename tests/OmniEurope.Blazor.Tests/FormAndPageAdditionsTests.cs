using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The unsaved-changes guard, the selectable card, the labelled check box and switch, the sign-up line
/// of the sign-in page, the back button look, the icon of a description item, the first line number
/// of a code excerpt, the appearance window and the loading bar of a grid.
/// </summary>
public sealed class FormAndPageAdditionsTests : OmniBunitContext
{
    // ---- unsaved changes ------------------------------------------------------------------------

    [Fact]
    public async Task Guard_WithChanges_AsksBeforeAnInternalNavigation_AndStayKeepsThePage()
    {
        using var service = new OmniOverlayService();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent(Guard(hasChanges: true)));
        var start = navigation.Uri;

        _ = host.InvokeAsync(() => navigation.NavigateTo("/ailleurs"));

        host.WaitForAssertion(() => Assert.Equal("Modifications non enregistrées", host.Find(".omni-dialog__title").TextContent), TimeSpan.FromSeconds(10));
        Assert.Contains("omni-dialog--intent-warning", host.Find(".omni-dialog").ClassList);
        Assert.Equal("Quitter sans enregistrer", host.Find(".omni-confirm__action span.omni-button__content > span").TextContent);
        Assert.Contains("omni-button--danger", host.Find(".omni-confirm__action").ClassList);
        host.Find(".omni-confirm__cancel").Click();

        host.WaitForAssertion(() => Assert.Empty(host.FindAll(".omni-dialog")), TimeSpan.FromSeconds(10));
        // The navigation manager records the prevented attempt when the location-changing handler
        // returns, after the last render of the dialog: WaitForAssertion only re-checks on a render, so it
        // would check once and time out (flaky on CI). The record is awaited by polling instead.
        var history = ((BunitNavigationManager)navigation).History;
        await UntilAsync(() => history.Count > 0);
        Assert.Equal(NavigationState.Prevented, Assert.Single(history).State);
        Assert.Equal(start, navigation.Uri);
    }

    [Fact]
    public async Task Guard_WithChanges_LetsTheNavigationThroughOnceConfirmed()
    {
        using var service = new OmniOverlayService();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent(Guard(hasChanges: true)));

        _ = host.InvokeAsync(() => navigation.NavigateTo("/ailleurs"));
        // Same readiness as the Stay test: the question is drawn (its title) before its button is used.
        host.WaitForAssertion(() => Assert.Equal("Modifications non enregistrées", host.Find(".omni-dialog__title").TextContent), TimeSpan.FromSeconds(10));
        host.Find(".omni-confirm__action").Click();

        host.WaitForAssertion(() => Assert.Empty(host.FindAll(".omni-dialog")), TimeSpan.FromSeconds(10));
        // The address changes once the location-changing handler returns, after the last render of the
        // host, so a render-driven WaitForAssertion would never re-check it: poll the address instead.
        await UntilAsync(() => navigation.Uri.EndsWith("/ailleurs", StringComparison.Ordinal));
        Assert.EndsWith("/ailleurs", navigation.Uri, StringComparison.Ordinal);
    }

    /// <summary>Polls <paramref name="condition"/> until it holds, for state that changes without a render.</summary>
    private static async Task UntilAsync(Func<bool> condition)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Delay(10);
        }
    }

    [Fact]
    public async Task Guard_WithoutChanges_NeverAsks()
    {
        using var service = new OmniOverlayService();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent(Guard(hasChanges: false)));

        _ = host.InvokeAsync(() => navigation.NavigateTo("/ailleurs"));

        await UntilAsync(() => navigation.Uri.EndsWith("/ailleurs", StringComparison.Ordinal));
        Assert.EndsWith("/ailleurs", navigation.Uri, StringComparison.Ordinal);
        Assert.Empty(host.FindAll(".omni-dialog"));
    }

    [Fact]
    public void TemplateForm_GuardsOnceAFieldChanges_AndAValidSubmitReleasesIt()
    {
        var model = new GuardedModel();
        var form = Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.GuardUnsavedChanges, true)
            .Add(component => component.OnValidSubmit, _ => { })
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(context => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "submit");
                builder.CloseElement();
            })));

        var guard = form.FindComponent<OmniUnsavedChangesGuard>();
        Assert.False(guard.Instance.HasChanges);

        var editContext = form.FindComponent<EditForm>().Instance.EditContext!;
        model.Name = "Ada";
        form.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(GuardedModel.Name))));
        Assert.True(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);

        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.False(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges));

        // Saved: "Ada" is the new reference, so a change back to it after an edit is no change.
        model.Name = "Grace";
        form.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(GuardedModel.Name))));
        Assert.True(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
        model.Name = "Ada";
        form.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(GuardedModel.Name))));
        Assert.False(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
    }

    [Fact]
    public void TemplateForm_TypingThenErasing_LeavesTheFormUnmodified()
    {
        // recette R-061: one keystroke in an empty password field, then erased, kept the form
        // modified and a tab click asked to leave. Empty text and no text are the same value.
        var model = new GuardedModel { Address = new GuardedAddress { City = "Lyon" }, Tags = ["a"] };
        var form = Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.GuardUnsavedChanges, true)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => _ => { })));
        var editContext = form.FindComponent<EditForm>().Instance.EditContext!;
        bool Modified() => form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges;
        void Changed(object owner, string field) => form.InvokeAsync(() => editContext.NotifyFieldChanged(new FieldIdentifier(owner, field)));

        model.Name = "x";
        Changed(model, nameof(GuardedModel.Name));
        Assert.True(Modified());
        model.Name = "";
        Changed(model, nameof(GuardedModel.Name));
        Assert.False(Modified());

        // A field of an object the model holds, and a list edited in place.
        model.Address.City = "Paris";
        Changed(model.Address, nameof(GuardedAddress.City));
        Assert.True(Modified());
        model.Address.City = "Lyon";
        Changed(model.Address, nameof(GuardedAddress.City));
        Assert.False(Modified());
        model.Tags.Add("b");
        Changed(model, nameof(GuardedModel.Tags));
        Assert.True(Modified());
        model.Tags.Remove("b");
        Changed(model, nameof(GuardedModel.Tags));
        Assert.False(Modified());
    }

    [Fact]
    public void TemplateForm_GuardIsOptIn()
    {
        // Off by default: a page that already guards its changes would otherwise ask twice.
        var form = Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, new GuardedModel())
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => _ => { })));

        Assert.Empty(form.FindComponents<OmniUnsavedChangesGuard>());
    }

    [Fact]
    public void TemplateForm_PutsIdClassAndAttributesOnTheForm()
    {
        var form = Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, new GuardedModel())
            .Add(component => component.Id, "profil")
            .Add(component => component.Class, "profil-form")
            .AddUnmatched("aria-label", "Profil")
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => _ => { })));

        var element = form.Find("form");
        Assert.Equal("profil", element.GetAttribute("id"));
        Assert.Contains("profil-form", element.ClassList);
        Assert.Contains("omni-template-form__form", element.ClassList);
        Assert.Equal("Profil", element.GetAttribute("aria-label"));
        Assert.Contains("omni-template-form", form.Find("div").ClassList);
        Assert.Throws<InvalidOperationException>(() => Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, new GuardedModel())
            .AddUnmatched("style", "color: red")
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => _ => { }))));
    }

    // ---- selectable card ------------------------------------------------------------------------

    [Fact]
    public void SelectableCard_SingleChoice_IsARadioThatAlwaysReportsItsPick()
    {
        var picked = new List<bool>();
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Linux")
            .Add(component => component.Description, "Debian, Ubuntu")
            .Add(component => component.Value, true)
            .Add(component => component.ValueChanged, value => picked.Add(value)));

        var button = card.Find("button.omni-selectable-card");
        Assert.Equal("radio", button.GetAttribute("role"));
        Assert.Equal("true", button.GetAttribute("aria-checked"));
        Assert.Contains("omni-selectable-card--selected", button.ClassList);
        Assert.Equal("Linux", card.Find(".omni-selectable-card__title").TextContent);
        Assert.Equal("Debian, Ubuntu", card.Find(".omni-selectable-card__description").TextContent);
        // No check mark: the choice is the frame, so the text never moves.
        Assert.Empty(card.FindAll("svg"));

        button.Click();
        Assert.Equal([true], picked);
    }

    [Fact]
    public void SelectableCard_Multiple_IsACheckBoxThatToggles_AndDisabledStaysFocusableButInert()
    {
        var picked = new List<bool>();
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Docker")
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, true)
            .Add(component => component.ValueChanged, value => picked.Add(value)));

        Assert.Equal("checkbox", card.Find("button").GetAttribute("role"));
        card.Find("button").Click();
        Assert.Equal([false], picked);

        card.Render(parameters => parameters.Add(component => component.Disabled, true));
        Assert.Equal("true", card.Find("button").GetAttribute("aria-disabled"));
        Assert.False(card.Find("button").HasAttribute("disabled"));
        card.Find("button").Click();
        Assert.Equal([false], picked);
    }

    [Fact]
    public void Stylesheet_SelectableCard_KeepsOneFrameWidthInEveryState()
    {
        Assert.Equal("2px solid var(--omni-color-border)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-selectable-card"), "border"));
        var selected = ShippedLookTests.Body(".omni-selectable-card.omni-selectable-card--selected");
        Assert.Equal("var(--omni-color-accent)", ShippedLookTests.Value(selected, "border-color"));
        Assert.Equal("var(--omni-color-accent-subtle)", ShippedLookTests.Value(selected, "background"));
        Assert.DoesNotContain("border-width", selected, StringComparison.Ordinal);
    }

    // ---- check box and switch -------------------------------------------------------------------

    [Fact]
    public void CheckBox_WithText_PutsClassOnTheLabel_AndTextFirstOrdersTheLine()
    {
        var value = false;
        var box = Render<OmniCheckBox<bool>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Class, "host-row")
            .Add(component => component.TextFirst, true)
            .AddChildContent("Se souvenir de moi"));

        var label = box.Find("label");
        Assert.Contains("omni-checkbox-label", label.ClassList);
        Assert.Contains("omni-checkbox-label--text-first", label.ClassList);
        Assert.Contains("host-row", label.ClassList);
        Assert.DoesNotContain("host-row", box.Find("input").ClassList);
        Assert.Contains("omni-checkbox", box.Find("input").ClassList);
    }

    [Fact]
    public void CheckBox_WithoutText_KeepsClassOnTheInput()
    {
        var value = false;
        var box = Render<OmniCheckBox<bool>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Class, "host-box"));

        Assert.Contains("host-box", box.Find("input").ClassList);
    }

    [Fact]
    public void Switch_TextFirst_IsOnlyDrawnWithAText()
    {
        var value = false;
        var withText = Render<OmniSwitch<bool>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.TextFirst, true)
            .AddChildContent("Suivre"));
        Assert.Contains("omni-switch--text-first", withText.Find("button").ClassList);

        var bare = Render<OmniSwitch<bool>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.TextFirst, true)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["aria-label"] = "Suivre" }));
        Assert.DoesNotContain("omni-switch--text-first", bare.Find("button").ClassList);
    }

    [Fact]
    public void Stylesheet_CheckBoxText_IsTrimmedToItsLettersAndTextFirstMovesTheBox()
    {
        Assert.Equal("trim-both cap alphabetic", ShippedLookTests.Value(ShippedLookTests.Body(".omni-checkbox-label__text"), "text-box"));
        Assert.Equal("1lh", ShippedLookTests.Value(DialogIntentAndOverflowMenuTests.BodyWith(".omni-checkbox-label", "min-block-size"), "min-block-size"));
        Assert.Equal("1", ShippedLookTests.Value(ShippedLookTests.Body(".omni-checkbox-label--text-first > .omni-checkbox"), "order"));
        Assert.Equal("1", ShippedLookTests.Value(ShippedLookTests.Body(".omni-switch--text-first > .omni-switch__track"), "order"));
    }

    // ---- pages ----------------------------------------------------------------------------------

    [Fact]
    public void FormActions_StayInTheFormOfTheSignInCard_AlignedToTheEnd()
    {
        // recette R-375: the sign-in button sits in the host's form, so it submits it, at the end of the row.
        RenderFragment form = builder =>
        {
            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "id", "sign-in");
            builder.OpenComponent<OmniFormActions>(2);
            builder.AddComponentParameter(3, nameof(OmniFormActions.Id), "sign-in-actions");
            builder.AddComponentParameter(4, nameof(OmniFormActions.ChildContent), (RenderFragment)(button => button.AddMarkupContent(0, "<button type=\"submit\">Se connecter</button>")));
            builder.CloseComponent();
            builder.CloseElement();
        };
        var shell = Render<OmniLoginShell>(parameters => parameters.Add(component => component.ChildContent, form));

        var row = shell.Find(".omni-card__body form#sign-in > div.omni-form-actions");
        Assert.Equal("sign-in-actions", row.Id);
        Assert.Equal("submit", row.QuerySelector("button")!.GetAttribute("type"));
        Assert.Empty(shell.FindAll(".omni-card__footer .omni-form-actions"));

        var rule = ShippedLookTests.Body(".omni-form-actions");
        Assert.Equal("flex", ShippedLookTests.Value(rule, "display"));
        Assert.Equal("end", ShippedLookTests.Value(rule, "justify-content"));
        Assert.Equal("wrap", ShippedLookTests.Value(rule, "flex-wrap"));
    }

    [Fact]
    public void LoginShell_SignUpHref_LeadsTheFooterWithADiscreetTextLink()
    {
        var shell = Render<OmniLoginShell>(parameters => parameters
            .Add(component => component.SignUpHref, "/inscription")
            .Add(component => component.Footer, builder => builder.AddMarkupContent(0, "<a class=\"forgot\" href=\"/oubli\">Mot de passe oublié</a>")));

        var line = shell.Find(".omni-card__footer > .omni-login-shell__sign-up");
        Assert.Equal("Pas de compte ?", line.QuerySelector("span")!.TextContent);
        var link = line.QuerySelector("a.omni-link.omni-login-shell__sign-up-link")!;
        Assert.Equal("/inscription", link.GetAttribute("href"));
        Assert.Equal("S'inscrire", link.TextContent.Trim());
        Assert.Empty(shell.FindAll(".omni-card__footer .omni-button"));
        // The host's footer follows the sign-up line.
        Assert.Equal("forgot", line.NextElementSibling!.ClassName);
    }

    [Fact]
    public void LoginShell_WithoutSignUp_KeepsItsFooterAsBefore()
    {
        var shell = Render<OmniLoginShell>();
        Assert.Empty(shell.FindAll(".omni-card__footer"));

        var custom = Render<OmniLoginShell>(parameters => parameters
            .Add(component => component.SignUpContent, builder => builder.AddContent(0, "Demander un accès")));
        Assert.Equal("Demander un accès", custom.Find(".omni-login-shell__sign-up").TextContent);
    }

    [Fact]
    public void PageHeader_BackVariant_ColoursTheBackButton_PrimaryByDefault()
    {
        // Owner decision (R-395): the same blue back arrow on every site; Ghost stays available.
        var blue = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Détail")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.ShowBack, true));
        Assert.Contains("omni-button--primary", blue.Find(".omni-page-header__back").ClassList);

        var plain = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Détail")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.ShowBack, true)
            .Add(component => component.BackVariant, OmniButtonVariant.Ghost));
        Assert.Contains("omni-button--ghost", plain.Find(".omni-page-header__back").ClassList);
    }

    [Fact]
    public void DescriptionItem_Icon_LeadsTheLabelAndIsDecorative()
    {
        var item = Render<OmniDescriptionItem>(parameters => parameters
            .Add(component => component.Label, "Version")
            .Add(component => component.Icon, builder =>
            {
                builder.OpenComponent<OmniIcon>(0);
                builder.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Tag);
                builder.CloseComponent();
            })
            .AddChildContent("1.2.0"));

        var icon = item.Find("dt.omni-description-list__label > .omni-description-list__icon");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Equal("Version", item.Find("dt").TextContent.Trim());
        Assert.Empty(Render<OmniDescriptionItem>(parameters => parameters.Add(component => component.Label, "Nom")).FindAll(".omni-description-list__icon"));
    }

    [Fact]
    public void CodeViewer_FirstLineNumber_NumbersAnExcerptAndItsHighlights()
    {
        var viewer = Render<OmniCodeViewer>(parameters => parameters
            .Add(component => component.Code, "a\nb\nc")
            .Add(component => component.FirstLineNumber, 631)
            .Add(component => component.HighlightedLines, [632]));

        Assert.Equal(["631", "632", "633"], viewer.FindAll(".omni-code-viewer__number").Select(number => number.TextContent));
        var highlighted = Assert.Single(viewer.FindAll(".omni-code-viewer__line--highlighted"));
        Assert.Equal("b", highlighted.QuerySelector(".omni-code-viewer__text")!.TextContent.Trim());
    }

    [Fact]
    public void CodeViewer_FirstLineNumberBelowOne_StartsAtOne()
    {
        var viewer = Render<OmniCodeViewer>(parameters => parameters
            .Add(component => component.Code, "a\nb")
            .Add(component => component.FirstLineNumber, 0));

        Assert.Equal(["1", "2"], viewer.FindAll(".omni-code-viewer__number").Select(number => number.TextContent));
    }

    // ---- appearance window ----------------------------------------------------------------------

    [Fact]
    public void AppearanceWindow_ShowsOnlyTheRowsTheHostBinds_ThemeAndPaletteFirst()
    {
        OmniThemePreset? theme = OmniThemePresets.All[0];
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.PresetChanged, value => theme = value)
            .Add(component => component.PaletteChanged, _ => { }));

        Assert.Equal("Apparence", window.Find(".omni-dialog__title").TextContent);
        Assert.Equal("false", window.Find(".omni-dialog").GetAttribute("aria-modal"));
        var look = window.FindAll(".omni-appearance-settings--window .omni-appearance-settings__row");
        Assert.Equal(2, look.Count);
        Assert.Contains("Thème", look[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Palette", look[1].TextContent, StringComparison.Ordinal);
        Assert.Empty(window.FindAll("input[type=range], [role=radiogroup]"));

        var withScales = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityChanged, _ => { }));
        Assert.Empty(withScales.FindAll("select"));
        Assert.Equal(2, withScales.FindAll(".omni-appearance-settings--window .omni-appearance-settings__row").Count);
    }

    [Fact]
    public void AppearanceSettings_Window_NowHoldsThemeAndPaletteAboveTheScales()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.PresetChanged, _ => { })
            .Add(component => component.PaletteChanged, _ => { })
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityChanged, _ => { }));

        settings.Find(".omni-appearance-settings__row--scale button").Click();

        var names = settings.FindAll(".omni-appearance-window .omni-appearance-settings--window .omni-appearance-settings__label").Select(label => label.TextContent.Trim());
        Assert.Equal(["Thème", "Palette", "Densité", "Taille du texte"], names);
    }

    // ---- grid loading bar -----------------------------------------------------------------------

    [Fact]
    public void Grid_Loading_DrawsABarBetweenTheHeadersAndTheRows_UnlessTurnedOff()
    {
        var grid = Render<OmniDataGrid<string>>(parameters => parameters
            .Add(component => component.Items, ["a", "b"])
            .Add(component => component.Busy, true));

        var row = grid.Find("thead > tr.omni-data-grid__progress");
        Assert.Equal("true", row.GetAttribute("aria-hidden"));
        Assert.Contains("omni-data-grid__progress", grid.Find("thead").LastElementChild!.ClassList);
        var bar = row.QuerySelector(".omni-loading-bar")!;
        Assert.Contains("omni-loading-bar--sweep", bar.ClassList);
        Assert.NotNull(bar.QuerySelector(".omni-loading-bar__track > .omni-loading-bar__indicator"));

        grid.Render(parameters => parameters.Add(component => component.LoadingBarMode, OmniLoadingBarMode.Continuous));
        Assert.Contains("omni-loading-bar--continuous", grid.Find(".omni-data-grid__progress .omni-loading-bar").ClassList);

        grid.Render(parameters => parameters.Add(component => component.ShowLoadingBar, false));
        Assert.Empty(grid.FindAll(".omni-data-grid__progress"));

        grid.Render(parameters => parameters
            .Add(component => component.ShowLoadingBar, true)
            .Add(component => component.Busy, false));
        Assert.Empty(grid.FindAll(".omni-data-grid__progress"));
    }

    [Fact]
    public void Grid_RemoteLoad_ShowsTheBarWhileTheRequestRuns()
    {
        var pending = new TaskCompletionSource<OmniDataGridResult<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.PageSize, 10)
            .Add(component => component.Load, _ => pending.Task));

        grid.WaitForAssertion(() => Assert.Single(grid.FindAll(".omni-data-grid__progress")));
        pending.SetResult(new OmniDataGridResult<int>([1, 2], 2));
        grid.WaitForAssertion(() => Assert.Empty(grid.FindAll(".omni-data-grid__progress")));
    }

    // ---- stylesheet -----------------------------------------------------------------------------

    [Fact]
    public void Stylesheet_ProgressAndLoadingBars_ShareOneTwoPixelThickness()
    {
        Assert.Equal("2px", ShippedLookTests.Value(DialogIntentAndOverflowMenuTests.BodyWith(":root", "--omni-progress-thickness"), "--omni-progress-thickness"));
        Assert.Equal("var(--omni-progress-thickness, 2px)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-progress__track"), "height"));
        Assert.Equal("var(--omni-progress-thickness, 2px)", ShippedLookTests.Value(DialogIntentAndOverflowMenuTests.BodyWith(".omni-loading-bar", "--omni-loading-bar-thickness"), "--omni-loading-bar-thickness"));
    }

    [Fact]
    public void Stylesheet_CardActionsSitAtTheEnd_ButASignInFooterReadsAsText()
    {
        var footer = DialogIntentAndOverflowMenuTests.BodyWith(".omni-card__footer", "justify-content");
        Assert.Equal("flex-end", ShippedLookTests.Value(footer, "justify-content"));
        Assert.Equal("flex", ShippedLookTests.Value(footer, "display"));
        Assert.Equal("block", ShippedLookTests.Value(ShippedLookTests.Body(".omni-login-shell__card > .omni-card__footer"), "display"));
    }

    [Fact]
    public void Stylesheet_WizardFoot_SticksToTheBottomAndThePageGrowsTheBody()
    {
        var foot = ShippedLookTests.Body(".omni-wizard__foot");
        Assert.Equal("sticky", ShippedLookTests.Value(foot, "position"));
        Assert.Equal("0", ShippedLookTests.Value(foot, "inset-block-end"));
        Assert.Equal("auto", ShippedLookTests.Value(foot, "margin-block-start"));
        Assert.Equal("1 0 auto", ShippedLookTests.Value(ShippedLookTests.Body(".omni-wizard__body"), "flex"));
    }

    [Fact]
    public void Wizard_ActionsSitInTheStickyFoot()
    {
        var wizard = Render<WizardTestHost>();
        Assert.NotNull(wizard.Find(".omni-wizard > .omni-wizard__foot > .omni-wizard__actions .omni-wizard__next"));
    }

    [Fact]
    public void Stylesheet_EveryActionOfAGridRow_TakesTheBadgeHeight()
    {
        var rule = ShippedLookTests.Body(".omni-data-grid__row > td :is(.omni-button, .omni-split-button)");
        Assert.Equal("var(--omni-badge-height)", ShippedLookTests.Value(rule, "--omni-button-size"));
        Assert.Equal("var(--omni-font-size-sm)", ShippedLookTests.Value(rule, "--omni-button-font-size"));
    }

    private static RenderFragment Guard(bool hasChanges) => builder =>
    {
        builder.OpenComponent<OmniUnsavedChangesGuard>(0);
        builder.AddComponentParameter(1, nameof(OmniUnsavedChangesGuard.HasChanges), hasChanges);
        builder.CloseComponent();
    };

    public sealed class GuardedModel
    {
        public string? Name { get; set; }

        public GuardedAddress Address { get; set; } = new();

        public List<string> Tags { get; set; } = [];
    }

    public sealed class GuardedAddress
    {
        public string? City { get; set; }
    }
}
