using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The runtime backstop of OE0001: a PascalCase attribute a component has no parameter for is a removed
/// or misspelled parameter, never an HTML attribute, so it fails at render instead of reaching the markup.
/// </summary>
public sealed class UnknownParameterTests : OmniBunitContext
{
    [Fact]
    public void ComponentBase_UnknownPascalCaseAttribute_IsRejectedWithTheComponentAndTheName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniBadge>(parameters => parameters
            .Add(component => component.Text, "Nouveau")
            .AddUnmatched("IconName", "star")));

        Assert.Equal("OmniBadge has no parameter 'IconName'.", exception.Message);
    }

    [Fact]
    public void InputBase_UnknownPascalCaseAttribute_IsRejected()
    {
        var value = false;
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniCheckBox<bool>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .AddUnmatched("AllowClear", true)));

        Assert.Equal("OmniCheckBox has no parameter 'AllowClear'.", exception.Message);
    }

    [Fact]
    public void GenericComponent_IsNamedWithoutItsArity()
    {
        var value = "alpha";
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("alpha", "Alpha")])
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .AddUnmatched("Presentation", "compact")));

        Assert.Equal("OmniDropDown has no parameter 'Presentation'.", exception.Message);
    }

    [Fact]
    public void ComponentThatOverridesOnParametersSet_StillRejectsIt()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniComponentsHost>(parameters => parameters
            .AddUnmatched("Position", "top")));

        Assert.Equal("OmniComponentsHost has no parameter 'Position'.", exception.Message);
    }

    [Fact]
    public void LowercaseAttributes_StillReachTheMarkup()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .Add(component => component.Text, "Nouveau")
            .AddUnmatched("data-kind", "info")
            .AddUnmatched("title", "Nouvel élément")
            .AddUnmatched("aria-describedby", "aide"));

        var root = badge.Find("[data-kind]");
        Assert.Equal("info", root.GetAttribute("data-kind"));
        Assert.Equal("Nouvel élément", root.GetAttribute("title"));
        Assert.Equal("aide", root.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void ParameterNamesMatchCaseInsensitively_AndAreNotUnmatched()
    {
        var badge = Render<OmniBadge>(parameters => parameters.AddUnmatched("TEXT", "Nouveau"));

        Assert.Contains("Nouveau", badge.Markup, StringComparison.Ordinal);
    }
}
