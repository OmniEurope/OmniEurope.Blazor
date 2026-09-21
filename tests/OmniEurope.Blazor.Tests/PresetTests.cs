using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class PresetTests : OmniBunitContext
{
    private sealed record Row(int Id);

    private static readonly Dictionary<string, object?> CompactGrid = new()
    {
        [nameof(OmniDataGrid<Row>.AllowSorting)] = true,
        [nameof(OmniDataGrid<Row>.AllowAlternatingRows)] = true,
        [nameof(OmniDataGrid<Row>.Density)] = OmniDensity.Compact,
    };

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? extra = null) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters.Add(grid => grid.Items, new[] { new Row(1) });
            extra?.Invoke(parameters);
        });

    [Fact]
    public void DefaultPreset_OfAnOpenGenericGrid_AppliesToAnyItemType()
    {
        Services.AddOmniEuropePreset(typeof(OmniDataGrid<>), "compact", CompactGrid, isDefault: true);

        var grid = RenderGrid().Instance;

        Assert.True(grid.AllowSorting);
        Assert.True(grid.AllowAlternatingRows);
        Assert.Equal(OmniDensity.Compact, grid.Density);
    }

    [Fact]
    public void ExplicitParameter_WinsOverThePreset_EvenAfterARerender()
    {
        Services.AddOmniEuropePreset(typeof(OmniDataGrid<>), "compact", CompactGrid, isDefault: true);

        var rendered = RenderGrid(parameters => parameters.Add(grid => grid.AllowSorting, false));
        Assert.False(rendered.Instance.AllowSorting);
        Assert.True(rendered.Instance.AllowAlternatingRows);

        rendered.Render(parameters => parameters.Add(grid => grid.AllowSorting, false));
        Assert.False(rendered.Instance.AllowSorting);
        Assert.Equal(OmniDensity.Compact, rendered.Instance.Density);
    }

    [Fact]
    public void NamedPreset_IsTakenOnlyWhenAsked_AndNoneOptsOutOfTheDefault()
    {
        Services.AddOmniEuropePreset(typeof(OmniDataGrid<>), "compact", CompactGrid, isDefault: true);
        Services.AddOmniEuropePreset(typeof(OmniDataGrid<>), "sparse", new Dictionary<string, object?>
        {
            [nameof(OmniDataGrid<Row>.Density)] = OmniDensity.Comfortable,
        });

        var named = RenderGrid(parameters => parameters.Add(grid => grid.PresetName, "sparse")).Instance;
        var none = RenderGrid(parameters => parameters.Add(grid => grid.PresetName, OmniPresetRegistry.None)).Instance;

        Assert.Equal(OmniDensity.Comfortable, named.Density);
        Assert.False(named.AllowAlternatingRows);
        Assert.NotEqual(OmniDensity.Compact, none.Density);
        Assert.False(none.AllowAlternatingRows);
    }

    [Fact]
    public void UnknownPresetName_FailsLoudly_WithoutAnyRegistry()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() =>
            RenderGrid(parameters => parameters.Add(grid => grid.PresetName, "missing")));
        Assert.Contains("'missing'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownPresetName_FailsLoudly_NextToRegisteredOnes()
    {
        Services.AddOmniEuropePreset(typeof(OmniDataGrid<>), "compact", CompactGrid);
        var error = Assert.ThrowsAny<InvalidOperationException>(() =>
            RenderGrid(parameters => parameters.Add(grid => grid.PresetName, "missing")));
        Assert.Contains("OmniDataGrid", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InputComponent_TakesItsDefaultPreset_UnderAnExplicitValue()
    {
        Services.AddOmniEuropePreset(typeof(OmniTextBox), "hinted", new Dictionary<string, object?>
        {
            [nameof(OmniTextBox.Placeholder)] = "from preset",
        }, isDefault: true);

        var value = "a";
        var preset = Render<OmniTextBox>(parameters => parameters.Add(box => box.Value, value).Add(box => box.ValueExpression, () => value));
        var explicitValue = Render<OmniTextBox>(parameters => parameters
            .Add(box => box.Value, value)
            .Add(box => box.ValueExpression, () => value)
            .Add(box => box.Placeholder, "explicit"));

        Assert.Equal("from preset", preset.Find("input").GetAttribute("placeholder"));
        Assert.Equal("explicit", explicitValue.Find("input").GetAttribute("placeholder"));
    }

    [Fact]
    public void Registration_RejectsMistakesAtStartup()
    {
        var registry = new OmniPresetRegistry();
        Dictionary<string, object?> One(string parameter, object? value) => new() { [parameter] = value };

        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "x", One("NoSuchParameter", true)));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "x", One(nameof(OmniDataGrid<Row>.AllowSorting), "yes")));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "x", One(nameof(OmniDataGrid<Row>.AllowSorting), null)));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "x", One(nameof(OmniDataGrid<Row>.AdditionalAttributes), null)));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "x", One(nameof(OmniDataGrid<Row>.PresetName), "y")));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<Row>), "x", CompactGrid));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(PresetTests), "x", CompactGrid));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), OmniPresetRegistry.None, CompactGrid));

        registry.Add(typeof(OmniDataGrid<>), "compact", CompactGrid, isDefault: true);
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "compact", CompactGrid));
        Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "other", CompactGrid, isDefault: true));
    }
}
