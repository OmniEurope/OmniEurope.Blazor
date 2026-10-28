using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The end content of a form field (client application acceptance review R-8): given one, the control row
/// is marked so the stylesheet lays the control and that content on one line; without one the row is
/// rendered exactly as before.
/// </summary>
public sealed class FormFieldEndContentTests : OmniBunitContext
{
    [Fact]
    public void WithoutEndContent_TheControlRowIsNotMarked()
    {
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "secret")
            .Add(component => component.Label, "Password")
            .AddChildContent("<input id=\"secret\" />"));

        Assert.Equal("omni-form-field__control", field.Find(".omni-form-field__control").ClassName);
    }

    [Fact]
    public void WithEndContent_TheControlRowIsMarked_AndHoldsTheControlThenTheEndContent()
    {
        RenderFragment end = builder => builder.AddMarkupContent(0, "<button id=\"generate\">Generate</button>");
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "secret")
            .Add(component => component.Label, "Password")
            .Add(component => component.EndContent, end)
            .AddChildContent("<input id=\"secret\" />"));

        var row = field.Find(".omni-form-field__control.omni-form-field__control--end");
        Assert.Equal(["secret", "generate"], row.Children.Select(child => child.Id));
    }
}
