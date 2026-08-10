using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class InteractionComponentTests : BunitContext
{
    [Fact]
    public void SecondBatch_RendersFoundationControlsWithoutInlineStyles()
    {
        var markups = new[]
        {
            Render<OmniBody>(parameters => parameters.AddChildContent("Body")).Markup,
            Render<OmniSidebar>(parameters => parameters.Add(component => component.Open, true).AddChildContent("Navigation")).Markup,
            Render<OmniSidebarToggle>(parameters => parameters.Add(component => component.Controls, "sidebar")).Markup,
            Render<OmniThemeScope>(parameters => parameters.Add(component => component.Appearance, OmniAppearance.Dark).AddChildContent("Theme")).Markup,
            Render<OmniAppearanceToggle>().Markup,
            Render<OmniLabel>(parameters => parameters.Add(component => component.For, "field").AddChildContent("Field")).Markup,
            Render<OmniFormField>(parameters => parameters
                .Add(component => component.For, "field")
                .Add(component => component.Label, Content("Field"))
                .AddChildContent("Control")).Markup
        };

        Assert.All(markups, AssertMarkupHasNoInlineStyle);
        Assert.Contains("data-omni-theme=\"dark\"", markups[3], StringComparison.Ordinal);
    }

    [Fact]
    public void ControlledToggles_ReportTheirNextState()
    {
        var sidebarState = false;
        var sidebarToggle = Render<OmniSidebarToggle>(parameters => parameters
            .Add(component => component.Controls, "sidebar")
            .Add(component => component.Open, sidebarState)
            .Add(component => component.OpenChanged, value => sidebarState = value));

        var appearance = OmniAppearance.System;
        var appearanceToggle = Render<OmniAppearanceToggle>(parameters => parameters
            .Add(component => component.Appearance, appearance)
            .Add(component => component.AppearanceChanged, value => appearance = value));

        sidebarToggle.Find("button").Click();
        appearanceToggle.Find("button").Click();

        Assert.True(sidebarState);
        Assert.Equal(OmniAppearance.Light, appearance);
    }

    [Fact]
    public void FormInputs_UpdateTheirEditContextModel()
    {
        var form = Render<FormTestHost>();

        form.Find("#name").Input("Alice");
        form.Find("#password").Input("secret");
        form.Find("#notes").Input("Notes");
        form.Find("#age").Change("42");
        form.Find("#accepted").Change(true);
        form.Find("#enabled").Click();
        form.Find("#optional-accepted").Click();
        form.Find("#optional-enabled").Click();

        Assert.Equal("Alice", form.Instance.Model.Name);
        Assert.Equal("secret", form.Instance.Model.Password);
        Assert.Equal("Notes", form.Instance.Model.Notes);
        Assert.Equal(42, form.Instance.Model.Age);
        Assert.True(form.Instance.Model.Accepted);
        Assert.True(form.Instance.Model.Enabled);
        Assert.True(form.Instance.Model.OptionalAccepted);
        Assert.True(form.Instance.Model.OptionalEnabled);
        AssertMarkupHasNoInlineStyle(form.Markup);
    }

    [Fact]
    public async Task RequiredValidator_TracksValidationRequestsAndFieldChanges()
    {
        var form = Render<FormTestHost>();

        Assert.False(await form.InvokeAsync(() => form.Instance.EditContext.Validate()));
        Assert.Contains("Ce champ est obligatoire.", form.Instance.EditContext.GetValidationMessages());
        form.WaitForAssertion(() =>
        {
            Assert.Equal("true", form.Find("#name").GetAttribute("aria-invalid"));
            Assert.Contains("Ce champ est obligatoire.", form.Find("[role=alert]").TextContent, StringComparison.Ordinal);
        });

        form.Find("#name").Input("Alice");

        Assert.True(await form.InvokeAsync(() => form.Instance.EditContext.Validate()));
        Assert.Empty(form.Instance.EditContext.GetValidationMessages());
        form.WaitForAssertion(() => Assert.Empty(form.FindAll(".omni-validation-message")));
    }

    [Fact]
    public void PasswordReveal_ChangesOnlyTheInputType()
    {
        var form = Render<FormTestHost>();

        Assert.Equal("password", form.Find("#password").GetAttribute("type"));
        form.Find(".omni-password__toggle").Click();
        Assert.Equal("text", form.Find("#password").GetAttribute("type"));
        Assert.Equal("true", form.Find(".omni-password__toggle").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task Validators_RejectLengthEmailAndComparisonMismatches()
    {
        var form = Render<FormTestHost>();
        form.Find("#name").Input("Al");
        form.Instance.Model.Email = "invalid";
        form.Instance.Model.Password = "first";
        form.Instance.Model.ConfirmedPassword = "second";

        Assert.False(await form.InvokeAsync(() => form.Instance.EditContext.Validate()));
        var messages = form.Instance.EditContext.GetValidationMessages().ToArray();

        Assert.Contains("La longueur de ce champ n'est pas valide.", messages);
        Assert.Contains("L'adresse e-mail n'est pas valide.", messages);
        Assert.Contains("Les valeurs ne correspondent pas.", messages);
    }

    [Fact]
    public async Task DelayedValidator_CancelsStaleFieldValidation()
    {
        var form = Render<FormTestHost>();

        form.Find("#name").Input("Al");
        form.Find("#name").Input("Alice");

        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            "La longueur de ce champ n'est pas valide.",
            form.Instance.EditContext.GetValidationMessages());
    }

    [Fact]
    public void Numeric_ParsesTheActiveCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var holder = new NumericHolder();
            var numeric = Render<OmniNumeric<decimal>>(parameters => parameters
                .Add(component => component.Value, holder.Value)
                .Add(component => component.ValueChanged, value => holder.Value = value)
                .Add(component => component.ValueExpression, () => holder.Value));

            numeric.Find("input").Change("12,5");

            Assert.Equal(12.5m, holder.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void TemplateForm_RequiresExactlyOneModelSource()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniTemplateForm<FormTestHost.FormTestModel>>());

        var model = new FormTestHost.FormTestModel();
        var form = Render<OmniTemplateForm<FormTestHost.FormTestModel>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.ChildContent, _ => Content("Fields")));

        Assert.Equal("Fields", form.Find("form").TextContent);
    }

    [Fact]
    public void TemplateForm_FocusesTheFirstInvalidControlThroughTheStaticModule()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        var module = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omniInterop.js");
        module.SetupVoid("focusFirstInvalid", _ => true);
        var form = Render<TemplateFormTestHost>();

        form.Find("form").Submit();

        form.WaitForAssertion(() =>
        {
            Assert.Single(module.Invocations["focusFirstInvalid"]);
            Assert.Equal("true", form.Find("#template-name").GetAttribute("aria-invalid"));
            Assert.Contains("Ce champ est obligatoire.", form.Find("[role=alert]").TextContent, StringComparison.Ordinal);
        });
    }

    private static RenderFragment Content(string value) => builder => builder.AddContent(0, value);

    private sealed class NumericHolder
    {
        public decimal Value { get; set; }
    }

    private static void AssertMarkupHasNoInlineStyle(string markup) =>
        Assert.False(markup.Contains("style=", StringComparison.OrdinalIgnoreCase), markup);
}
