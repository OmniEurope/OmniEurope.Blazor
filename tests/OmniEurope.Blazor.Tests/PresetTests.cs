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

    [Fact]
    public void Registration_RejectsAValueThatNoItemTypeCanFit()
    {
        // Items is an IReadOnlyList<TItem>: a string fits no grid, whatever its item type
        // (audit RCL-PRESET-001, it used to pass and fail every page at render).
        var registry = new OmniPresetRegistry();

        var error = Assert.Throws<ArgumentException>(() => registry.Add(typeof(OmniDataGrid<>), "bad",
            new Dictionary<string, object?> { [nameof(OmniDataGrid<Row>.Items)] = "texte" }));
        Assert.Contains("String", error.Message, StringComparison.Ordinal);

        // A list fits some grid and a null fits any: both wait for the closed type.
        registry.Add(typeof(OmniDataGrid<>), "list", new Dictionary<string, object?> { [nameof(OmniDataGrid<Row>.Items)] = new[] { 1, 2 } });
        registry.Add(typeof(OmniDataGrid<>), "unset", new Dictionary<string, object?> { [nameof(OmniDataGrid<Row>.Items)] = null });
    }

    [Fact]
    public void GenericValueOfTheWrongItemType_FailsAtRender_NamingThePreset()
    {
        Services.AddOmniEuropePreset(typeof(OmniDataGrid<>), "numbers", new Dictionary<string, object?>
        {
            [nameof(OmniDataGrid<Row>.Items)] = new[] { 1, 2 },
        });

        var error = Assert.ThrowsAny<InvalidOperationException>(() =>
            Render<OmniDataGrid<Row>>(parameters => parameters.Add(grid => grid.PresetName, "numbers")));

        Assert.Contains("'numbers'", error.Message, StringComparison.Ordinal);
        Assert.Contains("'Items'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BareGenericValue_FitsTheClosedTypeOrFailsAtRender()
    {
        Services.AddOmniEuropePreset(typeof(OmniStatusBadge<>), "word", new Dictionary<string, object?>
        {
            [nameof(OmniStatusBadge<int>.Value)] = "actif",
        });
        Services.AddOmniEuropePreset(typeof(OmniStatusBadge<>), "empty", new Dictionary<string, object?>
        {
            [nameof(OmniStatusBadge<int>.Value)] = null,
        });

        Assert.Equal("actif", Render<OmniStatusBadge<string>>(parameters => parameters.Add(badge => badge.PresetName, "word").Add(badge => badge.Map, new OmniStatusMap<string>())).Instance.Value);
        var wrong = Assert.ThrowsAny<InvalidOperationException>(() =>
            Render<OmniStatusBadge<int>>(parameters => parameters.Add(badge => badge.PresetName, "word").Add(badge => badge.Map, new OmniStatusMap<int>())));
        Assert.Contains("a String", wrong.Message, StringComparison.Ordinal);
        var empty = Assert.ThrowsAny<InvalidOperationException>(() =>
            Render<OmniStatusBadge<int>>(parameters => parameters.Add(badge => badge.PresetName, "empty").Add(badge => badge.Map, new OmniStatusMap<int>())));
        Assert.Contains("gives 'Value' null", empty.Message, StringComparison.Ordinal);
    }
}
