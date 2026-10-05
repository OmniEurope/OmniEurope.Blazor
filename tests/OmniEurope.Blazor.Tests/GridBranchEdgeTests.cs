using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDataGrid in the remaining shapes its suites leave out: footer and pager drawn top and bottom, an
/// edit column without editors, a virtual grid loading without its bar, a state store the host
/// registered, an ascending initial sort, a column that registers twice, a filter emptied, two sorts the
/// second descending, a date column's operators, aggregates written without a format, a fixed row
/// height without an estimate, and a fill that keeps its minimum height.
/// </summary>
public sealed class GridBranchEdgeTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, DateTimeOffset When);

    private static readonly IReadOnlyList<Row> Rows =
    [
        new("a", 3, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        new("b", 1, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)),
        new("c", 2, new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero))
    ];

    private static RenderFragment Columns(Action<int, RenderTreeBuilderExtra>? extra = null) => builder =>
    {
        foreach (var (property, index) in new[] { (nameof(Row.Name), 0), (nameof(Row.Amount), 1) })
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(index * 10);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniDataGridColumn<Row>.Property), property);
            builder.AddComponentParameter(index * 10 + 2, nameof(OmniDataGridColumn<Row>.Title), property);
            extra?.Invoke(index, new RenderTreeBuilderExtra(builder, index * 10 + 3));
            builder.CloseComponent();
        }
    };

    /// <summary>Adds parameters to the column being built, from a free sequence number.</summary>
    public sealed class RenderTreeBuilderExtra(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, int sequence)
    {
        private int _next = sequence;

        public void Add(string name, object? value) => builder.AddComponentParameter(_next++, name, value);
    }

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(Action<ComponentParameterCollectionBuilder<OmniDataGrid<Row>>>? extra = null, RenderFragment? columns = null) =>
        Render<OmniDataGrid<Row>>(parameters =>
        {
            parameters
                .Add(component => component.Items, Rows)
                .Add(component => component.KeyOf, row => row.Name)
                .Add(component => component.Columns, columns ?? Columns());
            extra?.Invoke(parameters);
        });

    [Fact]
    public void FooterAndPager_TopAndBottom_AreDrawnTwice()
    {
        var grid = RenderGrid(
            parameters => parameters
                .Add(component => component.FooterPosition, OmniDataGridPosition.TopAndBottom)
                .Add(component => component.PagerPosition, OmniDataGridPosition.TopAndBottom)
                .Add(component => component.PageSize, 2),
            Columns((index, column) =>
            {
                if (index == 1)
                {
                    column.Add(nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Sum);
                }
            }));

        Assert.Equal(2, grid.FindAll(".omni-pager").Count);
        Assert.Equal(2, grid.FindAll("tr.omni-data-grid__footer-row").Count);
        // Written without a format, the sum is the plain number.
        Assert.All(grid.FindAll("tr.omni-data-grid__footer-row td[data-omni-col='Amount']"), cell => Assert.Equal("6", cell.TextContent.Trim()));
    }

    [Fact]
    public void InitialSort_OnAColumnSortedByAnotherProperty_SendsThatProperty()
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<Row>(Rows, Rows.Count));
            })
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.Columns, Columns((index, column) =>
            {
                if (index == 0)
                {
                    column.Add(nameof(OmniDataGridColumn<Row>.SortOrder), OmniDataGridSortOrder.Descending);
                    column.Add(nameof(OmniDataGridColumn<Row>.SortProperty), nameof(Row.When));
                }
            })));

        grid.WaitForAssertion(() => Assert.NotEmpty(requests));
        var sort = Assert.Single(requests[0].Sorts);
        Assert.Equal(nameof(Row.When), sort.Property);
        Assert.True(sort.Descending);
    }

    [Fact]
    public void EditColumnSwitchedOff_IsNotDrawnEvenWithAnEditor()
    {
        RenderFragment<Row> editor = row => builder => builder.AddContent(0, row.Name);
        var grid = RenderGrid(
            parameters => parameters.Add(component => component.ShowEditColumn, false),
            Columns((index, column) =>
            {
                if (index == 0)
                {
                    column.Add(nameof(OmniDataGridColumn<Row>.EditTemplate), editor);
                }
            }));

        Assert.Empty(grid.FindAll("[data-omni-control='edit']"));
    }

    [Fact]
    public void Aggregate_WithoutItsOwnFormat_UsesTheColumnFormat()
    {
        var grid = RenderGrid(columns: Columns((index, column) =>
        {
            if (index == 1)
            {
                column.Add(nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Sum);
                column.Add(nameof(OmniDataGridColumn<Row>.FormatString), "[{0}]");
            }
        }));

        Assert.Equal("[6]", grid.Find("tr.omni-data-grid__footer-row td[data-omni-col='Amount']").TextContent.Trim());
    }

    /// <summary>A value whose text is null, as a type of the application may write it.</summary>
    public sealed record Mute(int Rank) : IComparable
    {
        public int CompareTo(object? other) => other is Mute mute ? Rank.CompareTo(mute.Rank) : 1;

        public override string? ToString() => null;
    }

    [Fact]
    public void Aggregate_OfAValueWithoutText_LeavesItsCellEmpty()
    {
        RenderFragment columns = builder =>
        {
            builder.OpenComponent<OmniDataGridColumn<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Title), "Rang");
            builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Key), "rank");
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => new Mute(row.Amount)));
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Max);
            builder.CloseComponent();
        };

        var grid = RenderGrid(columns: columns);

        Assert.Equal(string.Empty, grid.Find("tr.omni-data-grid__footer-row td[data-omni-col='rank']").TextContent.Trim());
    }

    [Fact]
    public void AutoFitTexts_OfAColumnTheGridDoesNotShow_AreNone()
    {
        var grid = RenderGrid();

        Assert.Null(grid.Instance.GetColumnAutoFitTexts("absente"));
    }

    [Fact]
    public void GroupOfAKeyWithoutColumn_IsNamedByItsKeyInThePanel()
    {
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.AllowGrouping, true)
            .Add(component => component.ShowGroupPanel, true)
            .Add(component => component.Groups, [new OmniDataGridGroup("absente"), new OmniDataGridGroup("Name")]));

        var chips = grid.FindAll(".omni-data-grid__group-chip").Select(chip => chip.TextContent).ToArray();
        Assert.Contains(chips, text => text.Contains("absente", StringComparison.Ordinal));
        Assert.Contains(chips, text => text.Contains("Name", StringComparison.Ordinal));
    }

    [Fact]
    public void FillAvailableHeight_WithABlankMinimum_SendsNoMinimum()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;

        RenderGrid(parameters => parameters
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.MinHeight, " "));

        var layout = Assert.Single(module.Invocations["applyLayout"]);
        Assert.Null(layout.Arguments[4]);
    }

    [Fact]
    public void RemotePagedGrid_LoadsAgainOnANewPageAndANewPageSize()
    {
        var requests = new List<OmniDataGridLoadRequest>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<Row>(Rows.Take(request.PageSize).ToArray(), 30));
            })
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.PageSize, 2)
            .Add(component => component.PageSizeOptions, [2, 5])
            .Add(component => component.Columns, Columns()));
        grid.WaitForAssertion(() => Assert.Single(requests));

        grid.Find(".omni-pager button[aria-label=\"Page suivante\"]").Click();
        grid.WaitForAssertion(() => Assert.Equal(2, requests[^1].Page));
        grid.Find(".omni-pager__page-size").Change("5");

        grid.WaitForAssertion(() => Assert.Equal(5, requests[^1].PageSize));
        Assert.Equal(1, requests[^1].Page);
    }

    [Fact]
    public async Task VirtualGridWhoseItemsShrink_DrawsOnlyTheRowsLeft()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var many = Enumerable.Range(0, 200).Select(index => new Row($"r{index}", index, default)).ToArray();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, many)
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.Columns, Columns())
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d));
        await grid.InvokeAsync(() => grid.Instance.OnViewportChangedAsync(7_000d, 400d));

        grid.Render(parameters => parameters.Add(component => component.Items, Rows));

        Assert.Equal(["a", "b", "c"], grid.FindAll("tbody tr[data-omni-row-index] td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim()));
    }

    [Fact]
    public void EditColumnAsked_WithoutAnyEditor_IsNotDrawn()
    {
        var grid = RenderGrid(parameters => parameters.Add(component => component.ShowEditColumn, true));

        Assert.Empty(grid.FindAll("[data-omni-control='edit']"));
    }

    [Fact]
    public void VirtualRemoteGridWithoutItsBar_ShowsTheLoadingRowUntilItsFirstRows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, _ => pending.Task)
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.ShowLoadingBar, false)
            .Add(component => component.Columns, Columns((index, column) =>
            {
                if (index == 1)
                {
                    column.Add(nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Sum);
                }
            })));

        grid.WaitForAssertion(() => Assert.NotEmpty(grid.FindAll(".omni-data-grid__state--loading")));
        pending.SetResult(new OmniDataGridResult<Row>(Rows, Rows.Count));

        grid.WaitForAssertion(() => Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count));
        Assert.Equal("6", grid.Find("tr.omni-data-grid__footer-row td[data-omni-col='Amount']").TextContent.Trim());
    }

    [Fact]
    public void RemotePagedGrid_SumsItsPage()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Load, _ => Task.FromResult(new OmniDataGridResult<Row>(Rows, 30)))
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.Columns, Columns((index, column) =>
            {
                if (index == 1)
                {
                    column.Add(nameof(OmniDataGridColumn<Row>.Aggregate), OmniDataGridAggregate.Sum);
                }
            })));

        grid.WaitForAssertion(() => Assert.Equal("6", grid.Find("tr.omni-data-grid__footer-row td[data-omni-col='Amount']").TextContent.Trim()));
    }

    private sealed class RegisteredStore : IOmniDataGridStateStore
    {
        public List<string> Keys { get; } = [];

        public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
        {
            Keys.Add(key);
            return Task.FromResult<string?>(null);
        }

        public Task SaveAsync(string key, string state, CancellationToken cancellationToken = default)
        {
            Keys.Add(key);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void StateStoreRegisteredByTheHost_IsUsedWithoutBeingPassed()
    {
        var store = new RegisteredStore();
        Services.AddSingleton<IOmniDataGridStateStore>(store);

        var grid = RenderGrid(parameters => parameters.Add(component => component.StateKey, "commandes"));
        grid.FindAll(".omni-data-grid__sort")[0].Click();

        Assert.Equal(["commandes", "commandes"], store.Keys);
    }

    [Fact]
    public void StateWithTheDefaultStore_GoesToTheBrowserStorageEachTime()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var grid = RenderGrid(parameters => parameters.Add(component => component.StateKey, "commandes"));
        grid.FindAll(".omni-data-grid__sort")[0].Click();
        grid.FindAll(".omni-data-grid__sort")[0].Click();

        Assert.Equal("descending", grid.FindAll("thead th")[0].GetAttribute("aria-sort"));
        Assert.Single(JSInterop.Invocations["localStorage.getItem"]);
        Assert.Equal(2, JSInterop.Invocations["localStorage.setItem"].Count);
    }

    [Fact]
    public void InitialSortAscending_OrdersTheRows()
    {
        var grid = RenderGrid(columns: Columns((index, column) =>
        {
            if (index == 1)
            {
                column.Add(nameof(OmniDataGridColumn<Row>.SortOrder), OmniDataGridSortOrder.Ascending);
            }
        }));

        Assert.Equal(["b", "c", "a"], grid.FindAll("tbody td[data-omni-col='Name']").Select(cell => cell.TextContent.Trim()));
    }

    [Fact]
    public void ColumnWithADefaultFilter_RegisteredAgain_KeepsTheFilterTheReaderCleared()
    {
        var host = Render<DataGridInitialLoadTestHost>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                var open = request.Filters.Any(filter => filter.Key == "Status");
                return Task.FromResult(new OmniDataGridResult<DataGridInitialLoadTestHost.Row>(
                    open ? [new(1, "Alpha", "Open")] : [new(1, "Alpha", "Open"), new(2, "Bravo", "Closed")], open ? 1 : 2));
            }));
        host.WaitForAssertion(() => Assert.Single(host.FindAll("tbody tr[data-omni-row-index]")));

        host.Find("th[data-omni-col=\"Status\"] .omni-data-grid__filter-reset").Click();
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr[data-omni-row-index]").Count));
        host.Render(parameters => parameters.Add(component => component.ShowStatus, false));
        host.Render(parameters => parameters.Add(component => component.ShowStatus, true));

        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr[data-omni-row-index]").Count));
    }

    [Fact]
    public void FilterEmptiedByTheReader_LeavesTheColumnInactive()
    {
        var grid = RenderGrid(parameters => parameters.Add(component => component.Filterable, true));
        var input = grid.Find("td[data-omni-col='Name'] .omni-data-grid__filter, th[data-omni-col='Name'] .omni-data-grid__filter");

        input.Input("a");
        grid.Find("td[data-omni-col='Name'] .omni-data-grid__filter, th[data-omni-col='Name'] .omni-data-grid__filter").Input(string.Empty);

        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.DoesNotContain("omni-data-grid__column--active", grid.Find("th[data-omni-col='Name']").ClassList);
    }

    [Fact]
    public void Projection_SecondSortDescending_BreaksTheTies()
    {
        var name = new OmniDataGridColumnDefinition<Row> { Key = "n", Title = "n", Value = row => row.Name };
        var parity = new OmniDataGridColumnDefinition<Row> { Key = "p", Title = "p", Value = row => row.Amount % 2 };
        var result = GridProjection<Row>.Create(Rows, [name, parity], new Dictionary<string, GridColumnFilter>(),
            [new OmniDataGridSort("p", false), new OmniDataGridSort("n", true)], false, false, 1, 10);

        Assert.Equal(["c", "b", "a"], result.Items.Select(row => row.Name));

        var ascending = GridProjection<Row>.Create(Rows, [name, parity], new Dictionary<string, GridColumnFilter>(),
            [new OmniDataGridSort("p", false), new OmniDataGridSort("n", false)], false, false, 1, 10);
        Assert.Equal(["c", "a", "b"], ascending.Items.Select(row => row.Name));
    }

    [Fact]
    public void Operators_OfADateTimeOffsetColumn_AreThoseOfAnOrderedValue()
    {
        var when = new OmniDataGridColumnDefinition<Row> { Key = "w", Title = "w", Value = row => row.When, ValueType = typeof(DateTimeOffset) };
        var day = new OmniDataGridColumnDefinition<Row> { Key = "d", Title = "d", Value = row => row.When.Date, ValueType = typeof(DateTime) };

        Assert.Contains(OmniDataGridFilterOperator.GreaterThan, GridFilterOperators<Row>.OperatorsFor(when));
        Assert.Contains(OmniDataGridFilterOperator.LessThan, GridFilterOperators<Row>.OperatorsFor(day));
    }

    [Fact]
    public void FixedRowHeight_WithoutAnEstimate_MeasuresTheRows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var grid = RenderGrid(parameters => parameters
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.FixedRowHeight, true)
            .Add(component => component.EstimatedRowHeight, 0d));

        Assert.Equal(3, grid.FindAll("tbody tr[data-omni-row-index]").Count);
    }

    [Fact]
    public void FillAvailableHeight_KeepsItsMinimumHeight()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;

        RenderGrid(parameters => parameters
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.MinHeight, "20rem"));

        Assert.Contains(module.Invocations, call => call.Arguments.Any(argument => Equals(argument, "20rem")));
    }
}
