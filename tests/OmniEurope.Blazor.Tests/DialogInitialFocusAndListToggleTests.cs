using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The initial focus of a dialog (<see cref="OmniDialog.InitialFocus"/>, recette R1-10) and the chevron
/// of a filterable list (<see cref="OmniAutocomplete{TValue}.ShowToggle"/>, recette R1-11). The focus
/// itself moves in the browser: the scripts probe (eng/scripts-probe/requests.mjs) proves it there.
/// </summary>
public sealed class DialogInitialFocusAndListToggleTests : OmniBunitContext
{
    private const string FocusModule = Internal.OmniModules.Focus;

    private static readonly IReadOnlyList<OmniOption<string>> Cities =
    [
        new("bxl", "Bruxelles"),
        new("brg", "Bruges"),
        new("brn", "Brno")
    ];

    [Fact]
    public void Dialog_ByDefault_KeepsTheCloseButtonFocusAndTheCallItAlwaysMade()
    {
        var module = JSInterop.SetupModule(FocusModule);

        var dialog = RenderDialog(null);

        dialog.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));
        Assert.Equal(2, module.Invocations["activateDialog"][0].Arguments.Count);
        Assert.True(dialog.Find(".omni-dialog__close").HasAttribute("autofocus"));
        Assert.False(dialog.Find("section.omni-dialog").HasAttribute("tabindex"));
    }

    [Fact]
    public void Dialog_WithPanelFocus_FocusesThePanelAndNothingElse()
    {
        var module = JSInterop.SetupModule(FocusModule);

        var dialog = RenderDialog(OmniDialogInitialFocus.Panel);

        dialog.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));
        var arguments = module.Invocations["activateDialog"][0].Arguments;
        Assert.Equal(4, arguments.Count);
        Assert.Equal(false, arguments[2]);
        Assert.Equal("panel", arguments[3]);
        // The panel can take the focus; no element of the dialog asks for it on its own.
        Assert.Equal("-1", dialog.Find("section.omni-dialog").GetAttribute("tabindex"));
        Assert.Empty(dialog.FindAll("[autofocus]"));
    }

    [Fact]
    public void Dialog_WithFirstFocusable_AsksForTheFirstElementOfTheContent()
    {
        var module = JSInterop.SetupModule(FocusModule);

        var dialog = RenderDialog(OmniDialogInitialFocus.FirstFocusable);

        dialog.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));
        Assert.Equal("content", module.Invocations["activateDialog"][0].Arguments[3]);
        Assert.Empty(dialog.FindAll("[autofocus]"));
        Assert.False(dialog.Find("section.omni-dialog").HasAttribute("tabindex"));
    }

    [Fact]
    public void ModelessWindow_WithPanelFocus_PassesItToTheWindowScript()
    {
        var module = JSInterop.SetupModule(FocusModule);

        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Modal, false)
            .Add(component => component.Title, "Fenêtre")
            .Add(component => component.InitialFocus, OmniDialogInitialFocus.Panel));

        dialog.WaitForAssertion(() => Assert.Single(module.Invocations["activateWindow"]));
        Assert.Equal("panel", module.Invocations["activateWindow"][0].Arguments[2]);
    }

    [Fact]
    public void DialogRequest_InitialFocus_ReachesTheDialogItOpens()
    {
        var module = JSInterop.SetupModule(FocusModule);
        using var service = new OmniOverlayService();
        var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

        service.OpenDialog(new OmniDialogRequest("Confirmation", builder => builder.AddContent(0, "Continuer ?"))
        {
            InitialFocus = OmniDialogInitialFocus.Panel
        });

        host.WaitForAssertion(() => Assert.Single(module.Invocations["activateDialog"]));
        Assert.Equal("panel", module.Invocations["activateDialog"][0].Arguments[3]);
        Assert.Equal("-1", host.Find("section.omni-dialog").GetAttribute("tabindex"));
    }

    [Fact]
    public void FilterableDropDown_DrawsTheChevron_AndAPlainAutocompleteDoesNot()
    {
        var dropDown = RenderDropDown();
        Assert.Single(dropDown.FindAll(".omni-autocomplete--toggle > .omni-autocomplete__toggle[aria-hidden=true]"));
        Assert.False(dropDown.Find(".omni-autocomplete__toggle").HasAttribute("tabindex"));

        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Search, (_, _) => Task.FromResult(Cities)));
        Assert.Empty(autocomplete.FindAll(".omni-autocomplete__toggle"));
    }

    [Fact]
    public async Task FilterableDropDown_Chevron_OpensTheWholeListUnfilteredThenClosesIt()
    {
        var dropDown = RenderDropDown();

        // A filter typed first: the chevron still lists every option.
        await dropDown.Find("input").InputAsync(new ChangeEventArgs { Value = "brn" });
        dropDown.WaitForAssertion(() => Assert.Single(dropDown.FindAll("[role=option]")));
        await dropDown.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(dropDown.FindAll("[role=option]"));

        await dropDown.Find(".omni-autocomplete__toggle").ClickAsync(new MouseEventArgs());
        dropDown.WaitForAssertion(() => Assert.Equal(3, dropDown.FindAll("[role=option]").Count));
        Assert.Contains("omni-autocomplete__toggle--open", dropDown.Find(".omni-autocomplete__toggle").ClassList);
        Assert.Equal("true", dropDown.Find("input").GetAttribute("aria-expanded"));

        await dropDown.Find(".omni-autocomplete__toggle").ClickAsync(new MouseEventArgs());
        Assert.Empty(dropDown.FindAll("[role=option]"));
        Assert.Equal("false", dropDown.Find("input").GetAttribute("aria-expanded"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task FilterableDropDown_Chevron_FollowsDisabledAndReadOnly(bool disabled, bool readOnly)
    {
        var dropDown = RenderDropDown(disabled, readOnly);

        var toggle = dropDown.Find(".omni-autocomplete__toggle");
        Assert.Contains("omni-autocomplete__toggle--disabled", toggle.ClassList);
        await toggle.ClickAsync(new MouseEventArgs());

        Assert.Empty(dropDown.FindAll("[role=option]"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData(true, true)]
    [InlineData("readonly", true)]
    public void Autocomplete_Chevron_ReadsTheReadonlyAttributeAsTheNativeInputDoes(object? readOnly, bool disabled)
    {
        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.ShowToggle, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Search, (_, _) => Task.FromResult(Cities))
            .AddUnmatched("readonly", readOnly));

        Assert.Equal(disabled, autocomplete.Find(".omni-autocomplete__toggle").ClassList.Contains("omni-autocomplete__toggle--disabled"));
        // A null or false value writes no readonly attribute: the input stays editable, and so does the chevron.
        if (readOnly is null or false) Assert.False(autocomplete.Find("input").HasAttribute("readonly"));
    }

    private IRenderedComponent<OmniDialog> RenderDialog(OmniDialogInitialFocus? initialFocus) =>
        Render<OmniDialog>(parameters =>
        {
            parameters
                .Add(component => component.Open, true)
                .Add(component => component.Title, "Confirmation")
                .Add(component => component.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "input");
                    builder.AddAttribute(1, "aria-label", "Nom");
                    builder.CloseElement();
                }));
            if (initialFocus is { } focus) parameters.Add(component => component.InitialFocus, focus);
        });

    private IRenderedComponent<OmniDropDown<string>> RenderDropDown(bool disabled = false, bool readOnly = false)
    {
        var value = string.Empty;
        return Render<OmniDropDown<string>>(parameters =>
        {
            parameters
                .Add(component => component.Filterable, true)
                .Add(component => component.Options, Cities)
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.ValueChanged, (string next) => value = next)
                .Add(component => component.Disabled, disabled);
            if (readOnly) parameters.AddUnmatched("readonly", true);
        });
    }
}
