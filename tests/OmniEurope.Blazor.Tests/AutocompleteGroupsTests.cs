using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniOption{TValue}.Group"/> in the suggestion list of <see cref="OmniAutocomplete{TValue}"/>
/// (and so of a filterable <see cref="OmniDropDown{TValue}"/>): one heading per run of consecutive options of
/// a group, a valid ARIA group structure, and a heading that is never an option.
/// </summary>
public sealed class AutocompleteGroupsTests : OmniBunitContext
{
    private static readonly IReadOnlyList<OmniOption<string>> Places =
    [
        new("bxl", "Bruxelles", Group: "Belgique"),
        new("lge", "Liege", Group: "Belgique"),
        new("par", "Paris", Group: "France"),
        new("any", "Anywhere"),
        new("lyo", "Lyon", Group: "France"),
        new("brn", "Brno", Group: "Tchequie")
    ];

    private IRenderedComponent<OmniAutocomplete<string>> RenderPlaces(Action<string>? changed = null, IReadOnlyList<OmniOption<string>>? options = null)
    {
        var source = options ?? Places;
        var value = string.Empty;
        return Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Id, "place")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, (string next) => changed?.Invoke(next))
            .Add(component => component.Debounce, TimeSpan.Zero)
            .Add(component => component.MinimumLength, 0)
            .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(
                [.. source.Where(place => place.Text.Contains(text, StringComparison.OrdinalIgnoreCase))])));
    }

    private static async Task TypeAsync(IRenderedComponent<OmniAutocomplete<string>> autocomplete, string text) =>
        await autocomplete.Find("input").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = text });

    private static async Task PressAsync(IRenderedComponent<OmniAutocomplete<string>> autocomplete, string key) =>
        await autocomplete.Find("input").KeyDownAsync(new KeyboardEventArgs { Key = key });

    [Fact]
    public async Task EachRunOfAGroup_GetsOneHeading_InSourceOrder()
    {
        var autocomplete = RenderPlaces();
        await TypeAsync(autocomplete, string.Empty);
        await PressAsync(autocomplete, "ArrowDown");

        var headings = autocomplete.FindAll(".omni-autocomplete__group-heading");
        // France comes back after the ungrouped option: a second heading, never a reordering.
        Assert.Equal(["Belgique", "France", "France", "Tchequie"], headings.Select(heading => heading.TextContent.Trim()));
        Assert.Equal(["Bruxelles", "Liege", "Paris", "Anywhere", "Lyon", "Brno"], autocomplete.FindAll("[role=option]").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public async Task AGroupedRun_IsARoleGroup_LabelledByAPresentationalHeading()
    {
        var autocomplete = RenderPlaces();
        await TypeAsync(autocomplete, "br");

        var group = autocomplete.Find("[role=listbox] [role=group]");
        var heading = group.QuerySelector("[role=presentation].omni-autocomplete__group-heading")!;
        Assert.Equal("Belgique", heading.TextContent.Trim());
        Assert.False(string.IsNullOrEmpty(heading.Id));
        Assert.Equal(heading.Id, group.GetAttribute("aria-labelledby"));
        Assert.Single(autocomplete.FindAll("#" + heading.Id));
        // The options are the group's children and the heading is not one of them.
        Assert.Single(group.QuerySelectorAll("[role=option]"));
        Assert.Equal("presentation", group.ParentElement!.GetAttribute("role"));
        Assert.DoesNotContain(autocomplete.FindAll("[role=option]"), option => option.ClassList.Contains("omni-autocomplete__group-heading"));
    }

    [Fact]
    public async Task UngroupedOptions_RenderNoHeading_AndStayDirectChildrenOfTheListbox()
    {
        var plain = new List<OmniOption<string>> { new("a", "Alpha"), new("b", "Alphabet") };
        var autocomplete = RenderPlaces(options: plain);
        await TypeAsync(autocomplete, "alpha");

        Assert.Empty(autocomplete.FindAll(".omni-autocomplete__group-heading"));
        Assert.Empty(autocomplete.FindAll("[role=group]"));
        Assert.Equal(2, autocomplete.FindAll("[role=listbox] > li[role=option]").Count);
    }

    [Fact]
    public async Task ABlankGroup_CountsAsNoGroup()
    {
        var blank = new List<OmniOption<string>> { new("a", "Alpha", Group: ""), new("b", "Alphabet", Group: "  ") };
        var autocomplete = RenderPlaces(options: blank);
        await TypeAsync(autocomplete, "alpha");

        Assert.Empty(autocomplete.FindAll(".omni-autocomplete__group-heading"));
        Assert.Equal(2, autocomplete.FindAll("[role=listbox] > li[role=option]").Count);
    }

    [Fact]
    public async Task Arrows_SkipTheHeadings_AndTheActiveDescendantIsAnOption()
    {
        var autocomplete = RenderPlaces();
        await TypeAsync(autocomplete, string.Empty);

        var expected = new[] { "Bruxelles", "Liege", "Paris", "Anywhere", "Lyon", "Brno" };
        foreach (var text in expected)
        {
            await PressAsync(autocomplete, "ArrowDown");
            var activeId = autocomplete.Find("input").GetAttribute("aria-activedescendant");
            var active = autocomplete.Find("#" + activeId);
            Assert.Equal("option", active.GetAttribute("role"));
            Assert.Equal(text, active.TextContent.Trim());
            Assert.Single(autocomplete.FindAll("[aria-selected=true]"));
        }

        // Wraps from the last option to the first, never onto a heading.
        await PressAsync(autocomplete, "ArrowDown");
        Assert.Equal("Bruxelles", autocomplete.Find("#" + autocomplete.Find("input").GetAttribute("aria-activedescendant")).TextContent.Trim());
        await PressAsync(autocomplete, "ArrowUp");
        Assert.Equal("Brno", autocomplete.Find("#" + autocomplete.Find("input").GetAttribute("aria-activedescendant")).TextContent.Trim());
        await PressAsync(autocomplete, "Home");
        Assert.Equal("Bruxelles", autocomplete.Find("#" + autocomplete.Find("input").GetAttribute("aria-activedescendant")).TextContent.Trim());
        await PressAsync(autocomplete, "End");
        Assert.Equal("Brno", autocomplete.Find("#" + autocomplete.Find("input").GetAttribute("aria-activedescendant")).TextContent.Trim());
    }

    [Fact]
    public async Task Enter_PicksTheRightOption_AcrossHeadings()
    {
        string? picked = null;
        var autocomplete = RenderPlaces(next => picked = next);
        await TypeAsync(autocomplete, string.Empty);
        for (var step = 0; step < 3; step++)
        {
            await PressAsync(autocomplete, "ArrowDown");
        }

        await PressAsync(autocomplete, "Enter");

        Assert.Equal("par", picked);
        Assert.Equal("Paris", autocomplete.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task ClickingAnOption_PicksItsOwnValue_WhateverTheHeadingsBefore()
    {
        string? picked = null;
        var autocomplete = RenderPlaces(next => picked = next);
        await TypeAsync(autocomplete, string.Empty);

        await autocomplete.FindAll("[role=option]")[4].ClickAsync();
        Assert.Equal("lyo", picked);
    }

    [Fact]
    public async Task Filtering_DropsTheHeadingOfAnEmptiedGroup()
    {
        var autocomplete = RenderPlaces();
        await TypeAsync(autocomplete, "l");

        // Liege (Belgique) and Lyon (France) match, Paris and Bruxelles do not.
        Assert.Equal(["Belgique", "France"], autocomplete.FindAll(".omni-autocomplete__group-heading").Select(heading => heading.TextContent.Trim()));

        await TypeAsync(autocomplete, "lyon");
        Assert.Equal(["France"], autocomplete.FindAll(".omni-autocomplete__group-heading").Select(heading => heading.TextContent.Trim()));
        Assert.Single(autocomplete.FindAll("[role=group]"));
    }

    [Fact]
    public async Task TheAnnouncement_CountsOptionsOnly_NotHeadings()
    {
        var autocomplete = RenderPlaces();
        await TypeAsync(autocomplete, string.Empty);

        var announced = autocomplete.Find("[role=status]").TextContent;
        Assert.Contains("6", announced, StringComparison.Ordinal);
        Assert.Equal(Places.Count, autocomplete.FindAll("[role=option]").Count);
    }

    [Fact]
    public void FilterableDropDown_ShowsTheGroupHeadings()
    {
        var value = string.Empty;
        var dropDown = Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Places)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        dropDown.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        dropDown.WaitForAssertion(() => Assert.Equal(
            ["Belgique", "France", "France", "Tchequie"],
            dropDown.FindAll(".omni-autocomplete__group-heading").Select(heading => heading.TextContent.Trim())));
        Assert.Equal(Places.Count, dropDown.FindAll("[role=option]").Count);
    }

    [Fact]
    public void FilterableDropDown_WithAnEmptyChoice_ListsItWithoutHeading()
    {
        var value = string.Empty;
        var dropDown = Render<OmniDropDown<string>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.AllowEmpty, true)
            .Add(component => component.Options, Places)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        dropDown.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        dropDown.WaitForAssertion(() => Assert.Equal(Places.Count + 1, dropDown.FindAll("[role=option]").Count));
        // The empty choice and the ungrouped option are the listbox's direct options.
        Assert.Equal(2, dropDown.FindAll("[role=listbox] > li[role=option]").Count);
        Assert.Equal(4, dropDown.FindAll(".omni-autocomplete__group-heading").Count);
    }

    [Fact]
    public void OptionHover_IsAWashOfTheTextColour_NotASurfaceToken()
    {
        var hover = ShippedLookTests.Body(".omni-autocomplete__option:hover:not(.omni-autocomplete__option--disabled),\n.omni-autocomplete__option--active");
        // A surface token would vanish on a theme whose list is already that surface.
        Assert.Equal("color-mix(in srgb, var(--omni-color-text) 8%, transparent)", ShippedLookTests.Value(hover, "background"));
        Assert.DoesNotContain("surface", ShippedLookTests.Value(hover, "background"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(".omni-combo__option:hover,\n.omni-combo__option--active")]
    [InlineData(".omni-multi-select__option:hover")]
    [InlineData(".omni-multi-select-compact__option:hover")]
    public void TheOtherListHovers_AreTheSameWash(string selector)
    {
        Assert.Equal("color-mix(in srgb, var(--omni-color-text) 8%, transparent)", ShippedLookTests.Value(ShippedLookTests.Body(selector), "background"));
    }

    [Fact]
    public void ActiveOption_KeepsItsAccentFrame_AndHeadingsAreDiscreetAndNotHoverable()
    {
        var active = ShippedLookTests.Body(".omni-autocomplete__option--active");
        Assert.Contains("var(--omni-color-accent)", ShippedLookTests.Value(active, "outline"), StringComparison.Ordinal);

        var heading = ShippedLookTests.Body(".omni-autocomplete__group-heading");
        Assert.Equal("var(--omni-color-text-muted)", ShippedLookTests.Value(heading, "color"));
        Assert.Equal("var(--omni-font-size-sm)", ShippedLookTests.Value(heading, "font-size"));
        Assert.Equal("600", ShippedLookTests.Value(heading, "font-weight"));
        Assert.Equal("default", ShippedLookTests.Value(heading, "cursor"));
        Assert.DoesNotContain(ShippedLookTests.Rules(), rule => rule.Selector.Contains("group-heading:hover", StringComparison.Ordinal));
    }
}
