using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The grid's internal helpers read on their own: aggregates over every number type and over nothing,
/// valueless operators, every filter operator and comparison, two-condition filters, sorting values of
/// mixed types, property paths through fields, structs and nullable values, the virtual window's bounds,
/// and the remote block cache when a load is replaced, cancelled or fails.
/// </summary>
public sealed class GridInternalsTests
{
    public sealed record Row(string Name, object? Value)
    {
        public int Count;
        public DateTime When { get; init; } = new(2026, 1, 2);
        public DateTime? Maybe { get; init; }
        public Row? Parent { get; init; }
    }

    private static OmniDataGridColumnDefinition<Row> Column(string key = "v", Func<Row, object?>? value = null) => new()
    {
        Key = key,
        Title = key,
        Value = value ?? (row => row.Value),
    };

    // ---- aggregates -------------------------------------------------------------------------------

    [Fact]
    public void Aggregates_AddEveryNumberType_AndAnswerNothingWhenNothingFits()
    {
        object?[] values = [(byte)1, (sbyte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL, 1m, "texte", null];
        var rows = values.Select(value => new Row("r", value)).ToList();

        Assert.Equal(9m, GridAggregates<Row>.Compute(rows, row => row.Value, OmniDataGridAggregate.Sum));
        Assert.Equal(11, GridAggregates<Row>.Compute(rows, row => row.Value, OmniDataGridAggregate.Count));
        Assert.Equal(1.5d, GridAggregates<Row>.Compute([new Row("a", 1f), new Row("b", 2d)], row => row.Value, OmniDataGridAggregate.Average));
        Assert.Null(GridAggregates<Row>.Compute([new Row("a", "x")], row => row.Value, OmniDataGridAggregate.Average));
        Assert.Null(GridAggregates<Row>.Compute([], row => row.Value, OmniDataGridAggregate.Min));
        Assert.Null(GridAggregates<Row>.Compute([new Row("a", new object())], row => row.Value, OmniDataGridAggregate.Max));
        Assert.Null(GridAggregates<Row>.Compute([new Row("a", 1)], row => row.Value, OmniDataGridAggregate.None));
    }

    // ---- filters ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(OmniDataGridFilterOperator.IsNull, true)]
    [InlineData(OmniDataGridFilterOperator.IsNotNull, true)]
    [InlineData(OmniDataGridFilterOperator.IsEmpty, true)]
    [InlineData(OmniDataGridFilterOperator.IsNotEmpty, true)]
    [InlineData(OmniDataGridFilterOperator.Contains, false)]
    public void ValuelessOperators_NeedNoValue(OmniDataGridFilterOperator filterOperator, bool valueless)
    {
        var filter = GridColumnFilter.Empty with { Operator = filterOperator, SecondOperator = filterOperator };

        Assert.Equal(valueless, GridColumnFilter.IsValueless(filterOperator));
        Assert.Equal(valueless, filter.HasFirst);
        Assert.Equal(valueless, filter.HasSecond);
    }

    [Theory]
    [InlineData("10", "9", OmniDataGridFilterOperator.GreaterThanOrEquals, true)]
    [InlineData("10", "9", OmniDataGridFilterOperator.LessThan, false)]
    [InlineData("9", "9", OmniDataGridFilterOperator.LessThanOrEquals, true)]
    [InlineData("2026-01-02", "2026-01-01", OmniDataGridFilterOperator.GreaterThan, true)]
    [InlineData("b", "a", OmniDataGridFilterOperator.GreaterThan, true)]
    [InlineData(null, "", OmniDataGridFilterOperator.IsNull, true)]
    [InlineData("x", "", OmniDataGridFilterOperator.IsNotNull, true)]
    public void MatchesFilter_ComparesNumbersDatesAndTexts(string? candidate, string filter, OmniDataGridFilterOperator filterOperator, bool expected) =>
        Assert.Equal(expected, GridProjection<Row>.MatchesFilter(candidate, filter, filterOperator, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Projection_TwoConditions_OrAndAnd_WithAPredicate_AndSortsMixedValues()
    {
        var rows = new List<Row> { new("a", 1), new("b", null), new("c", "x"), new("d", 2) };
        var column = Column();
        var or = GridColumnFilter.Empty with
        {
            Operator = OmniDataGridFilterOperator.Equals,
            Value = "1",
            LogicalOperator = OmniDataGridLogicalOperator.Or,
            SecondOperator = OmniDataGridFilterOperator.Equals,
            SecondValue = "2"
        };
        var filters = new Dictionary<string, GridColumnFilter> { ["v"] = or };

        var result = GridProjection<Row>.Create(rows, [column], filters, [], false, false, 1, 10);
        Assert.Equal(["a", "d"], result.Items.Select(row => row.Name));

        var and = or with { LogicalOperator = OmniDataGridLogicalOperator.And };
        Assert.Empty(GridProjection<Row>.Create(rows, [column], new Dictionary<string, GridColumnFilter> { ["v"] = and }, [], false, false, 1, 10).Items);

        var predicated = Column(value: row => row.Value);
        predicated.FilterPredicate = (row, text) => row.Name == text;
        var secondOnly = GridColumnFilter.Empty with { Value = "", LogicalOperator = OmniDataGridLogicalOperator.Or, SecondValue = "c" };
        Assert.Equal(["c"], GridProjection<Row>.Create(rows, [predicated], new Dictionary<string, GridColumnFilter> { ["v"] = secondOnly }, [], false, false, 1, 10).Items.Select(row => row.Name));

        var sorted = GridProjection<Row>.Create(rows, [column], new Dictionary<string, GridColumnFilter>(), [new OmniDataGridSort("v", false)], false, false, 1, 10);
        Assert.Equal("b", sorted.Items[0].Name);

        // Empty values sort first, ascending, and last, descending, wherever they start.
        List<Row> holes = [new("x", 3), new("n1", null), new("y", 1), new("n2", null), new("z", 2)];
        string[] Sorted(bool descending) => [.. GridProjection<Row>.Create(holes, [column], new Dictionary<string, GridColumnFilter>(), [new OmniDataGridSort("v", descending)], false, false, 1, 10).Items.Select(row => row.Name)];
        Assert.Equal(["n1", "n2", "y", "z", "x"], Sorted(false));
        Assert.Equal(["x", "z", "y", "n1", "n2"], Sorted(true));
    }

    [Theory]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(TimeOnly))]
    public void FilterOperators_OfADayOrATime_AreThoseOfAnOrderedValue(Type type)
    {
        var column = new OmniDataGridColumnDefinition<Row> { Key = "t", Title = "t", Value = row => row.Value, ValueType = type };

        Assert.Contains(OmniDataGridFilterOperator.LessThan, GridFilterOperators<Row>.OperatorsFor(column));
        Assert.DoesNotContain(OmniDataGridFilterOperator.Contains, GridFilterOperators<Row>.OperatorsFor(column));
    }

    [Fact]
    public void Projection_FirstConditionAnsweredByThePredicate()
    {
        var rows = new List<Row> { new("a", 1), new("b", 2) };
        var column = Column(value: row => row.Value);
        column.FilterPredicate = (row, text) => row.Name == text;
        var filter = GridColumnFilter.Empty with { Value = "b" };

        var result = GridProjection<Row>.Create(rows, [column], new Dictionary<string, GridColumnFilter> { ["v"] = filter }, [], false, false, 1, 10);

        Assert.Equal(["b"], result.Items.Select(row => row.Name));
    }

    [Fact]
    public void FilterOperators_OfAnOrderedValueType_AndAnUnknownName()
    {
        var dated = new OmniDataGridColumnDefinition<Row> { Key = "w", Title = "w", Value = row => row.When, ValueType = typeof(TimeSpan) };

        Assert.Contains(OmniDataGridFilterOperator.GreaterThan, GridFilterOperators<Row>.OperatorsFor(dated));
        Assert.Equal(OmniDataGridFilterOperator.Contains, GridFilterOperators<Row>.Parse("inconnu", OmniDataGridFilterOperator.Contains));
        dated.FilterOperators = [OmniDataGridFilterOperator.Contains];
        Assert.Contains(OmniDataGridFilterOperator.GreaterThan, GridFilterOperators<Row>.OperatorsFor(dated));
    }

    // ---- property paths ---------------------------------------------------------------------------

    [Fact]
    public void PropertyPaths_ThroughFieldsStructsAndNullableValues()
    {
        var row = new Row("a", null) { Count = 3, Maybe = new DateTime(2026, 5, 6), Parent = new Row("p", null) };

        Assert.Equal(3, GridPropertyAccessor.Create<Row>("Count")!(row));
        Assert.Equal(2026, GridPropertyAccessor.Create<Row>("When.Year")!(row));
        Assert.Equal(6, GridPropertyAccessor.Create<Row>("Maybe.Value.Day")!(row));
        Assert.Equal(typeof(int), GridPropertyAccessor.ValueType<Row>("Count"));
        Assert.Null(GridPropertyAccessor.ValueType<Row>("Parent.Unknown"));
        Assert.Null(GridPropertyAccessor.Create<Row>("Parent.Unknown"));
    }

    // ---- virtual window ---------------------------------------------------------------------------

    [Fact]
    public void VirtualWindow_RefusesMeasuresOutsideIt_AndAnEstimateThatIsNotPositive()
    {
        var window = new GridVirtualWindow();
        window.Configure(3, 0d);

        Assert.Equal(3, window.Count);
        Assert.Equal(1d, window.EstimatedRowHeight);
        Assert.False(window.IsMeasured(-1));
        Assert.False(window.Measure(-1, 10));
        Assert.False(window.Measure(3, 10));
        Assert.False(window.Measure(0, double.NaN));
        Assert.False(window.Measure(0, 0));
        Assert.True(window.Measure(0, 10));
    }

    [Fact]
    public void VirtualWindow_KeepsItsMeasuresAcrossANewEstimate_AndReadsAScrollThatIsNotANumberAsTheTop()
    {
        var window = new GridVirtualWindow();
        Assert.Equal(0, window.IndexAt(50));
        window.Configure(100, 20d);

        Assert.False(window.Measure(0, 20d));
        Assert.True(window.Measure(1, 60d));
        Assert.False(window.Measure(1, 60.2d));
        // Row 2 measures what the next estimate will be: once rebuilt, it differs from it by nothing.
        Assert.True(window.Measure(2, 30d));
        window.Configure(100, 30d);
        Assert.Equal(60d, window.HeightOf(1));
        Assert.Equal(30d, window.HeightOf(2));
        Assert.Equal(0, window.IndexAt(-5));

        var range = window.Compute(double.NaN, 0d, 2);
        Assert.Equal(0, range.StartIndex);
        Assert.True(range.Count > 0);
    }

    // ---- remote block cache -----------------------------------------------------------------------

    private static Task<OmniDataGridResult<Row>> Page(int start, int size) =>
        Task.FromResult(new OmniDataGridResult<Row>([.. Enumerable.Range(start, size).Select(index => new Row($"r{index}", index))], 100));

    [Fact]
    public async Task BlockCache_ResetDuringALoad_KeepsNothingOfIt()
    {
        var source = new GridVirtualDataSource<Row>();
        var pending = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var loading = source.EnsureRangeAsync(0, 10, 10, (_, _, _) => pending.Task);

        source.Reset();
        pending.SetResult(new OmniDataGridResult<Row>([new Row("x", 1)], 1));

        Assert.False(await loading);
        Assert.Equal(0, source.CachedItemCount);
    }

    [Fact]
    public async Task BlockCache_ResetByItsOwnLoader_KeepsNothingOfTheAnswer()
    {
        // A loader that resets the cache (a reload it triggers) and answers at once: its answer belongs
        // to a query that no longer exists.
        var source = new GridVirtualDataSource<Row>();

        Assert.False(await source.EnsureRangeAsync(0, 10, 10, (start, size, _) =>
        {
            source.Reset();
            return Page(start, size);
        }));
        Assert.Equal(0, source.CachedItemCount);
    }

    [Fact]
    public async Task BlockRefresh_ReplacedOrCancelledOrFailing_KeepsTheCacheItHad()
    {
        var source = new GridVirtualDataSource<Row>();
        Assert.True(await source.EnsureRangeAsync(0, 10, 10, (start, size, _) => Page(start, size)));

        var slow = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var replaced = source.RefreshAsync(0, 10, 10, (_, _, _) => slow.Task);
        Assert.True(await source.RefreshAsync(0, 10, 10, (start, size, _) => Page(start, size)));
        slow.SetResult(new OmniDataGridResult<Row>([], 0));
        Assert.False(await replaced);

        var failing = new TaskCompletionSource<OmniDataGridResult<Row>>();
        var late = source.RefreshAsync(0, 10, 10, (_, _, _) => failing.Task);
        Assert.True(await source.RefreshAsync(0, 10, 10, (start, size, _) => Page(start, size)));
        failing.SetException(new InvalidOperationException("tard"));
        Assert.False(await late);

        Assert.True(await source.RefreshAsync(0, 10, 10, (_, _, _) => Task.FromException<OmniDataGridResult<Row>>(new InvalidOperationException("panne"))));
        Assert.NotNull(source.Error);

        var hanging = source.RefreshAsync(0, 10, 10, (_, _, token) => Task.Delay(Timeout.Infinite, token).ContinueWith(_ => new OmniDataGridResult<Row>([], 0), TaskScheduler.Default));
        source.Reset();
        Assert.False(await hanging);
        Assert.Equal(0, source.CachedItemCount);
    }
}
