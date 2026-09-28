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
    public void Guard_WithChanges_AsksBeforeAnInternalNavigation_AndStayKeepsThePage()
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
        Assert.Equal(start, navigation.Uri);
        Assert.Equal(NavigationState.Prevented, Assert.Single(((BunitNavigationManager)navigation).History).State);
    }

    [Fact]
    public void Guard_WithChanges_LetsTheNavigationThroughOnceConfirmed()
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
        host.WaitForAssertion(() => Assert.EndsWith("/ailleurs", navigation.Uri, StringComparison.Ordinal), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Guard_WithoutChanges_NeverAsks()
    {
        using var service = new OmniOverlayService();
        var navigation = Services.GetRequiredService<NavigationManager>();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent(Guard(hasChanges: false)));

        _ = host.InvokeAsync(() => navigation.NavigateTo("/ailleurs"));

        host.WaitForAssertion(() => Assert.EndsWith("/ailleurs", navigation.Uri, StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        Assert.Empty(host.FindAll(".omni-dialog"));
    }

    [Fact]
    public void TemplateForm_GuardsOnceAFieldChanges_AndAValidSubmitReleasesIt()
    {
        var model = new GuardedModel();
        var form = Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, model)
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
        form.InvokeAsync(() => editContext.NotifyFieldChanged(editContext.Field(nameof(GuardedModel.Name))));
        Assert.True(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);

        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.False(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges));
    }

    [Fact]
    public void TemplateForm_GuardCanBeTurnedOff()
    {
        var form = Render<OmniTemplateForm<GuardedModel>>(parameters => parameters
            .Add(component => component.Model, new GuardedModel())
            .Add(component => component.GuardUnsavedChanges, false)
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => _ => { })));

        Assert.Empty(form.FindComponents<OmniUnsavedChangesGuard>());
    }

    // ---- selectable card ------------------------------------------------------------------------

    [Fact]
    public void SelectableCard_SingleChoice_IsARadioThatAlwaysReportsItsPick()
    {
        var picked = new List<bool>();
        var card = Render<OmniSelectableCard>(parameters => parameters
            .Add(component => component.Title, "Linux")
            .Add(component => component.Description, "Debian, Ubuntu")
            .Add(component => component.Selected, true)
            .Add(component => component.SelectedChanged, value => picked.Add(value)));

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
            .Add(component => component.Selected, true)
            .Add(component => component.SelectedChanged, value => picked.Add(value)));

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
    public void PageHeader_BackVariant_ColoursTheBackButton_GhostByDefault()
    {
        var plain = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Détail")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.ShowBack, true));
        Assert.Contains("omni-button--ghost", plain.Find(".omni-page-header__back").ClassList);

        var blue = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Détail")
            .Add(component => component.ShowTrail, false)
            .Add(component => component.ShowBack, true)
            .Add(component => component.BackVariant, OmniButtonVariant.Primary));
        Assert.Contains("omni-button--primary", blue.Find(".omni-page-header__back").ClassList);
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
        var look = window.FindAll(".omni-appearance-settings--look .omni-appearance-settings__row");
        Assert.Equal(2, look.Count);
        Assert.Contains("Thème", look[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Palette", look[1].TextContent, StringComparison.Ordinal);
        Assert.Empty(window.FindAll(".omni-appearance-settings--scale"));

        var withScales = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityLevelChanged, _ => { }));
        Assert.Empty(withScales.FindAll(".omni-appearance-settings--look"));
        Assert.Equal(2, withScales.FindAll(".omni-appearance-settings--scale .omni-appearance-settings__row").Count);
    }

    [Fact]
    public void AppearanceWindow_WithTheHostsOwnLook_NamesItFirst_AndListsEveryThemeByName()
    {
        OmniThemePreset? theme = OmniThemePresets.All[1];
        OmniThemePalette? palette = OmniThemePalettes.All[1];
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.DefaultThemeText, "Aetheus")
            .Add(component => component.DefaultPaletteText, "Palette du thème")
            .Add(component => component.PresetChanged, value => theme = value)
            .Add(component => component.PaletteChanged, value => palette = value));

        var selects = window.FindAll("select");
        var themes = selects[0].QuerySelectorAll("option").Select(option => option.TextContent).ToList();
        // The host's look comes first, then the whole catalogue, the first theme included by its own name.
        Assert.Equal("Aetheus", themes[0]);
        Assert.Equal(OmniThemePresets.All.Count + 1, themes.Count);
        Assert.Contains(OmniThemePresets.All[0].Name, themes.Skip(1));
        Assert.DoesNotContain(themes, text => text.Contains("(", StringComparison.Ordinal));
        // No theme chosen: the palette list starts with the host's own palette.
        Assert.Equal("Palette du thème", selects[1].QuerySelectorAll("option")[0].TextContent);

        // The drop-down posts the option's own value: pick by the visible name.
        static string ValueOf(AngleSharp.Dom.IElement select, string text) =>
            select.QuerySelectorAll("option").First(option => option.TextContent == text).GetAttribute("value")!;
        selects[0].Change(ValueOf(selects[0], OmniThemePresets.All[0].Name));
        Assert.Same(OmniThemePresets.All[0], theme);
        // The host keeps the value: render the window with the theme it now holds.
        window.Render(parameters => parameters.Add(component => component.Preset, theme));
        var themeSelect = window.FindAll("select")[0];
        themeSelect.Change(ValueOf(themeSelect, "Aetheus"));
        Assert.Null(theme);
        window.Render(parameters => parameters.Add(component => component.Preset, theme).Add(component => component.Palette, OmniThemePalettes.All[1]));
        var paletteSelect = window.FindAll("select")[1];
        paletteSelect.Change(ValueOf(paletteSelect, "Palette du thème"));
        Assert.Null(palette);
    }

    [Fact]
    public void AppearanceSettings_Window_NowHoldsThemeAndPaletteAboveTheScales()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.PresetChanged, _ => { })
            .Add(component => component.PaletteChanged, _ => { })
            .Add(component => component.TextSizeLevelChanged, _ => { })
            .Add(component => component.DensityLevelChanged, _ => { }));

        settings.FindAll(".omni-appearance-settings__row")[4].QuerySelector("button")!.Click();

        Assert.Equal(2, settings.FindAll(".omni-appearance-window .omni-appearance-settings--look .omni-appearance-settings__row").Count);
        Assert.Equal(2, settings.FindAll(".omni-appearance-window .omni-appearance-settings--scale .omni-appearance-settings__row").Count);
    }

    // ---- grid loading bar -----------------------------------------------------------------------

    [Fact]
    public void Grid_Loading_DrawsABarBetweenTheHeadersAndTheRows_UnlessTurnedOff()
    {
        var grid = Render<OmniDataGrid<string>>(parameters => parameters
            .Add(component => component.Items, ["a", "b"])
            .Add(component => component.IsLoading, true));

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
            .Add(component => component.IsLoading, false));
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
    }
}
