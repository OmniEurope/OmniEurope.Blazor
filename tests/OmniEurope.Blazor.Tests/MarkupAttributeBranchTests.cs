using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The conditional attributes and classes of the markup in the shapes the component suites leave out:
/// the Razor compiler maps each of these conditions to a neighbouring line, so the coverage report
/// showed them as unexplained branches of an <c>@if</c> or a <c>@ChildContent</c>.
/// </summary>
public sealed class MarkupAttributeBranchTests : OmniBunitContext
{
    private readonly string? _choice = null;
    private readonly DateOnly? _date = null;
    private readonly TimeOnly? _time = null;
    private readonly DateTime? _moment = null;

    [Fact]
    public void Main_ThatIsNoFocusTarget_AndScrollsWithAHiddenScrollbar()
    {
        var plain = Render<OmniMain>(parameters => parameters.Add(component => component.FocusTarget, false));
        var scrolling = Render<OmniMain>(parameters => parameters
            .Add(component => component.Scrollable, true)
            .Add(component => component.FocusTarget, false)
            .Add(component => component.AutoHideScrollbar, true));

        Assert.Null(plain.Find("main").GetAttribute("tabindex"));
        Assert.Null(scrolling.Find("main").GetAttribute("tabindex"));
        Assert.Contains("omni-main--scrollbar-autohide", scrolling.Find("main").ClassList);
    }

    [Fact]
    public void Row_WrapsByDefault_AndNotWhenAskedNotTo()
    {
        var wrapping = Render<OmniRow>();
        var single = Render<OmniRow>(parameters => parameters.Add(component => component.Wrap, false));

        Assert.Contains("omni-row--wrap", wrapping.Find(".omni-row").ClassList);
        Assert.DoesNotContain("omni-row--wrap", single.Find(".omni-row").ClassList);
    }

    [Fact]
    public void Header_LinkedLogoWithoutBrandText_IsTheLinkName()
    {
        var header = Render<OmniHeader>(parameters => parameters
            .Add(component => component.BrandLogo, "/logo.svg")
            .Add(component => component.BrandHref, "/"));

        Assert.NotNull(header.Find("a.omni-header__brand--link img"));

        var textOnly = Render<OmniHeader>(parameters => parameters
            .Add(component => component.Brand, "Omni")
            .Add(component => component.BrandHref, "/"));
        Assert.Empty(textOnly.FindAll("a.omni-header__brand--link img"));
        Assert.Contains("Omni", textOnly.Find("a.omni-header__brand--link").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Dialog_WindowedDraggableResizableAndNotDismissible()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Modal, false)
            .Add(component => component.Draggable, true)
            .Add(component => component.Resizable, true)
            .Add(component => component.Dismissible, false)
            .Add(component => component.Title, "Fenêtre"));

