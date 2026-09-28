using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The fit to content of a column (Excel's double click on its edge). The measurement itself runs in
/// omni-grid.js and is proven in a browser; these tests pin what .NET decides: where the gesture is
/// offered, which texts it measures, and what happens to the width it reports.
/// </summary>
public sealed class DataGridColumnAutoFitTests : OmniBunitContext
{
    public static TheoryData<bool, bool?, bool> Combinations => new()
    {
        { false, null, false },
        { true, null, true },
        { false, true, true },
        { true, false, false },
    };

    [Theory]
    [MemberData(nameof(Combinations))]
    public void AutoFit_FollowsTheGridUnlessTheColumnDecides(bool grid, bool? column, bool expected)
    {
        var rendered = RenderGrid(grid, column, allowResize: true);

        var handle = rendered.Find("th[data-omni-col='name'] .omni-data-grid__resize-handle");
        Assert.Equal(expected, handle.HasAttribute("data-omni-autofit"));
        Assert.True(handle.HasAttribute("data-omni-drag"));
        Assert.Equal(expected ? "Enter" : null, handle.GetAttribute("aria-keyshortcuts"));
    }

    [Fact]
    public void AutoFit_IsOffByDefault()
    {
        var rendered = RenderGrid(grid: null, column: null, allowResize: true);

        Assert.False(rendered.Instance.AllowColumnAutoFit);
        Assert.False(rendered.Find(".omni-data-grid__resize-handle").HasAttribute("data-omni-autofit"));
        Assert.Null(rendered.Instance.GetColumnAutoFitTexts("name"));
    }

    [Fact]
    public void AutoFit_KeepsTheEdgeHandleWithoutTheDrag()
    {
        var rendered = RenderGrid(grid: true, column: null, allowResize: false);

        var handle = rendered.Find(".omni-data-grid__resize-handle");
        Assert.True(handle.HasAttribute("data-omni-autofit"));
        Assert.False(handle.HasAttribute("data-omni-drag"));

        // Without the drag, the arrow keys no longer resize: only Enter, the fit, is offered.
        handle.KeyDown("ArrowRight");
        Assert.Empty(rendered.Instance.WidthChanges);
    }

    [Fact]
    public void AutoFit_WithoutEitherGestureRendersNoHandle()
    {
        var rendered = RenderGrid(grid: false, column: null, allowResize: false);

        Assert.Empty(rendered.FindAll(".omni-data-grid__resize-handle"));
    }

    [Fact]
    public void AutoFit_EnterAsksTheScriptToFitThatColumn()
    {
        var fit = JSInterop.SetupModule("./_content/OmniEurope.Blazor/omni-grid.js")
            .SetupVoid("autoFitColumn", _ => true);
        fit.SetVoidResult();
        var rendered = RenderGrid(grid: true, column: null, allowResize: true);

        rendered.Find(".omni-data-grid__resize-handle").KeyDown("Enter");

        var invocation = Assert.Single(fit.Invocations);
        Assert.Equal("name", invocation.Arguments[1]);
    }

    [Fact]
    public void AutoFit_OffersTheTextOfEveryLoadedRowIncludingTheVirtualizedOnesOffScreen()
    {
        var rows = Enumerable.Range(0, 5_000).Select(index => new Row(index, $"Ligne {index}")).ToList();
        rows[4_321] = new Row(4_321, "Une valeur très longue, bien au-delà de la fenêtre affichée");
        var rendered = RenderGrid(grid: true, column: null, allowResize: true, rows, virtualized: true);

        Assert.DoesNotContain("Une valeur très longue", rendered.Markup, StringComparison.Ordinal);

        var texts = rendered.Instance.GetColumnAutoFitTexts("name");

        Assert.NotNull(texts);
        Assert.Equal(5_000, texts.Length);
        Assert.Contains("Une valeur très longue, bien au-delà de la fenêtre affichée", texts);
    }

    [Fact]
    public void AutoFit_UsesTheColumnFormatStringAndSkipsDuplicates()
    {
        Row[] rows = [new(1, "a"), new(2, "a"), new(3, "b")];
        var rendered = RenderGrid(grid: true, column: null, allowResize: true, rows, formatString: "<{0}>");

        Assert.Equal(["<a>", "<b>"], rendered.Instance.GetColumnAutoFitTexts("name") ?? []);
    }

    [Fact]
    public void AutoFit_LeavesATemplatedColumnToItsRenderedRows()
    {
        var rendered = RenderGrid(grid: true, column: null, allowResize: true, template: row => builder => builder.AddContent(0, row.Name));

        Assert.Null(rendered.Instance.GetColumnAutoFitTexts("name"));
    }

    [Fact]
    public async Task AutoFit_AppliesPersistsAndAnnouncesTheWidthOnce()
    {
        var store = new RecordingStateStore();
        var rendered = RenderGrid(grid: true, column: null, allowResize: false, store: store);

        await rendered.InvokeAsync(() => rendered.Instance.OnColumnAutoFitAsync("name", 237d));

        var change = Assert.Single(rendered.Instance.WidthChanges);
        Assert.Equal("name", change.Key);
        Assert.Equal("237px", change.Width);
        Assert.Contains("\"name\":\"237px\"", Assert.Single(store.Saved), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AutoFit_NeverGoesBelowTheGridFloor()
    {
        var rendered = RenderGrid(grid: true, column: null, allowResize: true);

        await rendered.InvokeAsync(() => rendered.Instance.OnColumnAutoFitAsync("name", 12d));

        Assert.Equal("48px", Assert.Single(rendered.Instance.WidthChanges).Width);
    }

    [Fact]
    public async Task AutoFit_IgnoresAReportForAColumnThatDoesNotAllowIt()
    {
        var rendered = RenderGrid(grid: true, column: false, allowResize: true);

        await rendered.InvokeAsync(() => rendered.Instance.OnColumnAutoFitAsync("name", 237d));

        Assert.Empty(rendered.Instance.WidthChanges);
        Assert.Null(rendered.Instance.GetColumnAutoFitTexts("name"));
    }

    private IRenderedComponent<Host> RenderGrid(
        bool? grid,
        bool? column,
        bool allowResize,
        IReadOnlyList<Row>? rows = null,
        bool virtualized = false,
        string? formatString = null,
        RenderFragment<Row>? template = null,
        IOmniDataGridStateStore? store = null) => Render<Host>(parameters => parameters
            .Add(host => host.GridAutoFit, grid)
            .Add(host => host.ColumnAutoFit, column)
            .Add(host => host.AllowResize, allowResize)
            .Add(host => host.Rows, rows ?? [new Row(1, "Alice"), new Row(2, "Bob")])
            .Add(host => host.Virtualized, virtualized)
            .Add(host => host.FormatString, formatString)
            .Add(host => host.Template, template)
            .Add(host => host.Store, store));

    public sealed record Row(int Id, string Name);

    public sealed class Host : ComponentBase
    {
        private OmniDataGrid<Row>? _grid;

        [Parameter] public bool? GridAutoFit { get; set; }
        [Parameter] public bool? ColumnAutoFit { get; set; }
        [Parameter] public bool AllowResize { get; set; }
        [Parameter] public IReadOnlyList<Row> Rows { get; set; } = [];
        [Parameter] public bool Virtualized { get; set; }
        [Parameter] public string? FormatString { get; set; }
        [Parameter] public RenderFragment<Row>? Template { get; set; }
        [Parameter] public IOmniDataGridStateStore? Store { get; set; }

        public List<OmniDataGridColumnWidthChange> WidthChanges { get; } = [];

        public bool AllowColumnAutoFit => _grid!.AllowColumnAutoFit;

        public string[]? GetColumnAutoFitTexts(string key) => _grid!.GetColumnAutoFitTexts(key);

        public Task OnColumnAutoFitAsync(string key, double width) => _grid!.OnColumnAutoFitAsync(key, width);

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<OmniDataGrid<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGrid<Row>.Items), Rows);
            builder.AddComponentParameter(2, nameof(OmniDataGrid<Row>.AllowColumnResize), AllowResize);
            if (GridAutoFit is { } gridAutoFit)
            {
                builder.AddComponentParameter(3, nameof(OmniDataGrid<Row>.AllowColumnAutoFit), gridAutoFit);
            }

            builder.AddComponentParameter(4, nameof(OmniDataGrid<Row>.ScrollMode), Virtualized ? OmniDataGridScrollMode.Virtual : OmniDataGridScrollMode.Paged);
            builder.AddComponentParameter(5, nameof(OmniDataGrid<Row>.EstimatedRowHeight), 40d);
            builder.AddComponentParameter(6, nameof(OmniDataGrid<Row>.ColumnWidthChanged),
                EventCallback.Factory.Create<OmniDataGridColumnWidthChange>(this, WidthChanges.Add));
            if (Store is not null)
            {
                builder.AddComponentParameter(7, nameof(OmniDataGrid<Row>.StateKey), "autofit");
                builder.AddComponentParameter(8, nameof(OmniDataGrid<Row>.StateStore), Store);
            }

            builder.AddComponentParameter(9, nameof(OmniDataGrid<Row>.Columns), (RenderFragment)(columns =>
            {
                columns.OpenComponent<OmniDataGridColumn<Row>>(0);
                columns.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "name");
                columns.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
                columns.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
                columns.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.AutoFit), ColumnAutoFit);
                columns.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.FormatString), FormatString);
                columns.AddComponentParameter(6, nameof(OmniDataGridColumn<Row>.Template), Template);
                columns.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(10, component => _grid = (OmniDataGrid<Row>)component);
            builder.CloseComponent();
        }
    }

    private sealed class RecordingStateStore : IOmniDataGridStateStore
    {
        public List<string> Saved { get; } = [];

        public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task SaveAsync(string key, string state, CancellationToken cancellationToken = default)
        {
            Saved.Add(state);
            return Task.CompletedTask;
        }
    }
}
