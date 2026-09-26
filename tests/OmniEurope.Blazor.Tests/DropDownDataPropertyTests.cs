using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Model-backed options (<c>Data</c> with <c>ValueProperty</c> / <c>TextProperty</c>): a property whose value
/// is null yields a null value (or empty text), never the whole item. The fallback to the item converted
/// the model itself to <c>TValue</c> and threw "Object must implement IConvertible" (Aetheus recette R-357).
/// </summary>
public sealed class DropDownDataPropertyTests : OmniBunitContext
{
    private sealed record Choice(int? Id, string? Label);

    private readonly int? _nullable = null;
    private readonly int _number = 0;

    [Fact]
    public void NullValueProperty_YieldsANullOption_InsteadOfConvertingTheItem()
    {
        Choice[] data = [new Choice(null, "(none)"), new Choice(3, "Three")];

        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Data, data)
            .Add(component => component.ValueProperty, nameof(Choice.Id))
            .Add(component => component.TextProperty, nameof(Choice.Label))
            .Add(component => component.Value, (int?)null)
            .Add(component => component.ValueExpression, () => _nullable));

        var options = dropDown.FindAll("option");
        Assert.Equal(["(none)", "Three"], options.Select(option => option.TextContent));
        // The null-valued option is the one matching the null bound value.
        Assert.Equal("0", dropDown.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void NullValueProperty_OnANonNullableValue_YieldsTheDefault()
    {
        Choice[] data = [new Choice(null, "Zero")];

        var dropDown = Render<OmniDropDown<int>>(parameters => parameters
            .Add(component => component.Data, data)
            .Add(component => component.ValueProperty, nameof(Choice.Id))
            .Add(component => component.TextProperty, nameof(Choice.Label))
            .Add(component => component.Value, 0)
            .Add(component => component.ValueExpression, () => _number));

        Assert.Equal("0", dropDown.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void NullTextProperty_YieldsAnEmptyText_InsteadOfTheItemTypeName()
    {
        Choice[] data = [new Choice(1, null)];

        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Data, data)
            .Add(component => component.ValueProperty, nameof(Choice.Id))
            .Add(component => component.TextProperty, nameof(Choice.Label))
            .Add(component => component.ValueExpression, () => _nullable));

        Assert.Equal(string.Empty, dropDown.Find("option").TextContent);
    }

    [Fact]
    public void UnknownValueProperty_FailsExplicitly()
    {
        Choice[] data = [new Choice(1, "One")];

        var error = Assert.Throws<InvalidOperationException>(() => Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Data, data)
            .Add(component => component.ValueProperty, "Missing")
            .Add(component => component.TextProperty, nameof(Choice.Label))
            .Add(component => component.ValueExpression, () => _nullable)));

        Assert.Contains("Missing", error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(Choice), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutValueProperty_TheItemItselfIsTheValue()
    {
        int?[] data = [1, 2];

        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Data, data)
            .Add(component => component.Value, 2)
            .Add(component => component.ValueExpression, () => _nullable));

        Assert.Equal(["1", "2"], dropDown.FindAll("option").Select(option => option.TextContent));
        Assert.Equal("1", dropDown.Find("select").GetAttribute("value"));
    }
}
