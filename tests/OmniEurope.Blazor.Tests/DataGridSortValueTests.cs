using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A column whose <c>SortValue</c> orders the rows by something other than the text it shows: the rank
/// text stays what the cells, the multiple-selection filter and the export read.
/// </summary>
public sealed class DataGridSortValueTests : OmniBunitContext
{
    private sealed record Card(string Name, string Rank);

    private static readonly string[] Tiers = ["S", "A", "B", "C"];

    // Alphabetical order of the ranks (A, B, C, S) differs from the tier order (S, A, B, C).
    private static readonly IReadOnlyList<Card> Cards = [new("one", "B"), new("two", "S"), new("three", "C"), new("four", "A")];

    public DataGridSortValueTests() =>
        JSInterop.SetupModule(OmniModules.DocumentEditor).SetupVoid("download", _ => true).SetVoidResult();

    private IRenderedComponent<OmniDataGrid<Card>> RenderGrid(bool withSortValue, Action<ComponentParameterCollectionBuilder<OmniDataGrid<Card>>>? more = null)
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Card>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Card>.Key), "name");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Card>.Title), "Nom");
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Card>.Property), nameof(Card.Name));
            builder.CloseComponent();

            builder.OpenComponent<OmniDataGridColumn<Card>>(4);
            builder.AddComponentParameter(5, nameof(OmniDataGridColumn<Card>.Key), "rank");
            builder.AddComponentParameter(6, nameof(OmniDataGridColumn<Card>.Title), "Rang");
            builder.AddComponentParameter(7, nameof(OmniDataGridColumn<Card>.Value), (Func<Card, object?>)(card => card.Rank));
            builder.AddComponentParameter(8, nameof(OmniDataGridColumn<Card>.Filterable), true);
            builder.AddComponentParameter(9, nameof(OmniDataGridColumn<Card>.FilterType), OmniDataGridColumnFilterType.MultiSelect);
            if (withSortValue)
            {
                builder.AddComponentParameter(10, nameof(OmniDataGridColumn<Card>.SortValue), (Func<Card, IComparable?>)(card => Array.IndexOf(Tiers, card.Rank)));
            }

            builder.CloseComponent();
        };

        return Render<OmniDataGrid<Card>>(parameters =>
        {
            parameters
                .Add(component => component.Items, Cards)
                .Add(component => component.KeyOf, card => card.Name)
                .Add(component => component.FilterPlacement, OmniDataGridFilterPlacement.HeaderMenu)
                .Add(component => component.Columns, columns);
            more?.Invoke(parameters);
        });
    }

    private static string[] Ranks(IRenderedComponent<OmniDataGrid<Card>> grid) =>
        [.. grid.FindAll("tbody tr td[data-omni-col='rank']").Select(cell => cell.TextContent.Trim())];

    private static void ClickRankSort(IRenderedComponent<OmniDataGrid<Card>> grid) =>
        grid.Find("th[data-omni-col='rank'] .omni-data-grid__sort").Click();

    [Fact]
    public void SortValue_OrdersByPositionAscendingThenDescending_AndCellsKeepTheText()
    {
        var grid = RenderGrid(withSortValue: true);

        ClickRankSort(grid);
        Assert.Equal(["S", "A", "B", "C"], Ranks(grid));

        ClickRankSort(grid);
        Assert.Equal(["C", "B", "A", "S"], Ranks(grid));
    }

    [Fact]
    public void WithoutSortValue_TheColumnStillSortsByItsValue()
    {
        var grid = RenderGrid(withSortValue: false);

        ClickRankSort(grid);
        Assert.Equal(["A", "B", "C", "S"], Ranks(grid));
    }

    [Fact]
    public void SortValue_LeavesTheMultiSelectOptionsReadingTheText()
    {
        var grid = RenderGrid(withSortValue: true);

        var labels = grid.FindAll("th[data-omni-col='rank'] .omni-multi-select__option").Select(option => option.TextContent.Trim()).ToArray();
        Assert.Equal(4, labels.Length);
        Assert.All(labels, label => Assert.Contains(label, new[] { "S", "A", "B", "C" }));
    }

    [Fact]
    public async Task SortValue_LeavesTheExportReadingTheText()
    {
        OmniTableExportDocument? exported = null;
        var grid = RenderGrid(withSortValue: true, parameters => parameters
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.ExportMarkdown, false)
            .Add(component => component.ExportExcel, false)
            .Add(component => component.ExportPdf, false)
            .Add(component => component.OnExport, document => exported = document));

        ClickRankSort(grid);
        await grid.Find(".omni-data-grid__export-button").ClickAsync(new());

        var rank = exported!.Columns.Select(column => column.Title).ToList().IndexOf("Rang");
        Assert.Equal(["S", "A", "B", "C"], exported.Rows.Select(row => row[rank].Text).ToArray());
    }
}
