using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the check box and the switch bound to a bool or a bool?: the three-state cycle of a bool?
/// (the order of the states, the effect of AllowIndeterminate, the disabled guard, the accessible
/// projection of each state), the two-state rendering of a bool, the refusal of any other value type,
/// and the string parsing an EditContext relies on.
/// </summary>
public sealed class NullableInputTests : OmniBunitContext
{
    private static void Click<TComponent>(IRenderedComponent<TComponent> component)
        where TComponent : Microsoft.AspNetCore.Components.IComponent => component.Find("button").Click();

    /// <summary>
    /// Renders a nullable toggle bound to <paramref name="value"/>, each change given to <paramref name="changed"/>,
    /// with the flag <paramref name="flag"/> set to <paramref name="flagValue"/>.
    /// </summary>
    /// <summary>Without its indeterminate state, a nullable toggle bound to false turns true then false, never back to null.</summary>
    private void AssertNeverReturnsToNull<TComponent>(Expression<Func<TComponent, bool>> allowIndeterminate)
        where TComponent : InputBase<bool?>
    {
        bool? value = false;
        var component = RenderBoundToggle(value, changed => value = changed, allowIndeterminate, false);

        Click(component);
        Assert.True(value);

        Click(component);
        Assert.False(value);
    }

    private IRenderedComponent<TComponent> RenderBoundToggle<TComponent>(
        bool? value, Action<bool?> changed, Expression<Func<TComponent, bool>> flag, bool flagValue)
        where TComponent : InputBase<bool?> =>
        Render<TComponent>(parameters => parameters
            .Add(flag, flagValue)
            .BindValue(() => value, changed));

    [Fact]
    public void NullableCheckBox_CyclesNullThenTrueThenFalseAndBackToNull()
    {
        bool? value = null;
        var component = Render<OmniCheckBox<bool?>>(parameters => parameters
            .BindValue(() => value, changed => value = changed));

        Assert.Equal("mixed", component.Find("button").GetAttribute("aria-checked"));

        Click(component);
        Assert.True(value);

        Click(component);
        Assert.False(value);

        Click(component);
        Assert.Null(value);
    }

    [Fact]
    public void NullableCheckBox_WithoutIndeterminateNeverReturnsToNull()
    {
        AssertNeverReturnsToNull<OmniCheckBox<bool?>>(item => item.AllowIndeterminate);
    }

    [Fact]
    public void NullableCheckBox_DisabledIgnoresTheClick()
    {
        bool? value = null;
        var component = RenderBoundToggle<OmniCheckBox<bool?>>(value, changed => value = changed, item => item.Disabled, true);

        Click(component);

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
        var component = Render<OmniCheckBox<bool?>>(parameters => parameters
            .BindValue(() => value));

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
        var component = Render<OmniSwitch<bool?>>(parameters => parameters
            .BindValue(() => value, changed => value = changed));

        Click(component);
        Assert.True(value);

        Click(component);
        Assert.False(value);

        Click(component);
        Assert.Null(value);
    }

    [Fact]
    public void NullableSwitch_WithoutIndeterminateNeverReturnsToNull()
    {
        AssertNeverReturnsToNull<OmniSwitch<bool?>>(item => item.AllowIndeterminate);
    }

    [Fact]
    public void NullableSwitch_DisabledIgnoresTheClick()
    {
        bool? value = true;
        var component = RenderBoundToggle<OmniSwitch<bool?>>(value, changed => value = changed, item => item.Disabled, true);

        Click(component);

        Assert.True(value);
    }

    [Fact]
    public void NullableSwitch_DescribesOnlyTheIndeterminateState()
    {
        bool? value = null;
        var component = Render<OmniSwitch<bool?>>(parameters => parameters
            .Add(item => item.IndeterminateDescription, "Indefini")
            .BindValue(() => value, changed => value = changed));

        var button = component.Find("button");
        Assert.Equal("Indefini", button.GetAttribute("aria-description"));
        Assert.Equal("false", button.GetAttribute("aria-checked"));
        Assert.Contains("omni-switch--mixed", button.GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);

        Click(component);

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
            .BindValue(() => bound)).Instance;
        var toggle = Render<SwitchParseProbe>(parameters => parameters
            .BindValue(() => bound)).Instance;

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

    [Fact]
    public void BoolCheckBox_IsTheNativeInput_AndItsTextWrapsItInALabel()
    {
        var value = false;
        var bare = Render<OmniCheckBox<bool>>(parameters => parameters
            .BindValue(() => value));
        var labelled = Render<OmniCheckBox<bool>>(parameters => parameters
            .BindValue(() => value)
            .Add(item => item.ValueChanged, changed => value = changed)
            .AddChildContent("Recevoir les alertes"));

        Assert.Equal("INPUT", bare.Nodes.OfType<AngleSharp.Dom.IElement>().Single().TagName);
        Assert.Empty(bare.FindAll("label"));
        Assert.Empty(bare.FindAll("button"));

        var label = labelled.Find("label.omni-checkbox-label");
        Assert.NotNull(label.QuerySelector("input.omni-checkbox[type=checkbox]"));
        Assert.Equal("Recevoir les alertes", label.QuerySelector(".omni-checkbox-label__text")!.TextContent);

        labelled.Find("input").Change(true);
        Assert.True(value);
    }

    [Fact]
    public void BoolSwitch_HasNoThirdState_AndReturnsToTrueAfterFalse()
    {
        var value = true;
        var component = Render<OmniSwitch<bool>>(parameters => parameters
            .BindValue(() => value, changed => value = changed));

        Assert.False(component.Find("button").HasAttribute("aria-description"));
        Click(component);
        Assert.False(value);
        Click(component);
        Assert.True(value);
        Assert.DoesNotContain("omni-switch--mixed", component.Find("button").GetAttribute("class") ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void Toggles_RefuseAValueTypeOtherThanBoolOrNullableBool()
    {
        var value = 1;

        var checkBox = Assert.Throws<InvalidOperationException>(() => Render<OmniCheckBox<int>>(parameters => parameters
            .BindValue(() => value)));
        var toggle = Assert.Throws<InvalidOperationException>(() => Render<OmniSwitch<int>>(parameters => parameters
            .BindValue(() => value)));

        Assert.Contains("bool?", checkBox.Message, StringComparison.Ordinal);
        Assert.Contains("OmniSwitch", toggle.Message, StringComparison.Ordinal);
    }

    /// <summary>Reaches the protected parser without an EditForm, which never types into a button.</summary>
    private sealed class CheckBoxParseProbe : OmniCheckBox<bool?>
    {
        public bool Parse(string? value, out bool? result, out string message) =>
            TryParseValueFromString(value, out result, out message);
    }

    private sealed class SwitchParseProbe : OmniSwitch<bool?>
    {
        public bool Parse(string? value, out bool? result, out string message) =>
            TryParseValueFromString(value, out result, out message);
    }
}
