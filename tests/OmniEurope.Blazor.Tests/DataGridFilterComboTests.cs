using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>OmniDataGridFilterCombo: the list it opens, the keyboard that walks it and the pick.</summary>
public sealed class DataGridFilterComboTests : OmniBunitContext
{
    private static readonly string[] Cities = ["Bruxelles", "Liège", "Lille", "Namur"];

    private IRenderedComponent<OmniDataGridFilterCombo> RenderCombo(string value, List<string> changes, Action? onPick = null, int max = 50) =>
        Render<OmniDataGridFilterCombo>(parameters => parameters
            .Add(component => component.Id, "city")
            .Add(component => component.Value, value)
            .Add(component => component.Suggestions, Cities)
            .Add(component => component.MaxSuggestions, max)
            .Add(component => component.ValueChanged, text => changes.Add(text))
            .Add(component => component.OnPick, () => onPick?.Invoke()));

    private static IReadOnlyList<string> Options(IRenderedComponent<OmniDataGridFilterCombo> combo) =>
        [.. combo.FindAll("li[role=option]").Select(option => option.TextContent)];

    [Fact]
    public void Focus_OpensTheMatchingSuggestions_AndBlurClosesThem()
    {
        var combo = RenderCombo("li", []);
        var input = combo.Find("input");
        Assert.Equal("false", input.GetAttribute("aria-expanded"));

        input.Focus();
        Assert.Equal(["Liège", "Lille"], Options(combo));
        Assert.Equal("true", combo.Find("input").GetAttribute("aria-expanded"));
        Assert.Equal("city-options", combo.Find("input").GetAttribute("aria-controls"));

        combo.Find("input").Blur();
        Assert.Empty(Options(combo));
    }

    [Fact]
    public void EmptyBox_ListsEverySuggestion_UpToTheMaximum()
    {
        var combo = RenderCombo(string.Empty, [], max: 2);
        combo.Find("input").Focus();
        Assert.Equal(["Bruxelles", "Liège"], Options(combo));

        // A maximum under one still lists one suggestion.
        var single = RenderCombo(string.Empty, [], max: 0);
        single.Find("input").Focus();
        Assert.Single(Options(single));
    }

    [Fact]
    public void Typing_ReportsTheText_AndOpensTheList()
    {
        var changes = new List<string>();
        var combo = RenderCombo(string.Empty, changes);

        combo.Find("input").Input("nam");
        combo.Find("input").Input((object?)null);

        Assert.Equal(["nam", string.Empty], changes);
        Assert.Equal("true", combo.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Arrows_WalkTheList_WrappingAround_AndMarkTheActiveOption()
    {
        var combo = RenderCombo(string.Empty, []);
        var input = combo.Find("input");

        input.KeyDown("ArrowDown");
        Assert.Equal("city-options-0", combo.Find("input").GetAttribute("aria-activedescendant"));
        Assert.Equal("true", combo.Find("#city-options-0").GetAttribute("aria-selected"));
        Assert.Contains("omni-combo__option--active", combo.Find("#city-options-0").ClassList);
        Assert.DoesNotContain("omni-combo__option--active", combo.Find("#city-options-1").ClassList);

        input.KeyDown("ArrowUp");
        Assert.Equal("city-options-3", combo.Find("input").GetAttribute("aria-activedescendant"));
        input.KeyDown("ArrowDown");
        Assert.Equal("city-options-0", combo.Find("input").GetAttribute("aria-activedescendant"));
        input.KeyDown("ArrowDown");
        input.KeyDown("ArrowUp");
        Assert.Equal("city-options-0", combo.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public void ArrowUp_FromNoActiveOption_TakesTheLast()
    {
        var combo = RenderCombo(string.Empty, []);

        combo.Find("input").KeyDown("ArrowUp");

        Assert.Equal("city-options-3", combo.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public void Enter_PicksTheActiveOption_ReportsItAndCloses()
    {
        var changes = new List<string>();
        var picks = 0;
        var combo = RenderCombo("li", changes, () => picks++);
        var input = combo.Find("input");

        input.KeyDown("ArrowDown");
        input.KeyDown("ArrowDown");
        input.KeyDown("Enter");

        Assert.Equal(["Lille"], changes);
        Assert.Equal(1, picks);
        Assert.Empty(Options(combo));
    }

    [Fact]
    public void Enter_WithoutAnActiveOption_PicksNothing()
    {
        var changes = new List<string>();
        var combo = RenderCombo("li", changes);
        combo.Find("input").Focus();

        combo.Find("input").KeyDown("Enter");

        Assert.Empty(changes);
        Assert.Equal(2, Options(combo).Count);
    }

    [Fact]
    public void Enter_OnAnOptionTheHostNarrowedAway_PicksNothing()
    {
        var changes = new List<string>();
        var combo = RenderCombo(string.Empty, changes);
        combo.Find("input").KeyDown("ArrowUp");

        // The host narrows the text: the active position is past the one remaining match.
        combo.Render(parameters => parameters.Add(component => component.Value, "nam"));
        combo.Find("input").KeyDown("Enter");

        Assert.Empty(changes);
    }

    [Fact]
    public void Enter_OnAClosedList_PicksNothing()
    {
        var changes = new List<string>();
        var combo = RenderCombo(string.Empty, changes);
        combo.Find("input").KeyDown("ArrowDown");
        combo.Find("input").Blur();

        combo.Find("input").KeyDown("Enter");

        Assert.Empty(changes);
    }

    [Fact]
    public void Escape_ClosesTheList_AndOtherKeysLeaveIt()
    {
        var combo = RenderCombo(string.Empty, []);
        combo.Find("input").KeyDown("ArrowDown");

        combo.Find("input").KeyDown("Tab");
        Assert.Equal(4, Options(combo).Count);

        combo.Find("input").KeyDown("Escape");
        Assert.Empty(Options(combo));
        Assert.Null(combo.Find("input").GetAttribute("aria-activedescendant"));
    }

    [Fact]
    public void Arrows_WithoutAnyMatch_KeepTheListClosed()
    {
        var combo = RenderCombo("zzz", []);

        combo.Find("input").KeyDown("ArrowDown");
        combo.Find("input").KeyDown("ArrowUp");

        Assert.Empty(Options(combo));
        Assert.Equal("false", combo.Find("input").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void PressOnASuggestion_PicksIt()
    {
        var changes = new List<string>();
        var picks = 0;
        var combo = RenderCombo("na", changes, () => picks++);
        combo.Find("input").Focus();

        combo.Find("li[role=option]").MouseDown();

        Assert.Equal(["Namur"], changes);
        Assert.Equal(1, picks);
    }
}
