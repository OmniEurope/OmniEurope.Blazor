using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The selection controls at their edges: no value yet, a choice made on a locked control or option, a
/// change the browser cannot send, values of no option, text parsing that none of them offers, an
/// unparsable colour or number, invalid slider settings, and the autocomplete's search, keys and errors.
/// </summary>
public sealed class SelectionEdgeTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Systems =
    [
        new("linux", "Linux"),
        new("mac", "macOS", Disabled: true),
        new("windows", "Windows"),
    ];

    private readonly string? _text = null;

    private readonly IReadOnlyList<string>? _list = null;

    private readonly Silent? _silent = null;

    // ---- check box list ---------------------------------------------------------------------------

    [Fact]
    public void CheckBoxList_FromNoValue_AddsThenRemoves_AndIgnoresLockedOrStrangeChanges()
    {
        IReadOnlyList<string>? value = null;
        var list = Render<OmniCheckBoxList<string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => _list!));
        var boxes = () => list.FindAll("input[type=checkbox]");
        Assert.All(boxes(), box => Assert.False(box.HasAttribute("checked")));

        boxes()[0].Change(true);
        Assert.Equal(["linux"], value);
        list.Render(parameters => parameters.Add(component => component.Value, value));
        boxes()[1].Change(true);
        boxes()[2].Change("oui");
        Assert.Equal(["linux"], value);

        boxes()[0].Change(false);
        Assert.Empty(value!);

        list.Render(parameters => parameters.Add(component => component.Disabled, true));
        boxes()[2].Change(true);
        Assert.Empty(value!);
    }

    [Fact]
    public void ChoiceControls_RefuseTextParsing()
    {
        var checks = Render<ParsingCheckBoxList>(parameters => parameters.Add(component => component.ValueExpression, () => _list!));
        var radios = Render<ParsingRadioList>(parameters => parameters.Add(component => component.ValueExpression, () => _text!));
        var bar = Render<ParsingSelectBar>(parameters => parameters.Add(component => component.ValueExpression, () => _text!));
        var autocomplete = Render<ParsingAutocomplete>(parameters => parameters.Add(component => component.ValueExpression, () => _text!));

        Assert.False(checks.Instance.Parse("a", out var message1));
        Assert.False(radios.Instance.Parse("a", out var message2));
        Assert.False(bar.Instance.Parse("a", out var message3));
        Assert.False(autocomplete.Instance.Parse("a", out var message4));
        Assert.All([message1, message2, message3, message4], message => Assert.False(string.IsNullOrEmpty(message)));
    }

    public sealed class ParsingCheckBoxList : OmniCheckBoxList<string>
    {
        public bool Parse(string text, out string message) => TryParseValueFromString(text, out _, out message);
    }

    public sealed class ParsingRadioList : OmniRadioButtonList<string>
    {
        public bool Parse(string text, out string message) => TryParseValueFromString(text, out _, out message);
    }

    public sealed class ParsingSelectBar : OmniSelectBar<string>
    {
        public bool Parse(string text, out string message) => TryParseValueFromString(text, out _, out message);
    }

    public sealed class ParsingAutocomplete : OmniAutocomplete<string>
    {
        public bool Parse(string text, out string message) => TryParseValueFromString(text, out _, out message);
    }

    // ---- radio list and select bar ----------------------------------------------------------------

    [Fact]
    public void RadioListAndSelectBar_LockedOrOnALockedOption_ChooseNothing()
    {
        var changes = new List<string>();
        var radios = Render<OmniRadioButtonList<string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text!));
        radios.FindAll("input[type=radio]")[1].Change(true);
        radios.Render(parameters => parameters.Add(component => component.Disabled, true));
        radios.FindAll("input[type=radio]")[0].Change(true);

        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Disabled, true)
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text!));
        bar.FindAll(".omni-select-bar__item")[0].Click();

        Assert.Empty(changes);
    }

    [Fact]
    public async Task SelectBar_OnALostCircuit_StartsAndLeavesQuietly()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["configureSelectBarOverflow"] = new JSDisconnectedException("perdu");
        runtime.Module.CallFailures["disposeTabsOverflow"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.ValueExpression, () => _text!));

        await bar.Instance.DisposeAsync();

        Assert.Equal(["configureSelectBarOverflow", "disposeTabsOverflow"], runtime.Module.Calls);
    }

    // ---- colour and slider ------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("#12345")]
    [InlineData("1234567")]
    [InlineData("#GGGGGG")]
    public void ColorPicker_RefusesWhatIsNotAColour_AndKeepsTheCurrentOne(string? text)
    {
        string? value = null;
        var picker = Render<OmniColorPicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => _text!)
            .Add(component => component.ShowValue, false));

        picker.Find("input").Input(text);

        Assert.Null(value);
        Assert.Empty(picker.FindAll("output"));
    }

    [Fact]
    public void ColorPicker_RefusedText_KeepsTheColourAlreadyChosen()
    {
        var value = "#112233";
        var picker = Render<OmniColorPicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next!)
            .Add(component => component.ValueExpression, () => _text!));

        picker.Find("input").Input("rouge");

        Assert.Equal("#112233", value);
        Assert.Equal("#112233", picker.Find("output").TextContent);
    }

    [Fact]
    public void RadioList_WithoutOptions_DrawsNoButton() =>
        Assert.Empty(Render<OmniRadioButtonList<string>>(parameters => parameters.Add(component => component.ValueExpression, () => _text!)).FindAll("input"));

    [Fact]
    public void Slider_ShowsItsValueWhenTheFormatGivesNothing_RefusesOutOfBounds_AndCommitsNumbersOnly()
    {
        var value = 10d;
        var commits = new List<double>();
        var slider = Render<OmniSlider>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.FormatValue, _ => null!)
            .Add(component => component.OnValueCommit, next => commits.Add(next)));
        Assert.Equal(10d.ToString(System.Globalization.CultureInfo.CurrentCulture), slider.Find("output").TextContent);

        slider.Find("input").Input("500");
        slider.Find("input").Input((object?)null);
        Assert.Equal(10d, value);

        slider.Find("input").Change("abc");
        slider.Find("input").Change((object?)null);
        slider.Find("input").Change("40");
        Assert.Equal([40d], commits);

        slider.Render(parameters => parameters.Add(component => component.FormatValue, number => $"{number} %"));
        Assert.Equal("10 %", slider.Find("output").TextContent);
    }

    [Theory]
    [InlineData(double.NaN, 100d, 1d, 0d)]
    [InlineData(0d, double.PositiveInfinity, 1d, 0d)]
    [InlineData(0d, 100d, double.NaN, 0d)]
    [InlineData(0d, 100d, 1d, double.NaN)]
    [InlineData(0d, 100d, 1d, -1d)]
    public void Slider_InvalidSettings_AreRefused(double minimum, double maximum, double step, double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniSlider>(parameters => parameters
            .Add(component => component.Minimum, minimum)
            .Add(component => component.Maximum, maximum)
            .Add(component => component.Step, step)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)));

    // ---- drop down and list box -------------------------------------------------------------------

    [Fact]
    public void DropDown_DrawsGroups_AndIgnoresChangesToNoOption()
    {
        var changes = new List<string>();
        IReadOnlyList<OmniOption<string>> grouped = [new("a", "A"), new("b", "B", Group: "Lettres"), new("c", "C", Disabled: true)];
        var dropDown = Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Options, grouped)
            .Add(component => component.ValueChanged, next => changes.Add(next))
            .Add(component => component.ValueExpression, () => _text!));

        Assert.Equal("Lettres", dropDown.Find("optgroup").GetAttribute("label"));
        foreach (var raw in new object?[] { null, "-1", "9", "2", "x" })
        {
            dropDown.Find("select").Change(raw);
        }

        Assert.Empty(changes);
    }

    [Fact]
    public void FilterableDropDown_WritesAValueOfNoOptionByItsText_AndNullAsNothing()
    {
        var dropDown = Render<OmniDropDown<string?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string?>("a", "A")])
            .Add(component => component.Filterable, true)
            .Add(component => component.Value, "z")
            .Add(component => component.ValueExpression, () => _text));
        Assert.Equal("z", dropDown.Find("input").GetAttribute("value"));

        dropDown.Render(parameters => parameters.Add(component => component.Value, "a"));
        Assert.Equal("A", dropDown.Find("input").GetAttribute("value"));

        var silent = Render<OmniDropDown<Silent>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<Silent>(new Silent(), "Muet")])
            .Add(component => component.Filterable, true)
            .Add(component => component.Value, new Silent())
            .Add(component => component.ValueExpression, () => _silent!));
        Assert.Equal(string.Empty, silent.Find("input").GetAttribute("value") ?? string.Empty);
    }

    [Fact]
    public void ListBox_ReadsEveryShapeOfMultipleChange_AndIgnoresSingleChangesToNoOption()
    {
        IReadOnlyList<string>? many = null;
        var multiple = Render<OmniListBox<string, IReadOnlyList<string>?>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Multiple, true)
            .Add(component => component.Value, many)
            .Add(component => component.ValueChanged, next => many = next)
            .Add(component => component.ValueExpression, () => _list));
        Assert.Empty(multiple.FindAll("option[selected]"));

        multiple.Find("select").Change(new ChangeEventArgs { Value = new List<string> { "0", "1", "x" } });
        Assert.Equal(["linux"], many);
        multiple.Find("select").Change("2");
        Assert.Equal(["windows"], many);
        multiple.Find("select").Change((object?)null);
        Assert.Empty(many!);

        var singles = new List<string?>();
        var single = Render<OmniListBox<string?, string?>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string?>(null, "Aucun"), new OmniOption<string?>("a", "A", Disabled: true)])
            .Add(component => component.Value, null)
            .Add(component => component.ValueChanged, next => singles.Add(next))
            .Add(component => component.ValueExpression, () => _text));
        Assert.True(single.FindAll("option")[0].HasAttribute("selected"));
        foreach (var raw in new object?[] { "x", "-1", "5", "1", null })
        {
            single.Find("select").Change(raw);
        }

        Assert.Empty(singles);
    }

    // ---- multi select -----------------------------------------------------------------------------

    [Fact]
    public void MultiSelect_FromNoValue_TogglesOnlyRealChanges_AndIgnoresTheSameFilter()
    {
        IReadOnlyList<string>? value = null;
        var filters = new List<string?>();
        var select = Render<OmniMultiSelect<string>>(parameters => parameters
            .Add(component => component.Options, Systems)
            .Add(component => component.Filterable, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueChanged, next => value = next)
            .Add(component => component.FilterTextChanged, text => filters.Add(text))
            .Add(component => component.ValueExpression, () => _list!));
        var boxes = () => select.FindAll(".omni-multi-select-compact__option input");

        boxes()[0].Change(false);
        Assert.Null(value);
        boxes()[0].Change(true);
        Assert.Equal(["linux"], value);
        select.Render(parameters => parameters.Add(component => component.Value, value));
        boxes()[0].Change(false);
        Assert.Empty(value!);

        select.Find("input[type=search], .omni-multi-select-compact__panel input:not([type=checkbox])").Input("li");
        select.Find("input[type=search], .omni-multi-select-compact__panel input:not([type=checkbox])").Input("li");
        Assert.Equal(["li"], filters);
    }

    // ---- autocomplete -----------------------------------------------------------------------------

    private IRenderedComponent<OmniAutocomplete<string>> RenderCities(Action<ComponentParameterCollectionBuilder<OmniAutocomplete<string>>>? extra = null) =>
        Render<OmniAutocomplete<string>>(parameters =>
        {
            parameters
                .Add(component => component.Id, "city")
                .Add(component => component.ValueExpression, () => _text!)
                .Add(component => component.Debounce, TimeSpan.FromSeconds(-1))
                .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                    [.. Systems.Where(option => option.Text.Contains(text, StringComparison.OrdinalIgnoreCase))]));
            extra?.Invoke(parameters);
        });

    [Fact]
    public async Task Autocomplete_ShortOrNullText_ListsNothing_AndAnUnsearchableFieldNeither()
    {
        var autocomplete = RenderCities(parameters => parameters.Add(component => component.MinimumLength, 2));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "w" });
        Assert.Empty(autocomplete.FindAll("[role=option]"));
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = null });
        Assert.Empty(autocomplete.FindAll("[role=option]"));

        var unsearchable = Render<OmniAutocomplete<string>>(parameters => parameters.Add(component => component.ValueExpression, () => _text!));
        await unsearchable.Find("input").InputAsync(new ChangeEventArgs { Value = "lin" });
        Assert.Empty(unsearchable.FindAll("[role=option]"));
    }

    [Fact]
    public async Task Autocomplete_UpFromTheSecond_GoesToTheFirst_AndUpOnNothingFoundHighlightsNothing()
    {
        var autocomplete = RenderCities();
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "i" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[0].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));

        var nothing = RenderCities();
        await nothing.Find("input").InputAsync(new ChangeEventArgs { Value = "zz" });
        await nothing.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Null(nothing.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task Autocomplete_ArrowUpOnClosedSuggestions_ReopensOnTheLast_AndAParentValueBeforeAnySearchIsShown()
    {
        var autocomplete = RenderCities();
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "i" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[^1].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));

        var fresh = RenderCities();
        fresh.Render(parameters => parameters.Add(component => component.Value, "linux"));
        Assert.Equal("linux", fresh.Find("input").GetAttribute("value"));

        await fresh.Find("input").InputAsync(new ChangeEventArgs { Value = "w" });
        fresh.Render(parameters => parameters.Add(component => component.Value, "windows"));
        Assert.Empty(fresh.FindAll("[role=option]"));
    }

    [Fact]
    public async Task Autocomplete_Locked_IgnoresKeys()
    {
        var autocomplete = RenderCities(parameters => parameters.Add(component => component.Disabled, true));
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "i" });

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.Null(autocomplete.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task Autocomplete_FailedSearch_ShowsTheHostMessageAndContent()
    {
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.ValueExpression, () => _text!)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.SearchErrorMessage, "Annuaire indisponible.")
            .Add(component => component.ErrorContent, error => builder => builder.AddContent(0, $"Erreur : {error.Message}"))
            .Add(component => component.Search, (_, _) => throw new InvalidOperationException("down")));

        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "x" });

        Assert.Contains("Erreur : down", autocomplete.Markup, StringComparison.Ordinal);
        Assert.Contains("Annuaire indisponible.", autocomplete.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Autocomplete_ValueReplacedByTheParent_IsWrittenByItsTextOrNothing()
    {
        var autocomplete = RenderCities(parameters => parameters.Add(component => component.FormatValue, _ => null!));

        autocomplete.Render(parameters => parameters.Add(component => component.Value, "linux"));
        Assert.Equal("linux", autocomplete.Find("input").GetAttribute("value"));

        var silent = Render<OmniAutocomplete<Silent>>(parameters => parameters
            .Add(component => component.Value, new Silent())
            .Add(component => component.ValueExpression, () => _silent!));
        Assert.Equal(string.Empty, silent.Find("input").GetAttribute("value") ?? string.Empty);
    }

    /// <summary>A value whose text is null, as a careless ToString can return.</summary>
    public sealed class Silent
    {
        public override string? ToString() => null;
    }
}
