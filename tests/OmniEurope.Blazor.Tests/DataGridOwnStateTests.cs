using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The grid keeps its own page and selection, mirrored from its parameters when the host passes a new
/// value, instead of writing its own parameters; and the filter editors take the host's attributes.
/// </summary>
public sealed class DataGridOwnStateTests : OmniBunitContext
{
    private static readonly int[] Numbers = [1, 2, 3, 4, 5];

    [Fact]
    public void Page_NotBound_StaysWhereTheReaderWent_AcrossAHostRender()
    {
        var pages = new List<int>();
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Numbers)
            .Add(component => component.PageSize, 2)
            .Add(component => component.PageChanged, EventCallback.Factory.Create<int>(this, pages.Add)));

        grid.Find(".omni-pager button[aria-label='Page suivante']").Click();
        Assert.Equal([2], pages);
        Assert.Equal(["3", "4"], grid.FindAll("tbody tr[data-omni-row-index]").Select(row => row.TextContent.Trim()));

        // The host renders again with the same Page it always passed: not a new value, so no jump back.
        grid.Render(parameters => parameters.Add(component => component.Caption, "Nombres"));
        Assert.Equal(["3", "4"], grid.FindAll("tbody tr[data-omni-row-index]").Select(row => row.TextContent.Trim()));
        Assert.Equal(1, grid.Instance.Page);

        // A new value from the host moves it.
        grid.Render(parameters => parameters.Add(component => component.Page, 3));
        Assert.Equal(["5"], grid.FindAll("tbody tr[data-omni-row-index]").Select(row => row.TextContent.Trim()));
    }

    [Fact]
    public void Selection_NotBound_IsKeptAcrossAHostRender_AndReported()
    {
        IReadOnlyList<int>? reported = null;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, Numbers)
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Multiple)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<int>>(this, value => reported = value)));

        grid.FindAll("tbody input[type=checkbox]")[1].Change(true);
        grid.Render(parameters => parameters.Add(component => component.Caption, "Nombres"));

        Assert.Equal([2], reported);
        Assert.Equal([false, true, false, false, false], grid.FindAll("tbody input[type=checkbox]").Select(box => box.HasAttribute("checked")));
        Assert.Empty(grid.Instance.Value);
    }

    [Fact]
    public void Selection_MatchesNewInstancesThroughKeyOf()
    {
        var grid = Render<OmniDataGrid<Item>>(parameters => parameters
            .Add(component => component.Items, [new Item(1, "a"), new Item(2, "b")])
            .Add(component => component.KeyOf, item => item.Id)
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Single)
            .Add(component => component.Value, [new Item(2, "b, reloaded")]));

        Assert.Equal([false, true], grid.FindAll("tbody input[type=checkbox]").Select(box => box.HasAttribute("checked")));
    }

    [Fact]
    public void FilterEditors_RenderTheHostAttributes_AndTheirId()
    {
        var combo = Render<OmniDataGridFilterCombo>(parameters => parameters
            .Add(component => component.Id, "combo")
            .AddUnmatched("data-probe", "combo"));
        Assert.Equal("combo", combo.Find(".omni-combo").GetAttribute("data-probe"));
        Assert.Equal("combo", combo.Find("input").Id);

        var list = Render<OmniDataGridFilterMultiSelect>(parameters => parameters
            .Add(component => component.Id, "list")
            .Add(component => component.Filterable, true)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.List)
            .AddUnmatched("data-probe", "list"));
        Assert.Equal("list", list.Find(".omni-multi-select").GetAttribute("data-probe"));
        Assert.Equal("list", list.Find("input.omni-multi-select__search").Id);

        var range = Render<OmniDataGridFilterDateRange>(parameters => parameters
            .Add(component => component.Id, "range")
            .AddUnmatched("data-probe", "range"));
        Assert.Equal("range", range.Find(".omni-data-grid__date-range").GetAttribute("data-probe"));
        Assert.Equal("range", range.Find("input.omni-data-grid__date-range-start").Id);
    }

    [Fact]
    public void FilterEditors_MarkEveryMatch_IgnoringCaseAndAccents()
    {
        var combo = Render<OmniDataGridFilterCombo>(parameters => parameters
            .Add(component => component.Id, "combo")
            .Add(component => component.Value, "li")
            .Add(component => component.Suggestions, ["Liège Lille"]));
        combo.Find("input").Focus();

        Assert.Equal(["Li", "Li"], combo.FindAll(".omni-combo__option mark").Select(mark => mark.TextContent));

        var list = Render<OmniDataGridFilterMultiSelect>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Presentation, OmniMultiSelectPresentation.List)
            .Add(component => component.Suggestions, ["Liège"]));
        list.Find("input.omni-multi-select__search").Input("liege");

        Assert.Equal("Liège", list.Find(".omni-multi-select__option mark").TextContent);
    }

    private sealed record Item(int Id, string Name);
}
