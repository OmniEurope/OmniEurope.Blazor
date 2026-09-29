using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniAutocomplete{TValue}"/> follows the editable combobox pattern: the focus stays in the
/// field, the arrows move a visual focus announced through <c>aria-activedescendant</c>, Enter picks,
/// Escape closes, and the suggestions are options, never buttons nested in options.
/// </summary>
public sealed class AutocompleteKeyboardTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Cities =
    [
        new("bxl", "Bruxelles"),
        new("brg", "Bruges"),
        new("brn", "Brno")
    ];

    private IRenderedComponent<OmniAutocomplete<string>> RenderCities(Action<string>? changed = null, string value = "")
    {
        return Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Id, "city")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => changed?.Invoke(next))
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                [.. Cities.Where(city => city.Text.Contains(text, StringComparison.OrdinalIgnoreCase))])));
    }

    [Fact]
    public async Task Suggestions_AreOptions_WithoutButtonsInside()
    {
        var autocomplete = RenderCities();
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "br" });

        var options = autocomplete.FindAll("[role=listbox] > li[role=option]");
        Assert.Equal(3, options.Count);
        Assert.Empty(autocomplete.FindAll("[role=option] button"));
        Assert.Empty(autocomplete.FindAll("[role=listbox] button"));
        Assert.All(options, option => Assert.False(string.IsNullOrEmpty(option.Id)));
    }

    [Fact]
    public async Task Arrows_MoveTheActiveDescendant_AndWrapAtBothEnds()
    {
        var autocomplete = RenderCities();
        var input = autocomplete.Find("input");
        await input.InputAsync(new ChangeEventArgs { Value = "br" });

        Assert.Equal("true", input.GetAttribute("aria-expanded"));
        Assert.Null(autocomplete.Find("input").GetAttribute("aria-activedescendant"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        var options = autocomplete.FindAll("[role=option]");
        Assert.Equal(options[0].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));
        Assert.Equal("true", options[0].GetAttribute("aria-selected"));
        Assert.Contains("omni-autocomplete__option--active", options[0].ClassList);

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[2].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[0].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowUp" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[2].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task HomeAndEnd_JumpToTheEnds_OnlyWhileASuggestionIsHighlighted()
    {
        var autocomplete = RenderCities();
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "br" });

        // Nothing highlighted: Home and End belong to the caret of the text.
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "End" });
        Assert.Null(autocomplete.Find("input").GetAttribute("aria-activedescendant"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "End" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[2].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal(autocomplete.FindAll("[role=option]")[0].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task Enter_PicksTheHighlightedSuggestion()
    {
        string? picked = null;
        var autocomplete = RenderCities(next => picked = next);
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "br" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("brg", picked);
        Assert.Equal("Bruges", autocomplete.Find("input").GetAttribute("value"));
        Assert.Equal("false", autocomplete.Find("input").GetAttribute("aria-expanded"));
        Assert.Empty(autocomplete.FindAll("[role=listbox]"));
    }

    [Fact]
    public async Task Escape_ClosesTheList_AndArrowDownOpensItAgain()
    {
        var autocomplete = RenderCities();
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "br" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal("false", autocomplete.Find("input").GetAttribute("aria-expanded"));
        Assert.Null(autocomplete.Find("input").GetAttribute("aria-activedescendant"));
        Assert.Empty(autocomplete.FindAll("[role=listbox]"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal("true", autocomplete.Find("input").GetAttribute("aria-expanded"));
        Assert.Equal(autocomplete.FindAll("[role=option]")[0].Id, autocomplete.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public async Task ArrowDown_OnAFieldWithoutSuggestions_SearchesItsText()
    {
        var autocomplete = RenderCities(value: "bru");
        Assert.Equal("false", autocomplete.Find("input").GetAttribute("aria-expanded"));

        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });

        Assert.Equal("true", autocomplete.Find("input").GetAttribute("aria-expanded"));
        Assert.Equal(2, autocomplete.FindAll("[role=option]").Count);
    }

    [Fact]
    public async Task ADisabledSuggestion_IsMarked_AndCannotBePicked()
    {
        string? picked = null;
        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => picked = next)
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>([new("x", "Fermé", Disabled: true)])));
        await autocomplete.Find("input").InputAsync(new ChangeEventArgs { Value = "f" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("true", autocomplete.Find("[role=option]").GetAttribute("aria-disabled"));
        Assert.Null(picked);
    }

    [Fact]
    public void Label_IsTheOnlyAriaLabel_SoAFormFieldLabelNamesTheFieldOtherwise()
    {
        var unnamed = RenderCities();
        Assert.Null(unnamed.Find("input").GetAttribute("aria-label"));

        var value = string.Empty;
        var named = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>([]))
            .Add(component => component.Label, "Ville"));
        Assert.Equal("Ville", named.Find("input").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task FilterableDropDown_InheritsTheComboboxKeyboard()
    {
        var value = string.Empty;
        var dropDown = Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Cities)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => value = next));

        await dropDown.Find("input").InputAsync(new ChangeEventArgs { Value = "brn" });
        dropDown.WaitForAssertion(() => Assert.Single(dropDown.FindAll("[role=option]")));
        await dropDown.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(dropDown.Find("[role=option]").Id, dropDown.Find("input").GetAttribute("aria-activedescendant"));
        await dropDown.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("brn", value);
    }
}
