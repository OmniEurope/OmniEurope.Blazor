using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Two instances of a component without an <c>Id</c> side by side never share an id, and every label
/// and every <c>aria-labelledby</c> of theirs points to an element that exists (audit of 2026-10-07,
/// RCL-004, RCL-006 and RCL-014).
/// </summary>
public sealed class InstanceIdTests : OmniBunitContext
{
    public sealed record Row(int Number, string Name);

    private static readonly Row[] Rows = [new(1, "Alpha"), new(2, "Beta")];

    private static RenderFragment Columns(bool searchable) => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "name");
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
        builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
        builder.CloseComponent();
        builder.OpenComponent<OmniDataGridColumn<Row>>(4);
        builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Key), "kind");
        builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.Title), "Genre");
        builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
        builder.AddComponentParameter(8, nameof(OmniDataGridColumn<Row>.FilterType), OmniDataGridColumnFilterType.MultiSelect);
        builder.AddComponentParameter(9, nameof(OmniDataGridColumn<Row>.FilterSearchable), searchable);
        builder.CloseComponent();
    };

    private static void Grid(RenderTreeBuilder builder, int sequence, bool headerMenu, bool searchable)
    {
        builder.OpenComponent<OmniDataGrid<Row>>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniDataGrid<Row>.Items), Rows);
        builder.AddComponentParameter(sequence + 2, nameof(OmniDataGrid<Row>.Filterable), true);
        builder.AddComponentParameter(sequence + 3, nameof(OmniDataGrid<Row>.ShowHeaderFilterMenu), headerMenu);
        builder.AddComponentParameter(sequence + 4, nameof(OmniDataGrid<Row>.PagerPosition), OmniDataGridPosition.TopAndBottom);
        builder.AddComponentParameter(sequence + 5, nameof(OmniDataGrid<Row>.PageSizeOptions), new[] { 10, 20 });
        builder.AddComponentParameter(sequence + 6, nameof(OmniDataGrid<Row>.Columns), Columns(searchable));
        builder.CloseComponent();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TwoGridsWithoutId_ShareNoId_AndEveryReferenceFindsItsElement(bool headerMenu, bool searchable)
    {
        var page = Render(builder =>
        {
            Grid(builder, 0, headerMenu, searchable);
            Grid(builder, 10, headerMenu, searchable);
        });

        AssertIdsUniqueAndReferencesResolved(page.Nodes.QuerySelectorAll("*"));
        Assert.NotEmpty(page.FindAll("[id*='-filter-name']"));
    }

    [Fact]
    public void MultiSelectOpenWithoutSearch_TakesTheIdItself_AndItsOwnName()
    {
        var select = Render<OmniDataGridFilterMultiSelect>(parameters => parameters
            .Add(component => component.Id, "genre")
            .Add(component => component.Suggestions, ["a"])
            .Add(component => component.Label, "Filtrer Genre")
            .Add(component => component.Presentation, OmniMultiSelectPresentation.List));

        var list = select.Find("[role=listbox]");
        Assert.Equal("genre", list.Id);
        Assert.Equal("Filtrer Genre", list.GetAttribute("aria-label"));
        Assert.Null(list.GetAttribute("aria-labelledby"));

        select.Render(parameters => parameters.Add(component => component.Label, null));
        Assert.Null(select.Find("[role=listbox]").GetAttribute("aria-label"));
        select.Render(parameters => parameters.Add(component => component.Placeholder, "Choisir"));
        Assert.Equal("Choisir", select.Find("[role=listbox]").GetAttribute("aria-label"));

        select.Render(parameters => parameters.Add(component => component.Filterable, true));
        list = select.Find("[role=listbox]");
        Assert.Null(list.GetAttribute("id"));
        Assert.Equal("genre", list.GetAttribute("aria-labelledby"));
        Assert.Equal("genre", select.Find(".omni-multi-select__search").Id);
    }

    [Fact]
    public void TwoSheetsAndTwoPagersWithoutId_ShareNoId()
    {
        var page = Render(builder =>
        {
            for (var index = 0; index < 2; index++)
            {
                builder.OpenComponent<OmniSpreadsheet>(0);
                builder.AddComponentParameter(1, nameof(OmniSpreadsheet.Value), OmniSpreadsheetData.FromRows([["A", "1"]]));
                builder.CloseComponent();
                builder.OpenComponent<OmniPager>(2);
                builder.AddComponentParameter(3, nameof(OmniPager.PageCount), 3);
                builder.AddComponentParameter(4, nameof(OmniPager.PageSize), 10);
                builder.AddComponentParameter(5, nameof(OmniPager.PageSizeOptions), new[] { 10, 20 });
                builder.CloseComponent();
            }
        });

        AssertIdsUniqueAndReferencesResolved(page.Nodes.QuerySelectorAll("*"));
        Assert.Equal(2, page.FindAll(".omni-spreadsheet__formula-input").Select(input => input.Id).Distinct().Count());
        Assert.Equal(2, page.FindAll(".omni-pager__page-size").Select(input => input.Id).Distinct().Count());
    }

    [Fact]
    public void AppMenu_PutsItsIdOnTheTrigger()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Id, "compte")
            .Add(component => component.AppearanceChanged, _ => { }));

        Assert.Equal("compte", menu.Find(".omni-app-menu__trigger").Id);
    }

    private static void AssertIdsUniqueAndReferencesResolved(IEnumerable<IElement> elements)
    {
        var all = elements.ToList();
        var ids = all.Where(element => !string.IsNullOrEmpty(element.Id)).Select(element => element.Id!).ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
        var byId = all.Where(element => !string.IsNullOrEmpty(element.Id)).ToDictionary(element => element.Id!, StringComparer.Ordinal);

        foreach (var label in all.Where(element => element.LocalName == "label" && element.HasAttribute("for")))
        {
            Assert.True(byId.TryGetValue(label.GetAttribute("for")!, out var target), $"label for={label.GetAttribute("for")} sans cible");
            Assert.Contains(target!.LocalName, new[] { "input", "select", "textarea", "button" });
        }

        foreach (var element in all.Where(element => element.HasAttribute("aria-labelledby")))
        {
            foreach (var reference in element.GetAttribute("aria-labelledby")!.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                Assert.True(byId.ContainsKey(reference), $"aria-labelledby={reference} sans cible");
            }
        }
    }
}
