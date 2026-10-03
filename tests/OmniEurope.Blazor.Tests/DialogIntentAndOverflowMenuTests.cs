using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The intention of a dialog (tinted header and footer, a round mark before the title, a discreet close
/// button) and the "⋮" overflow menu (borderless trigger, vertical menu, focus given back).
/// </summary>
public sealed class DialogIntentAndOverflowMenuTests : OmniBunitContext
{
    private const string FocusModule = Internal.OmniModules.Focus;

    // ---- dialog intention -----------------------------------------------------------------------

    [Theory]
    [InlineData(OmniTone.Accent, "omni-dialog--intent-accent", OmniSeverity.Info)]
    [InlineData(OmniTone.Info, "omni-dialog--intent-info", OmniSeverity.Info)]
    [InlineData(OmniTone.Success, "omni-dialog--intent-success", OmniSeverity.Success)]
    [InlineData(OmniTone.Warning, "omni-dialog--intent-warning", OmniSeverity.Warning)]
    [InlineData(OmniTone.Danger, "omni-dialog--intent-danger", OmniSeverity.Danger)]
    public void Dialog_WithAnIntention_TintsItsBandsAndLeadsTheTitleWithTheGlyphOfTheSameSeverity(OmniTone intent, string expectedClass, OmniSeverity severity)
    {
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Annuler l'exécution")
            .Add(component => component.Intent, intent)
            .AddChildContent("Continuer ?"));

        Assert.Contains(expectedClass, dialog.Find(".omni-dialog").ClassList);
        var mark = dialog.Find(".omni-dialog__header > .omni-dialog__intent");
        Assert.Equal("true", mark.GetAttribute("aria-hidden"));
        // The glyph is the one the alert and the notification draw for the severity of the same name.
        var alert = Render<OmniAlert>(parameters => parameters.Add(component => component.Severity, severity).AddChildContent("x"));
        Assert.Equal(alert.Find(".omni-alert__glyph path").GetAttribute("d"), mark.QuerySelector("svg.omni-dialog__glyph path")!.GetAttribute("d"));
        // The mark leads the title and does not enter the heading that names the dialog.
        Assert.Equal("omni-dialog__title", mark.NextElementSibling!.ClassName);
        Assert.Equal("Annuler l'exécution", dialog.Find(".omni-dialog__title").TextContent);
    }

    [Fact]
    public void Dialog_IntentionIcon_CanBeReplaced()
    {
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Paramètres d'exécution")
            .Add(component => component.Intent, OmniTone.Accent)
            .Add(component => component.Icon, builder => builder.AddMarkupContent(0, "<span class=\"host-icon\"></span>")));

        Assert.NotNull(dialog.Find(".omni-dialog__intent .host-icon"));
        Assert.Empty(dialog.FindAll(".omni-dialog__intent svg"));
    }

    [Fact]
    public void Dialog_WithoutIntention_HasNoMarkAndNoIntentClass()
    {
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Confirmation")
            .Add(component => component.Icon, builder => builder.AddContent(0, "ignored")));

        Assert.Empty(dialog.FindAll(".omni-dialog__intent"));
        Assert.DoesNotContain("intent", dialog.Find(".omni-dialog").ClassName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(OmniButtonVariant.Danger, "omni-dialog--intent-warning")]
    [InlineData(OmniButtonVariant.Primary, "omni-dialog--intent-accent")]
    public void ConfirmAsync_DerivesTheIntentionFromTheActionUnlessGiven(OmniButtonVariant variant, string expectedClass)
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        _ = service.ConfirmAsync(new OmniConfirmRequest("Annuler l'exécution", "Ses étapes en cours seront arrêtées.") { ConfirmVariant = variant });

        host.WaitForAssertion(() => Assert.Contains(expectedClass, host.Find(".omni-dialog").ClassList));
        service.CloseDialog(false);

        _ = service.ConfirmAsync(new OmniConfirmRequest("Publier", "Publier la page ?") { ConfirmVariant = variant, Intent = OmniTone.Danger });
        host.WaitForAssertion(() => Assert.Contains("omni-dialog--intent-danger", host.Find(".omni-dialog").ClassList));
    }

    [Fact]
    public void DialogRequest_PassesItsIntentionToTheDialog()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Créer une alerte", builder => builder.AddContent(0, "Formulaire")) { Intent = OmniTone.Accent });

        host.WaitForAssertion(() => Assert.Contains("omni-dialog--intent-accent", host.Find(".omni-dialog").ClassList));
        Assert.Equal(OmniTone.Neutral, new OmniDialogRequest("Titre", _ => { }).Intent);
    }

    [Fact]
    public void Stylesheet_BandsAreOneHeightAndTheCloseButtonIsDiscreet()
    {
        var header = BodyWith(".omni-dialog__header", "min-block-size");
        Assert.Equal("calc(var(--omni-control-height) + 2 * (var(--omni-card-pad) - 2px))", ShippedLookTests.Value(header, "min-block-size"));
        var close = ShippedLookTests.Body(".omni-dialog .omni-dialog__close");
        Assert.Equal("transparent", ShippedLookTests.Value(close, "background"));
        Assert.Equal("var(--omni-control-height)", ShippedLookTests.Value(close, "block-size"));
        Assert.Equal("0", ShippedLookTests.Value(close, "border"));
        // The drawn button is smaller, its target keeps the audited 44 px.
        Assert.Equal("min(0px, calc((var(--omni-control-height) - 2.75rem) / 2))", ShippedLookTests.Value(ShippedLookTests.Body(".omni-dialog .omni-dialog__close::before"), "inset"));
        // The bands stay neutral whatever the intention (Aetheus R2-029): only the mark takes its tint.
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.Contains("omni-dialog--intent", StringComparison.Ordinal)
            && rule.Selector.Contains("omni-dialog__footer", StringComparison.Ordinal));
        Assert.StartsWith("var(--omni-dialog-band,", ShippedLookTests.Value(ShippedLookTests.Body(".omni-dialog__intent"), "background"), StringComparison.Ordinal);
        Assert.Equal("var(--omni-color-warning-subtle)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-dialog--intent-warning"), "--omni-dialog-band"));
        Assert.Equal("var(--omni-color-accent-subtle)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-dialog--intent-accent"), "--omni-dialog-band"));
    }

    // ---- overflow menu --------------------------------------------------------------------------

    [Fact]
    public void OverflowMenu_Trigger_IsABorderlessThreeDotButtonThatAnnouncesItsMenu()
    {
        var menu = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.ChildContent, Items(_ => { })));

        var trigger = menu.Find("button.omni-overflow-menu__trigger");
        Assert.Contains("omni-button--ghost", trigger.ClassList);
        Assert.Equal("menu", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Equal("Plus d'actions", trigger.GetAttribute("aria-label"));
        Assert.NotNull(trigger.QuerySelector("svg.omni-icon"));
        Assert.Empty(menu.FindAll("[role=menu]"));
    }

    [Fact]
    public void OverflowMenu_Click_OpensAVerticalMenuAndPlacesItThroughTheScript()
    {
        var module = JSInterop.SetupModule(FocusModule);
        var menu = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.Id, "row-actions")
            .Add(component => component.Label, "Actions du pipeline")
            .Add(component => component.ChildContent, Items(_ => { })));

        menu.Find(".omni-overflow-menu__trigger").Click();

        var popup = menu.Find("#row-actions-menu");
        Assert.Equal("menu", popup.GetAttribute("role"));
        Assert.Equal("Actions du pipeline", popup.GetAttribute("aria-label"));
        Assert.Equal("true", menu.Find(".omni-overflow-menu__trigger").GetAttribute("aria-expanded"));
        Assert.Equal("row-actions-menu", menu.Find(".omni-overflow-menu__trigger").GetAttribute("aria-controls"));
        var items = menu.FindAll("[role=menuitem]");
        Assert.Equal(["Modifier", "Supprimer"], items.Select(item => item.QuerySelector(".omni-menu__label")!.TextContent));
        Assert.All(items, item => Assert.Equal("-1", item.GetAttribute("tabindex")));
        // The rows are menu rows, not buttons: the grid's button rules cannot size them.
        Assert.All(items, item => Assert.DoesNotContain("omni-button", item.ClassList));
        menu.WaitForAssertion(() => Assert.Single(module.Invocations, call => call.Identifier == "openMenu"));
        Assert.Equal("row-actions-menu", module.Invocations.First(call => call.Identifier == "openMenu").Arguments[0]);
    }

    [Fact]
    public void EveryMenu_IsNeverTallerThanTheWindow_AndScrollsInsideItself()
    {
        // Astraia recette: 25 commands in the editor's "⋮" menu ran below a 720px window, out of reach.
        var menu = ShippedLookTests.Rules().Last(rule => rule.Selector == ".omni-menu" && rule.Body.Contains("max-block-size", StringComparison.Ordinal)).Body;
        // The dynamic window height, after a static fallback for browsers without dvh.
        Assert.Contains("max-block-size: calc(100dvh - 16px)", menu, StringComparison.Ordinal);
        Assert.Equal("auto", ShippedLookTests.Value(menu, "overflow-y"));
        Assert.Equal("contain", ShippedLookTests.Value(menu, "overscroll-behavior"));
    }

    [Fact]
    public void OverflowMenu_Item_ClosesTheMenuWithTheFocusBackBeforeItsActionRuns()
    {
        var module = JSInterop.SetupModule(FocusModule);
        var calls = new List<string>();
        var menu = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.ChildContent, Items(label => calls.Add($"{label}:{module.Invocations.Count(call => call.Identifier == "closeMenu")}"))));

        menu.Find(".omni-overflow-menu__trigger").Click();
        menu.WaitForAssertion(() => Assert.Single(module.Invocations, call => call.Identifier == "openMenu"));
        menu.Find(".omni-menu__item").Click();

        // The menu was closed (focus restored) once when the action ran.
        Assert.Equal(["Modifier:1"], calls);
        var close = module.Invocations.Single(call => call.Identifier == "closeMenu");
        Assert.Equal(true, close.Arguments[1]);
        Assert.Empty(menu.FindAll("[role=menu]"));
        Assert.Equal("false", menu.Find(".omni-overflow-menu__trigger").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void OverflowMenu_TriggerClickedAgain_ClosesTheMenu()
    {
        JSInterop.SetupModule(FocusModule);
        var menu = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.ChildContent, Items(_ => { })));

        menu.Find(".omni-overflow-menu__trigger").Click();
        Assert.Single(menu.FindAll("[role=menu]"));
        menu.Find(".omni-overflow-menu__trigger").Click();
        Assert.Empty(menu.FindAll("[role=menu]"));
    }

    [Fact]
    public void OverflowMenu_DisabledItem_IsSkippedAndRunsNothing()
    {
        JSInterop.SetupModule(FocusModule);
        var ran = false;
        var menu = Render<OmniOverflowMenu>(parameters => parameters
            .Add(component => component.ChildContent, builder =>
            {
                builder.OpenComponent<OmniMenuItem>(0);
                builder.AddComponentParameter(1, nameof(OmniMenuItem.Disabled), true);
                builder.AddComponentParameter(2, nameof(OmniMenuItem.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => ran = true));
                builder.AddComponentParameter(3, nameof(OmniMenuItem.ChildContent), (RenderFragment)(label => label.AddContent(0, "Publier")));
                builder.CloseComponent();
            }));

        menu.Find(".omni-overflow-menu__trigger").Click();
        var item = menu.Find("[role=menuitem]");
        Assert.True(item.HasAttribute("disabled"));
        Assert.False(ran);
    }

    [Fact]
    public void OverflowMenu_InsideAHost_IsDrawnInTheOverlayPortal()
    {
        JSInterop.SetupModule(FocusModule);
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniOverflowMenu>(0);
                builder.AddComponentParameter(1, nameof(OmniOverflowMenu.ChildContent), Items(_ => { }));
                builder.CloseComponent();
            }));

        host.Find(".omni-overflow-menu__trigger").Click();

        host.WaitForAssertion(() => Assert.NotNull(host.Find(".omni-overlay-portal__entry--overflowmenu .omni-overflow-menu__popup")));
        Assert.Empty(host.FindAll(".omni-overflow-menu > .omni-overflow-menu__popup"));
    }

    [Fact]
    public void Stylesheet_OverflowMenu_HasNoFrameAtRestAndAFixedIconColumn()
    {
        Assert.Equal("transparent", ShippedLookTests.Value(ShippedLookTests.Body(".omni-overflow-menu > .omni-overflow-menu__trigger"), "border-color"));
        var popup = BodyWith(".omni-menu", "backdrop-filter");
        Assert.Equal("fixed", ShippedLookTests.Value(popup, "position"));
        Assert.Equal("var(--omni-menu-x, var(--omni-space-md))", ShippedLookTests.Value(popup, "left"));
        var icon = ShippedLookTests.Body(".omni-menu__icon");
        Assert.Equal("1.25rem", ShippedLookTests.Value(icon, "inline-size"));
        Assert.Equal("none", ShippedLookTests.Value(icon, "flex"));
    }

    internal static string BodyWith(string selector, string declaration) =>
        ShippedLookTests.Rules().First(rule => rule.Selector == selector && rule.Body.Contains(declaration, StringComparison.Ordinal)).Body;

    private RenderFragment Items(Action<string> clicked) => builder =>
    {
        AddItem(builder, 0, "Modifier", clicked);
        AddItem(builder, 10, "Supprimer", clicked);
    };

    private void AddItem(RenderTreeBuilder builder, int sequence, string label, Action<string> clicked)
    {
        builder.OpenComponent<OmniMenuItem>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniMenuItem.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => clicked(label)));
        builder.AddComponentParameter(sequence + 2, nameof(OmniMenuItem.Icon), (RenderFragment)(icon =>
        {
            icon.OpenComponent<OmniIcon>(0);
            icon.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Edit);
            icon.CloseComponent();
        }));
        builder.AddComponentParameter(sequence + 3, nameof(OmniMenuItem.ChildContent), (RenderFragment)(text => text.AddContent(0, label)));
        builder.CloseComponent();
    }
}
