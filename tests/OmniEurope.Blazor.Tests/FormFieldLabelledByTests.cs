using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-007 lot 12: a control that a <c>label for</c> cannot name (the editable surface of the HTML
/// editor, a radio group, a fieldset without legend) takes the name of the enclosing
/// <see cref="OmniFormField"/> through <c>aria-labelledby</c> to the label's stable id
/// <c>{For}-label</c>; its own localized default name is kept only outside a form field.
/// </summary>
public sealed class FormFieldLabelledByTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Choices = [new("a", "Alpha"), new("b", "Beta")];

    [Fact]
    public void FormField_GivesItsLabelTheStableIdForLabel()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "body")
            .Add(component => component.Label, "Corps du message")
            .AddChildContent("<input id=\"body\" />"));

        var label = field.Find("label");
        Assert.Equal("body-label", label.GetAttribute("id"));
        Assert.Equal("body", label.GetAttribute("for"));
    }

    [Fact]
    public void HtmlEditor_InsideAFormField_IsNamedByTheFieldLabel_NotByItsDefaultName()
    {
        var value = "<p>a</p>";
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "body")
            .Add(component => component.Label, "Corps du message")
            .AddChildContent<OmniHtmlEditor>(editor => editor
                .Add(component => component.Id, "body")
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)));

        var surface = field.Find("[role=textbox]");
        Assert.Equal("body-label", surface.GetAttribute("aria-labelledby"));
        Assert.Null(surface.GetAttribute("aria-label"));
        var region = field.Find("section.omni-html-editor");
        Assert.Equal("body-label", region.GetAttribute("aria-labelledby"));
        Assert.Null(region.GetAttribute("aria-label"));
        Assert.Equal("Corps du message", field.Find("#body-label").TextContent.Trim());
    }

    [Fact]
    public void HtmlEditor_OwnLabel_WinsOverTheFieldLabel()
    {
        var value = "<p>a</p>";
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "body")
            .Add(component => component.Label, "Corps du message")
            .AddChildContent<OmniHtmlEditor>(editor => editor
                .Add(component => component.Id, "body")
                .Add(component => component.Label, "Contenu")
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)));

        var surface = field.Find("[role=textbox]");
        Assert.Equal("Contenu", surface.GetAttribute("aria-label"));
        Assert.Null(surface.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void HtmlEditor_OutsideAFormField_OrUnderAFieldForAnotherControl_KeepsItsLocalizedDefaultName()
    {
        var value = "<p>a</p>";
        var alone = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Id, "body")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        Assert.Equal("Éditeur HTML", alone.Find("[role=textbox]").GetAttribute("aria-label"));
        Assert.Null(alone.Find("[role=textbox]").GetAttribute("aria-labelledby"));

        var elsewhere = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "other")
            .Add(component => component.Label, "Autre")
            .AddChildContent<OmniHtmlEditor>(editor => editor
                .Add(component => component.Id, "body")
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)));
        Assert.Equal("Éditeur HTML", elsewhere.Find("[role=textbox]").GetAttribute("aria-label"));
        Assert.Null(elsewhere.Find("[role=textbox]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Rating_InsideAFormField_IsNamedByTheFieldLabel()
    {
        int? stars = 2;
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "stars")
            .Add(component => component.Label, "Satisfaction")
            .AddChildContent<OmniRating>(rating => rating
                .Add(component => component.Id, "stars")
                .Add(component => component.Value, stars)
                .Add(component => component.ValueExpression, () => stars)));

        var group = field.Find("[role=radiogroup]");
        Assert.Equal("stars-label", group.GetAttribute("aria-labelledby"));
        Assert.Null(group.GetAttribute("aria-label"));
    }

    [Fact]
    public void RadioButtonList_WithoutLegend_InsideAFormField_IsNamedByTheFieldLabel()
    {
        var choice = "a";
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "choice")
            .Add(component => component.Label, "Option")
            .AddChildContent<OmniRadioButtonList<string>>(list => list
                .Add(component => component.Id, "choice")
                .Add(component => component.Options, Choices)
                .Add(component => component.Value, choice)
                .Add(component => component.ValueExpression, () => choice)));

        Assert.Equal("choice-label", field.Find("fieldset").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void SelectBar_InsideAFormField_IsNamedByTheFieldLabel()
    {
        var choice = "a";
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "mode")
            .Add(component => component.Label, "Mode")
            .AddChildContent<OmniSelectBar<string>>(bar => bar
                .Add(component => component.Id, "mode")
                .Add(component => component.Options, Choices)
                .Add(component => component.Value, choice)
                .Add(component => component.ValueExpression, () => choice)));

        Assert.Equal("mode-label", field.Find("[role=radiogroup]").GetAttribute("aria-labelledby"));
    }
}
