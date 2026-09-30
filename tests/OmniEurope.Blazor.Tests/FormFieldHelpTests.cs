using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The help of a form field: a "?" at the end of the label, outside it, whose tooltip carries the text
/// on hover and on keyboard focus. Without a help text the field is rendered exactly as before.
/// </summary>
public sealed class FormFieldHelpTests : OmniBunitContext
{
    [Fact]
    public void WithoutHelp_TheLabelIsADirectChild_AndThereIsNoMark()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.Id, "host-field")
            .Add(component => component.For, "host")
            .Add(component => component.Label, "Hote")
            .AddChildContent("<input id=\"host\" />"));

        Assert.Equal("LABEL", field.Find(".omni-form-field").Children[0].TagName);
        Assert.Empty(field.FindAll(".omni-form-field__label-row"));
        Assert.Empty(field.FindAll(".omni-tooltip"));
    }

    [Fact]
    public void WithHelp_AFocusableMarkBesideTheLabel_CarriesTheText()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.Id, "host-field")
            .Add(component => component.For, "host")
            .Add(component => component.Label, "Hote")
            .Add(component => component.Help, "Nom DNS du serveur de messagerie.")
            .AddChildContent("<input id=\"host\" />"));

        var row = field.Find(".omni-form-field > .omni-form-field__label-row");
        var label = row.QuerySelector("label")!;
        Assert.Equal("Hote", label.TextContent.Trim());
        // The mark is beside the label, never inside it: a click on it does not focus the control.
        Assert.Null(label.QuerySelector(".omni-tooltip"));
        var tooltip = row.QuerySelector(".omni-tooltip.omni-form-field__help")!;
        Assert.Equal("host-field-help", tooltip.GetAttribute("id"));
        var trigger = tooltip.QuerySelector(".omni-tooltip__trigger")!;
        Assert.Equal("0", trigger.GetAttribute("tabindex"));
        Assert.Equal("host-field-help-content", trigger.GetAttribute("aria-describedby"));
        var mark = trigger.QuerySelector(".omni-form-field__help-mark")!;
        Assert.Equal("img", mark.GetAttribute("role"));
        Assert.Equal("Aide", mark.GetAttribute("aria-label"));
        var content = field.Find("#host-field-help-content");
        Assert.Equal("tooltip", content.GetAttribute("role"));
        Assert.Equal("Nom DNS du serveur de messagerie.", content.TextContent);
    }
}
