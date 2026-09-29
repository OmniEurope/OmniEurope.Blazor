using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The runtime backstop of OE0001: a PascalCase attribute a component has no parameter for is a removed
/// or misspelled parameter, never an HTML attribute, so it fails at render instead of reaching the markup.
/// A lowercase class or id handed over in the captured attributes is refused too: Class and Id are the
/// parameters, placed by the component itself.
/// </summary>
public sealed class UnknownParameterTests : OmniBunitContext
{
    [Fact]
    public void ComponentBase_UnknownPascalCaseAttribute_IsRejectedWithTheComponentAndTheName()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniBadge>(parameters => parameters
            .AddChildContent("Nouveau")
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
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniAlert>(parameters => parameters
            .AddChildContent("Message.")
            .AddUnmatched("Position", "top")));

        Assert.Equal("OmniAlert has no parameter 'Position'.", exception.Message);
    }

    [Theory]
    [InlineData("Position")]
    [InlineData("Class")]
    [InlineData("Id")]
    [InlineData("data-zone")]
    public void ComponentsHost_DrawsNoElement_AndTakesNoAttribute(string name)
    {
        // It captures no attribute at all: an id, a class or a data attribute would have nowhere to go,
        // so Blazor refuses it rather than the host dropping it silently (OE0001 refuses it at build).
        var exception = Assert.Throws<InvalidOperationException>(() => Render(builder =>
        {
            builder.OpenComponent<OmniComponentsHost>(0);
            builder.AddComponentParameter(1, name, "x");
            builder.CloseComponent();
        }));

        Assert.Contains($"'{name}'", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("class", "Class")]
    [InlineData("id", "Id")]
    [InlineData("CLASS", "Class")]
    public void LowercaseClassOrId_InTheCapturedAttributes_IsRefusedWithTheParameterToUse(string attribute, string parameter)
    {
        // Blazor binds a class or id written on the tag to the parameter; only a dictionary handed over
        // as AdditionalAttributes can smuggle one past it, where it would replace the component's own.
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniBadge>(parameters => parameters
            .AddChildContent("Nouveau")
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { [attribute] = "x" })));

        Assert.Contains(parameter, exception.Message, StringComparison.Ordinal);
        Assert.StartsWith("OmniBadge", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LowercaseId_InTheCapturedAttributesOfAFormControl_IsRefused()
    {
        var value = "x";
        var exception = Assert.Throws<InvalidOperationException>(() => Render<OmniTextBox>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.AdditionalAttributes, new Dictionary<string, object> { ["id"] = "nom" })));

        Assert.Equal("OmniTextBox does not take a 'id' attribute: use its 'Id' parameter.", exception.Message);
    }

    [Fact]
    public void LowercaseAttributes_StillReachTheMarkup()
    {
        var badge = Render<OmniBadge>(parameters => parameters
            .AddChildContent("Nouveau")
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
        var badge = Render<OmniBadge>(parameters => parameters
            .AddUnmatched("TONE", OmniTone.Danger)
            .AddChildContent("Nouveau"));

        Assert.Contains("omni-badge--danger", badge.Find(".omni-badge").ClassList);
    }

    [Fact]
    public void ComponentOwnedAttributes_WinOverTheCapturedOnes()
    {
        // @attributes first, the component's own attributes after: a captured role cannot turn a
        // progress bar into something else.
        var progress = Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Value, 40)
            .AddUnmatched("role", "img")
            .AddUnmatched("data-step", "2"));

        var root = progress.Find(".omni-progress");
        Assert.Equal("progressbar", root.GetAttribute("role"));
        Assert.Equal("2", root.GetAttribute("data-step"));
    }
}
