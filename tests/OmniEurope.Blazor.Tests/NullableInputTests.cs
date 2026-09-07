using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the three-state cycle of the nullable inputs: the order of the states, the effect of
/// AllowIndeterminate, the disabled guard, the accessible projection of each state, and the string
/// parsing an EditContext relies on.
/// </summary>
public sealed class NullableInputTests : OmniBunitContext
{
    [Fact]
    public void NullableCheckBox_CyclesNullThenTrueThenFalseAndBackToNull()
    {
        bool? value = null;
        var component = Render<OmniNullableCheckBox>(parameters => parameters
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        Assert.Equal("mixed", component.Find("button").GetAttribute("aria-checked"));

        component.Find("button").Click();
        Assert.True(value);

        component.Find("button").Click();
        Assert.False(value);

        component.Find("button").Click();
        Assert.Null(value);
    }

    [Fact]
    public void NullableCheckBox_WithoutIndeterminateNeverReturnsToNull()
    {
        bool? value = false;
        var component = Render<OmniNullableCheckBox>(parameters => parameters
            .Add(item => item.AllowIndeterminate, false)
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        component.Find("button").Click();
        Assert.True(value);

        component.Find("button").Click();
        Assert.False(value);
    }

    [Fact]
    public void NullableCheckBox_DisabledIgnoresTheClick()
    {
        bool? value = null;
        var component = Render<OmniNullableCheckBox>(parameters => parameters
            .Add(item => item.Disabled, true)
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        component.Find("button").Click();

        Assert.Null(value);
        Assert.True(component.Find("button").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData(null, "mixed", "omni-checkbox-nullable--mixed")]
    [InlineData(true, "true", "omni-checkbox-nullable--checked")]
    [InlineData(false, "false", null)]
    public void NullableCheckBox_ProjectsEachStateOnTheButton(bool? state, string ariaChecked, string? modifier)
    {
        var value = state;
        var component = Render<OmniNullableCheckBox>(parameters => parameters
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value));

        var button = component.Find("button");
        Assert.Equal(ariaChecked, button.GetAttribute("aria-checked"));

        var css = button.GetAttribute("class") ?? string.Empty;
        if (modifier is null)
        {
            Assert.DoesNotContain("--mixed", css, StringComparison.Ordinal);
            Assert.DoesNotContain("--checked", css, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains(modifier, css, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void NullableSwitch_CyclesNullThenTrueThenFalseAndBackToNull()
    {
        bool? value = null;
        var component = Render<OmniNullableSwitch>(parameters => parameters
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        component.Find("button").Click();
        Assert.True(value);

        component.Find("button").Click();
        Assert.False(value);

        component.Find("button").Click();
        Assert.Null(value);
    }

    [Fact]
    public void NullableSwitch_WithoutIndeterminateNeverReturnsToNull()
    {
        bool? value = false;
        var component = Render<OmniNullableSwitch>(parameters => parameters
            .Add(item => item.AllowIndeterminate, false)
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        component.Find("button").Click();
        Assert.True(value);

        component.Find("button").Click();
        Assert.False(value);
    }

    [Fact]
    public void NullableSwitch_DisabledIgnoresTheClick()
    {
        bool? value = true;
        var component = Render<OmniNullableSwitch>(parameters => parameters
            .Add(item => item.Disabled, true)
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        component.Find("button").Click();

        Assert.True(value);
    }

    [Fact]
    public void NullableSwitch_DescribesOnlyTheIndeterminateState()
    {
        bool? value = null;
        var component = Render<OmniNullableSwitch>(parameters => parameters
            .Add(item => item.IndeterminateDescription, "Indefini")
            .Add(item => item.Value, value)
            .Add(item => item.ValueExpression, () => value)
            .Add(item => item.ValueChanged, changed => value = changed));

        var button = component.Find("button");
        Assert.Equal("Indefini", button.GetAttribute("aria-description"));
        Assert.Equal("false", button.GetAttribute("aria-checked"));
        Assert.Contains("omni-switch--mixed", button.GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);

        component.Find("button").Click();

        Assert.False(component.Find("button").HasAttribute("aria-description"));
        Assert.Contains("omni-switch--checked", component.Find("button").GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", true, null)]
    [InlineData("true", true, true)]
    [InlineData("False", true, false)]
    [InlineData("oui", false, null)]
    public void NullableInputs_ParseTheStringAnEditContextSupplies(string candidate, bool expected, bool? parsed)
    {
        bool? bound = null;
        var checkBox = Render<CheckBoxParseProbe>(parameters => parameters
            .Add(item => item.Value, bound)
            .Add(item => item.ValueExpression, () => bound)).Instance;
        var toggle = Render<SwitchParseProbe>(parameters => parameters
            .Add(item => item.Value, bound)
            .Add(item => item.ValueExpression, () => bound)).Instance;

        Assert.Equal(expected, checkBox.Parse(candidate, out var checkBoxValue, out var checkBoxError));
        Assert.Equal(parsed, checkBoxValue);
        Assert.Equal(expected, toggle.Parse(candidate, out var switchValue, out var switchError));
        Assert.Equal(parsed, switchValue);

        if (expected)
        {
            Assert.Null(checkBoxError);
            Assert.Null(switchError);
        }
        else
        {
            Assert.NotEmpty(checkBoxError);
            Assert.NotEmpty(switchError);
        }
    }

    /// <summary>Reaches the protected parser without an EditForm, which never types into a button.</summary>
    private sealed class CheckBoxParseProbe : OmniNullableCheckBox
    {
        public bool Parse(string? value, out bool? result, out string message) =>
            TryParseValueFromString(value, out result, out message);
    }

    private sealed class SwitchParseProbe : OmniNullableSwitch
    {
        public bool Parse(string? value, out bool? result, out string message) =>
            TryParseValueFromString(value, out result, out message);
    }
}
