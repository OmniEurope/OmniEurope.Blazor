using System.Globalization;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Options added to components other applications already use. Each one is proved twice: left
/// unset, the component renders the markup and runs the behaviour it had before the option
/// existed; set, it does what the option says.
/// </summary>
public sealed class OptInEvolutionTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";
    private const string GridModule = "./_content/OmniEurope.Blazor/omni-grid.js";

    [Fact]
    public void Tabs_WithoutExternalBinding_SwitchTheirVisiblePanel()
    {
        RenderFragment tabs = builder =>
        {
            builder.OpenComponent<OmniTabsItem>(0);
            builder.AddAttribute(1, nameof(OmniTabsItem.Title), "First");
            builder.AddAttribute(2, nameof(OmniTabsItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "First panel")));
            builder.CloseComponent();
            builder.OpenComponent<OmniTabsItem>(3);
            builder.AddAttribute(4, nameof(OmniTabsItem.Title), "Second");
            builder.AddAttribute(5, nameof(OmniTabsItem.ChildContent), (RenderFragment)(content => content.AddContent(0, "Second panel")));
            builder.CloseComponent();
        };
        var cut = Render<OmniTabs>(parameters => parameters.Add(component => component.Tabs, tabs));

        cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll("[role='tab']")[0].GetAttribute("aria-selected")));
        cut.FindAll("[role='tab']")[1].Click();

        Assert.Equal("true", cut.FindAll("[role='tab']")[1].GetAttribute("aria-selected"));
        Assert.Equal("Second panel", cut.FindAll("[role='tabpanel']")[1].TextContent);
        Assert.True(cut.FindAll("[role='tabpanel']")[0].HasAttribute("hidden"));
        Assert.False(cut.FindAll("[role='tabpanel']")[1].HasAttribute("hidden"));
    }

    // ---- dialogs ---------------------------------------------------------------------------------

    [Fact]
    public void Dialog_WithoutDismissible_RendersTheMarkupItAlwaysHad()
    {
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Id, "confirm")
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Confirmation")
            .Add(component => component.CloseLabel, "Fermer")
            .AddChildContent("Continuer ?"));

        dialog.MarkupMatches("""
            <div class="omni-overlay" role="presentation">
              <section id="confirm" class="omni-dialog" role="dialog" aria-modal="true" aria-labelledby="confirm-title">
                <button type="button" class="omni-visually-hidden" data-focus-sentinel aria-label="Fin du dialogue"></button>
                <header class="omni-dialog__header">
                  <h2 id="confirm-title" class="omni-dialog__title">Confirmation</h2>
                  <button type="button" class="omni-dialog__close" aria-label="Fermer" autofocus>×</button>
                </header>
                <div class="omni-dialog__content" tabindex="-1">Continuer ?</div>
                <button type="button" class="omni-visually-hidden" data-focus-sentinel aria-label="Début du dialogue"></button>
              </section>
            </div>
            """);
    }

    [Fact]
    public void DialogRequest_WithoutTheNewOptions_ClosesOnBackdropEscapeAndItsButtonAsBefore()
    {
        var request = new OmniDialogRequest("Confirmation", Content("Continuer ?"));
        Assert.True(request.CloseOnBackdropClick);
        Assert.True(request.Dismissible);

        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(request);
        host.WaitForAssertion(() => Assert.Equal("dialog", host.Find(".omni-dialog").GetAttribute("role")));
        Assert.False(host.Find(".omni-dialog").HasAttribute("tabindex"));
        Assert.False(host.Find(".omni-dialog").HasAttribute("aria-describedby"));
        Assert.False(host.Find(".omni-dialog__content").HasAttribute("id"));
        host.Find(".omni-overlay").Click();
        Assert.Null(service.Dialog);

        service.OpenDialog(new OmniDialogRequest("Confirmation", Content("Continuer ?")));
        host.WaitForAssertion(() => Assert.Single(host.FindAll(".omni-dialog")));
        host.Find(".omni-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(service.Dialog);

        service.OpenDialog(new OmniDialogRequest("Confirmation", Content("Continuer ?")));
        host.WaitForAssertion(() => Assert.Single(host.FindAll(".omni-dialog")));
        host.Find(".omni-dialog__close").Click();
        Assert.Null(service.Dialog);
    }

    [Fact]
    public void Dialog_WithABackdropThatCloses_ActivatesTheFocusTrapWithTheCallItAlwaysMade()
    {
        var module = JSInterop.SetupModule(FocusModule);

        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Confirmation"));

        dialog.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));
        Assert.Equal(2, module.Invocations["activateDialog"][0].Arguments.Count);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void DialogRequest_WithABackdropThatClosesNothing_AsksTheTrapToHoldFocusOnIt(bool closeOnBackdropClick, bool dismissible)
    {
        var module = JSInterop.SetupModule(FocusModule);
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Confirmation", Content("Continuer ?"))
        {
            CloseOnBackdropClick = closeOnBackdropClick,
            Dismissible = dismissible
        });

        host.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));
        var arguments = module.Invocations["activateDialog"][0].Arguments;
        Assert.Equal(3, arguments.Count);
        Assert.Equal(true, arguments[2]);
    }

    [Fact]
    public void DialogRequest_CloseOnBackdropClickFalse_IgnoresTheBackdropOnly()
    {
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Confirmation", Content("Continuer ?")) { CloseOnBackdropClick = false });
        host.WaitForAssertion(() => Assert.Single(host.FindAll(".omni-dialog")));

        host.Find(".omni-overlay").Click();
        Assert.NotNull(service.Dialog);
        Assert.Single(host.FindAll(".omni-dialog__close"));
        Assert.Equal("dialog", host.Find(".omni-dialog").GetAttribute("role"));

        host.Find(".omni-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(service.Dialog);
    }

    [Fact]
    public async Task DialogRequest_NotDismissible_IsAnAlertDialogThatOnlyItsContentCloses()
    {
        var module = JSInterop.SetupModule(FocusModule);
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        var pending = service.OpenDialogAsync(new OmniDialogRequest("Mise à jour", Content("Installation en cours."))
        {
            Dismissible = false,
            Footer = builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "class", "done");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create(this, () => service.CloseDialog("done")));
                builder.AddContent(4, "Terminer");
                builder.CloseElement();
            }
        });

        host.WaitForAssertion(() => Assert.Single(host.FindAll(".omni-dialog")));
        var dialog = host.Find(".omni-dialog");
        Assert.Equal("alertdialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("-1", dialog.GetAttribute("tabindex"));
        Assert.Equal(host.Find(".omni-dialog__content").Id, dialog.GetAttribute("aria-describedby"));
        Assert.Empty(host.FindAll(".omni-dialog__close"));
        Assert.Equal(2, host.FindAll("[data-focus-sentinel]").Count);
        host.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));

        host.Find(".omni-overlay").Click();
        host.Find(".omni-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.NotNull(service.Dialog);
        Assert.False(pending.IsCompleted);

        host.Find(".done").Click();

        Assert.Equal("done", await pending);
        Assert.Null(service.Dialog);
        host.WaitForAssertion(() => Assert.Empty(host.FindAll(".omni-dialog")));
    }

    [Fact]
    public void Dialog_NotDismissible_OverridesCloseOnEscapeAndCloseOnBackdrop()
    {
        var open = true;
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, open)
            .Add(component => component.OpenChanged, value => open = value)
            .Add(component => component.Title, "Mise à jour")
            .Add(component => component.CloseOnEscape, true)
            .Add(component => component.CloseOnBackdrop, true)
            .Add(component => component.Dismissible, false)
            .AddChildContent("Installation en cours."));

        dialog.Find(".omni-overlay").Click();
        dialog.Find(".omni-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(open);
        Assert.Equal("alertdialog", dialog.Find(".omni-dialog").GetAttribute("role"));
    }

    // ---- numeric ---------------------------------------------------------------------------------

    [Fact]
    public void Numeric_WithoutClamp_KeepsAValueOutsideItsBoundsAsBefore()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            int value = 5;
            var numeric = Render<OmniNumeric<int>>(parameters => parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, next => value = next)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.Minimum, "0")
                .Add(component => component.Maximum, "100"));

            numeric.MarkupMatches("""<input class="omni-input omni-numeric" type="number" value="5" min="0" max="100" />""");

            numeric.Find("input").Change("150");
            Assert.Equal(150, value);
            Assert.Equal("150", numeric.Find("input").GetAttribute("value"));

            numeric.Find("input").Change("-7");
            Assert.Equal(-7, value);
        });
    }

    [Fact]
    public void Numeric_Clamp_BringsAnIntegerBackToTheNearestBound()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            var received = new List<int>();
            var value = 5;
            var numeric = Render<OmniNumeric<int>>(parameters => parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, received.Add)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.Minimum, "0")
                .Add(component => component.Maximum, "100")
                .Add(component => component.Clamp, true));

            numeric.Find("input").Change("150");
            Assert.Equal(100, received[^1]);
            Assert.Equal("100", numeric.Find("input").GetAttribute("value"));

            numeric.Find("input").Change("-7");
            Assert.Equal(0, received[^1]);
            Assert.Equal("0", numeric.Find("input").GetAttribute("value"));

            numeric.Find("input").Change("42");
            Assert.Equal(42, received[^1]);
        });
    }

    [Fact]
    public void Numeric_Clamp_RendersTheTypedTextThenTheBoundSoTheBrowserTextIsReplaced()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            // The value is already the maximum: without the extra render, the second render would
            // carry the same "20" and the browser would keep the "150" the reader typed.
            var value = 20;
            var numeric = Render<OmniNumeric<int>>(parameters => parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, next => value = next)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.Minimum, "0")
                .Add(component => component.Maximum, "20")
                .Add(component => component.Clamp, true));

            var before = numeric.RenderCount;
            numeric.Find("input").Change("150");
            numeric.WaitForAssertion(() => Assert.Equal(before + 2, numeric.RenderCount));
            Assert.Equal("20", numeric.Find("input").GetAttribute("value"));
            Assert.Equal(20, value);

            // Within the bounds nothing is clamped: one render, as without the parameter.
            before = numeric.RenderCount;
            numeric.Find("input").Change("7");
            Assert.Equal(before + 1, numeric.RenderCount);
            Assert.Equal(7, value);
        });
    }

    [Fact]
    public void Numeric_WithoutClamp_RendersOnceForAChangeAsBefore()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            var value = 20;
            var numeric = Render<OmniNumeric<int>>(parameters => parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueChanged, next => value = next)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.Minimum, "0")
                .Add(component => component.Maximum, "20"));

            var before = numeric.RenderCount;
            numeric.Find("input").Change("150");
            Assert.Equal(before + 1, numeric.RenderCount);
            Assert.Equal("150", numeric.Find("input").GetAttribute("value"));
        });
    }

    [Fact]
    public void Numeric_Clamp_WorksForLongDecimalAndDouble()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(9_000_000_000L, ClampThrough<long>("0", "9000000000", "12000000000", 0L));
            Assert.Equal(10.5m, ClampThrough<decimal>("0.5", "10.5", "12.75", 1m));
            Assert.Equal(0.5m, ClampThrough<decimal>("0.5", "10.5", "0.25", 1m));
            Assert.Equal(-1.25d, ClampThrough<double>("-1.25", "1.25", "-3.5", 0d));
            Assert.Equal(0.75d, ClampThrough<double>("-1.25", "1.25", "0.75", 0d));
        });
    }

    [Fact]
    public void Numeric_Clamp_OnNullableTypesKeepsNullAndClampsValues()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(100, ClampThrough<int?>("0", "100", "500", null));
            Assert.Null(ClampThrough<int?>("0", "100", string.Empty, initial: 20));
            Assert.Equal(-2L, ClampThrough<long?>("-2", "2", "-9", null));
            Assert.Equal(2.5m, ClampThrough<decimal?>("0", "2.5", "3", null));
            Assert.Null(ClampThrough<decimal?>("0", "2.5", string.Empty, initial: 1m));
            Assert.Equal(1d, ClampThrough<double?>("0", "1", "1.5", null));
        });
    }

    [Fact]
    public void Numeric_Clamp_IgnoresABoundThatIsMissingOrUnreadable()
    {
        WithCulture(CultureInfo.InvariantCulture, () =>
        {
            Assert.Equal(500, ClampThrough<int>(null, "abc", "500", 0));
            Assert.Equal(0, ClampThrough<int>("0", null, "-4", 5));
        });
    }

    private TValue ClampThrough<TValue>(string? minimum, string? maximum, string typed, TValue initial)
    {
        var value = initial;
        var numeric = Render<OmniNumeric<TValue>>(parameters => parameters
            .Add(component => component.Value, initial)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Minimum, minimum)
            .Add(component => component.Maximum, maximum)
            .Add(component => component.Clamp, true));

        numeric.Find("input").Change(typed);
        return value;
    }

    // ---- split button ----------------------------------------------------------------------------

    [Fact]
    public void SplitButton_WithoutVariantOrIcon_RendersTheMarkupItAlwaysHad()
    {
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Publier")
            .Add(component => component.MenuLabel, "Autres actions"));

        split.MarkupMatches("""
            <div class="omni-split-button omni-split-button--medium">
              <button type="button" class="omni-split-button__main">Publier</button>
              <button type="button" class="omni-split-button__toggle" aria-haspopup="menu" aria-expanded="false" aria-label="Autres actions">
                <svg class="omni-icon omni-icon--small" viewBox:ignore fill="currentColor" focusable="false" aria-hidden="true"><path d:ignore></path></svg>
              </button>
            </div>
            """);
        Assert.Equal("Publier", split.Find(".omni-split-button__main").InnerHtml);
        Assert.Equal(OmniButtonVariant.Secondary, new OmniSplitButton().Variant);
    }

    [Theory]
    [InlineData(OmniButtonVariant.Secondary, null)]
    [InlineData(OmniButtonVariant.Primary, "omni-split-button--primary")]
    [InlineData(OmniButtonVariant.Ghost, "omni-split-button--ghost")]
    [InlineData(OmniButtonVariant.Danger, "omni-split-button--danger")]
    [InlineData(OmniButtonVariant.Success, "omni-split-button--success")]
    [InlineData(OmniButtonVariant.Warning, "omni-split-button--warning")]
    public void SplitButton_Variant_AddsItsModifierExceptTheOriginalSecondary(OmniButtonVariant variant, string? expected)
    {
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Publier")
            .Add(component => component.Variant, variant));

        var classes = split.Find(".omni-split-button").ClassList.ToArray();
        var expectedClasses = expected is null
            ? new[] { "omni-split-button", "omni-split-button--medium" }
            : new[] { "omni-split-button", "omni-split-button--medium", expected };
        Assert.Equal(expectedClasses, classes);
    }

    [Theory]
    [InlineData(OmniControlSize.Small, "omni-icon--small")]
    [InlineData(OmniControlSize.Medium, "omni-icon--medium")]
    [InlineData(OmniControlSize.Large, "omni-icon--medium")]
    public void SplitButton_Icon_IsDrawnBeforeTheTextOfTheMainPartOnly(OmniControlSize size, string iconClass)
    {
        var split = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Publier")
            .Add(component => component.Size, size)
            .Add(component => component.Icon, OmniIconName.RocketLaunch));

        var main = split.Find(".omni-split-button__main");
        var icon = Assert.Single(main.QuerySelectorAll("svg"));
        Assert.Same(icon, main.FirstElementChild);
        Assert.Contains(iconClass, icon.ClassList);
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Equal("Publier", main.TextContent.Trim());
        Assert.EndsWith("Publier", main.InnerHtml, StringComparison.Ordinal);

        var toggleIcon = Assert.Single(split.Find(".omni-split-button__toggle").QuerySelectorAll("svg"));
        Assert.Contains("omni-icon--small", toggleIcon.ClassList);
    }

    // ---- fieldset --------------------------------------------------------------------------------

    [Fact]
    public void Fieldset_WithoutCollapsedChanged_RendersAsBeforeAndRunsNoScript()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;

        var open = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, Content("Avancé"))
            .Add(component => component.Collapsible, true)
            .AddChildContent("Champs"));
        var folded = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, Content("Avancé"))
            .Add(component => component.Collapsible, true)
            .Add(component => component.Collapsed, true)
            .AddChildContent("Champs"));
        var plain = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, Content("Contact"))
            .AddChildContent("Champs"));

        open.MarkupMatches("""
            <fieldset class="omni-fieldset omni-fieldset--collapsible">
              <details open>
                <summary class="omni-fieldset__legend omni-fieldset__summary">Avancé</summary>
                <div class="omni-fieldset__content">Champs</div>
              </details>
            </fieldset>
            """);
        folded.MarkupMatches("""
            <fieldset class="omni-fieldset omni-fieldset--collapsible">
              <details>
                <summary class="omni-fieldset__legend omni-fieldset__summary">Avancé</summary>
                <div class="omni-fieldset__content">Champs</div>
              </details>
            </fieldset>
            """);
        plain.MarkupMatches("""
            <fieldset class="omni-fieldset">
              <legend class="omni-fieldset__legend">Contact</legend>
              <div class="omni-fieldset__content">Champs</div>
            </fieldset>
            """);
        Assert.Empty(JSInterop.Invocations);
    }

    [Fact]
    public async Task Fieldset_CollapsedChanged_ReportsWhatTheReaderDoesButNotWhatTheHostDid()
    {
        var module = JSInterop.SetupModule(FocusModule);
        var host = Render<FieldsetToggleTestHost>();

        host.WaitForAssertion(() => Assert.Single(module.Invocations["observeFieldsetToggle"]));
        Assert.False(host.Find("details").HasAttribute("open"));
        var reference = module.Invocations["observeFieldsetToggle"][0].Arguments[1]!;

        // The reader opens, then closes the group; the bound value follows.
        await host.InvokeAsync(() => InvokeJsCallback(reference, "OmniFieldset.Toggled", true));
        Assert.False(host.Instance.Collapsed);
        await host.InvokeAsync(() => InvokeJsCallback(reference, "OmniFieldset.Toggled", false));
        Assert.True(host.Instance.Collapsed);
        Assert.Equal(new[] { false, true }, host.Instance.Reported);

        // The host opens it: the toggle the browser fires for that change is not echoed back.
        await host.InvokeAsync(() => host.Instance.Fold(false));
        Assert.True(host.Find("details").HasAttribute("open"));
        await host.InvokeAsync(() => InvokeJsCallback(reference, "OmniFieldset.Toggled", true));
        Assert.Equal(new[] { false, true }, host.Instance.Reported);

        // The listener goes when the callback does, on the same fieldset.
        await host.InvokeAsync(host.Instance.StopListening);
        host.WaitForAssertion(() => Assert.Single(module.Invocations["disposeFieldsetToggle"]));
        Assert.Single(module.Invocations["observeFieldsetToggle"]);
    }

    [Fact]
    public void Fieldset_CollapsedChangedOnANonCollapsibleGroup_RunsNoScript()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;

        Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, Content("Contact"))
            .Add(component => component.CollapsedChanged, (bool _) => { })
            .AddChildContent("Champs"));

        Assert.Empty(JSInterop.Invocations);
    }

    // ---- data grid -------------------------------------------------------------------------------

    [Fact]
    public void Grid_WithoutMaxHeight_KeepsTheViewportAndTheInteropItHadBefore()
    {
        var module = JSInterop.SetupModule(GridModule);

        var virtualized = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2, 3 })
            .Add(component => component.AllowVirtualization, true));

        virtualized.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["applyLayout"]));
        Assert.Equal(
            new[] { "omni-data-grid__viewport", "omni-data-grid__viewport--virtual" },
            virtualized.Find(".omni-data-grid__viewport").ClassList.ToArray());
        Assert.All(module.Invocations["applyLayout"], invocation => Assert.Equal(5, invocation.Arguments.Count));
        Assert.Empty(module.Invocations["applyMaxHeight"]);
        Assert.DoesNotContain("style=", virtualized.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlainGrid_WithoutMaxHeight_KeepsItsViewportAndMakesNoCeilingCall()
    {
        var module = JSInterop.SetupModule(GridModule);

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2, 3 }));

        Assert.Equal(new[] { "omni-data-grid__viewport" }, grid.Find(".omni-data-grid__viewport").ClassList.ToArray());
        Assert.Empty(module.Invocations["applyMaxHeight"]);
        Assert.Empty(module.Invocations["applyLayout"]);
    }

    [Fact]
    public void VirtualizedGrid_MaxHeight_CapsTheViewportThroughTheScript()
    {
        var module = JSInterop.SetupModule(GridModule);

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 10_000).ToArray())
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.MaxHeight, " 20rem "));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations["applyMaxHeight"]));
        Assert.Equal("20rem", module.Invocations["applyMaxHeight"][0].Arguments[1]);
        Assert.Contains("omni-data-grid__viewport--capped", grid.Find(".omni-data-grid__viewport").ClassList);
        Assert.Contains("omni-data-grid__viewport--virtual", grid.Find(".omni-data-grid__viewport").ClassList);
        Assert.DoesNotContain("style=", grid.Markup, StringComparison.OrdinalIgnoreCase);

        // Removing the ceiling hands the viewport back to the fixed virtual height.
        grid.Render(parameters => parameters.Add(component => component.MaxHeight, null));
        grid.WaitForAssertion(() => Assert.Equal(2, module.Invocations["applyMaxHeight"].Count));
        Assert.Null(module.Invocations["applyMaxHeight"][1].Arguments[1]);
        Assert.DoesNotContain("omni-data-grid__viewport--capped", grid.Find(".omni-data-grid__viewport").ClassList);
    }

    [Fact]
    public void PlainGrid_MaxHeight_CapsTheViewportToo()
    {
        var module = JSInterop.SetupModule(GridModule);

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 200).ToArray())
            .Add(component => component.MaxHeight, "50vh"));

        grid.WaitForAssertion(() => Assert.Single(module.Invocations["applyMaxHeight"]));
        Assert.Equal("50vh", module.Invocations["applyMaxHeight"][0].Arguments[1]);
        Assert.Contains("omni-data-grid__viewport--capped", grid.Find(".omni-data-grid__viewport").ClassList);
    }

    [Fact]
    public void Grid_MaxHeight_GivesWayToAnExplicitHeightOrToFilling()
    {
        var module = JSInterop.SetupModule(GridModule);

        var sized = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2, 3 })
            .Add(component => component.AllowVirtualization, true)
            .Add(component => component.Height, "480px")
            .Add(component => component.MaxHeight, "20rem"));
        var filling = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, new[] { 1, 2, 3 })
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.MaxHeight, "20rem"));

        sized.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["applyLayout"]));
        Assert.DoesNotContain("omni-data-grid__viewport--capped", sized.Find(".omni-data-grid__viewport").ClassList);
        Assert.DoesNotContain("omni-data-grid__viewport--capped", filling.Find(".omni-data-grid__viewport").ClassList);
        Assert.Empty(module.Invocations["applyMaxHeight"]);
    }

    // ---- helpers ---------------------------------------------------------------------------------

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);

    private static void WithCulture(CultureInfo culture, Action action)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // What the browser does through the DotNetObjectReference the script received.
    private static Task InvokeJsCallback(object reference, string identifier, params object?[] arguments)
    {
        var target = reference.GetType().GetProperty("Value")!.GetValue(reference)!;
        var method = target.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(candidate => candidate.GetCustomAttribute<JSInvokableAttribute>()?.Identifier == identifier);
        return (Task)method.Invoke(target, arguments)!;
    }
}