        var section = dialog.Find("section");
        Assert.Equal("alertdialog", section.GetAttribute("role"));
        Assert.Equal("false", section.GetAttribute("aria-modal"));
        Assert.Contains("omni-dialog--draggable", section.ClassList);
        Assert.Contains("omni-dialog--resizable", section.ClassList);
        Assert.NotNull(dialog.Find(".omni-window-layer"));
    }

    [Fact]
    public void Tooltip_LongAndFocusable_UnfoldsOnDemand()
    {
        var tooltip = Render<OmniTooltip>(parameters => parameters
            .Add(component => component.Text, string.Concat(Enumerable.Repeat("Un texte assez long pour être replié. ", 20)))
            .Add(component => component.Focusable, true)
            .AddChildContent("?"));

        Assert.Equal("0", tooltip.Find(".omni-tooltip__trigger").GetAttribute("tabindex"));
        tooltip.Find(".omni-tooltip__more").Click();
        Assert.Contains("omni-tooltip--expanded", tooltip.Find(".omni-tooltip").ClassList);
    }

    [Fact]
    public void PageHeader_TrailNamedByTheHost()
    {
        Services.GetRequiredService<OmniBreadcrumbService>().Set(new OmniBreadcrumbEntry("Accueil", "/"), new OmniBreadcrumbEntry("Factures", "/factures"));

        var header = Render<OmniPageHeader>(parameters => parameters
            .Add(component => component.Title, "Factures")
            .Add(component => component.TrailLabel, "Fil"));

        Assert.Equal("Fil", header.Find(".omni-page-header__trail nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void Diff_AndCode_ThatWrapTheirLines()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters
            .Add(component => component.Files, Array.Empty<OmniDiffFile>())
            .Add(component => component.Wrap, true));
        var code = Render<OmniCodeBlock>(parameters => parameters
            .Add(component => component.Code, "dotnet test")
            .Add(component => component.Inline, true)
            .Add(component => component.Wrap, true));

        Assert.Contains("omni-unified-diff--wrap", diff.Find(".omni-unified-diff").ClassList);
        Assert.Contains("omni-code-block--wrap", code.Find("figure").ClassList);
        Assert.Contains("omni-code-block--inline", code.Find("figure").ClassList);
    }

    [Fact]
    public void ChoiceList_OptionWithoutValue_PostsNothing()
    {
        string? chosen = "x";
        var list = Render<OmniRadioButtonList<string?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string?>(null, "Aucun"), new OmniOption<string?>("a", "A")])
            .Add(component => component.Value, "a")
            .Add(component => component.ValueChanged, value => chosen = value)
            .Add(component => component.ValueExpression, () => _choice));

        Assert.Null(list.FindAll("input")[0].GetAttribute("value"));
        list.FindAll("input")[0].Change(true);
        Assert.Null(chosen);
    }

    [Fact]
    public void DatePickers_ReadOnly_DisableTheirToggle()
    {
        var date = Render<OmniDatePicker>(parameters => parameters.Add(component => component.ReadOnly, true).Add(component => component.ValueExpression, () => _date));
        var time = Render<OmniTimePicker>(parameters => parameters.Add(component => component.ReadOnly, true).Add(component => component.ValueExpression, () => _time));
        var moment = Render<OmniDateTimePicker>(parameters => parameters.Add(component => component.ReadOnly, true).Add(component => component.ValueExpression, () => _moment));

        Assert.NotNull(date.Find(".omni-date__toggle").GetAttribute("disabled"));
        Assert.NotNull(time.Find("button[aria-haspopup]").GetAttribute("disabled"));
        Assert.NotNull(moment.Find("button[aria-haspopup]").GetAttribute("disabled"));
    }

    [Fact]
    public void DatePickers_Disabled_DisableTheirToggle()
    {
        var date = Render<OmniDatePicker>(parameters => parameters.Add(component => component.Disabled, true).Add(component => component.ValueExpression, () => _date));
        var time = Render<OmniTimePicker>(parameters => parameters.Add(component => component.Disabled, true).Add(component => component.ValueExpression, () => _time));
        var moment = Render<OmniDateTimePicker>(parameters => parameters.Add(component => component.Disabled, true).Add(component => component.ValueExpression, () => _moment));

        Assert.NotNull(date.Find(".omni-date__toggle").GetAttribute("disabled"));
        Assert.NotNull(time.Find("button[aria-haspopup]").GetAttribute("disabled"));
        Assert.NotNull(moment.Find("button[aria-haspopup]").GetAttribute("disabled"));
    }

    private readonly string _text = string.Empty;

    [Fact]
    public void Password_NotRevealable_HasNoEye_AndADisabledCopyableBox_DisablesItsCopy()
    {
        var password = Render<OmniPassword>(parameters => parameters
            .Add(component => component.Revealable, false)
            .Add(component => component.ValueExpression, () => _text));
        var box = Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, "secret")
            .Add(component => component.Copyable, true)
            .Add(component => component.Disabled, true)
            .Add(component => component.ValueExpression, () => _text));

        Assert.Empty(password.FindAll(".omni-password__toggle"));
        Assert.NotNull(box.Find(".omni-text-box-field__copy").GetAttribute("disabled"));
    }

    [Fact]
    public void Upload_Disabled_DisablesItsInput_AsAZoneAndAsAField()
    {
        var zone = Render<OmniUpload>(parameters => parameters.Add(component => component.Disabled, true));
        var field = Render<OmniUpload>(parameters => parameters.Add(component => component.Disabled, true).Add(component => component.Display, OmniUploadDisplay.Field));

        Assert.NotNull(zone.Find("input[type=file]").GetAttribute("disabled"));
        Assert.NotNull(field.Find(".omni-upload__field input[type=file]").GetAttribute("disabled"));
    }
}
