using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A column without a width sizes itself to its content, unless it asks for <c>Truncate</c>. The
/// measuring runs in omni-grid.js and is proven in a browser (eng/Test-ShowcaseGridAutoFitProbe.mjs);
/// these tests pin what .NET hands the script: which columns the markup marks as content-sized, which
/// as truncated or sized, that the script is asked once and detached at disposal.
/// </summary>
public sealed class DataGridContentFitTests : OmniBunitContext
{
    public sealed record Row(int Id, string Name, string Description);

    private static readonly Row[] Rows = [new(1, "@example-org/web-analytics", "Long description")];

    [Fact]
    public void Truncate_IsOffByDefault()
    {
        var grid = RenderGrid(width: null, truncate: false);

        Assert.False(grid.FindComponent<OmniDataGridColumn<Row>>().Instance.Truncate);
    }

    [Fact]
    public void AColumnWithNeitherAWidthNorTruncate_IsMarkedForNeitherAttribute()
    {
        var grid = RenderGrid(width: null, truncate: false);

        var col = grid.Find("col[data-omni-col='name']");
        // The script reads exactly these two attributes: both absent means the column sizes to its content.
        Assert.False(col.HasAttribute("data-omni-width"));
        Assert.False(col.HasAttribute("data-omni-truncate"));
    }

    [Fact]
    public void ADeclaredWidth_IsInTheMarkup_AndLeavesTheColumnOutOfTheContentFit()
    {
        var grid = RenderGrid(width: "10rem", truncate: false);

        var col = grid.Find("col[data-omni-col='name']");
        Assert.Equal("10rem", col.GetAttribute("data-omni-width"));
        Assert.False(col.HasAttribute("data-omni-truncate"));
    }

    [Fact]
    public void ATruncateColumn_IsMarkedSo_WhateverItsWidth()
    {
        var grid = RenderGrid(width: null, truncate: true);

        Assert.Equal("true", grid.Find("col[data-omni-col='name']").GetAttribute("data-omni-truncate"));
        // The other column of the grid is not affected.
        Assert.False(grid.Find("col[data-omni-col='description']").HasAttribute("data-omni-truncate"));
    }

    [Fact]
    public void TruncateChangedAfterTheFirstRender_UpdatesTheMarkup()
    {
        var grid = RenderGrid(width: null, truncate: false);
        Assert.False(grid.Find("col[data-omni-col='name']").HasAttribute("data-omni-truncate"));

        grid.Render(parameters => parameters.Add(host => host.Truncate, true));

        Assert.Equal("true", grid.Find("col[data-omni-col='name']").GetAttribute("data-omni-truncate"));

        grid.Render(parameters => parameters.Add(host => host.Truncate, false));

        Assert.False(grid.Find("col[data-omni-col='name']").HasAttribute("data-omni-truncate"));
    }

    [Fact]
    public async Task AWidthChosenByTheReader_TakesTheColumnOutOfTheContentFit()
    {
        var grid = RenderGrid(width: null, truncate: false, allowResize: true);
        Assert.False(grid.Find("col[data-omni-col='name']").HasAttribute("data-omni-width"));

        await grid.InvokeAsync(() => grid.Instance.OnColumnResizedAsync("name", 120d));

        Assert.Equal("120px", grid.Find("col[data-omni-col='name']").GetAttribute("data-omni-width"));
    }

    [Fact]
    public async Task TheScript_IsAskedOnce_AndDetachedAtDisposal()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        var grid = RenderGrid(width: null, truncate: false);

        var attach = Assert.Single(module.Invocations["attachContentFit"]);
        Assert.NotNull(attach.Arguments[0]);

        // Later renders (a page, a sort, a filter) are followed by the script itself: no new call.
        grid.Render(parameters => parameters.Add(host => host.Truncate, true));
        grid.Render();
        Assert.Single(module.Invocations["attachContentFit"]);

        await grid.Instance.DisposeGridAsync();

        Assert.Single(module.Invocations["detachContentFit"]);
        Assert.Equal("uninstallPackageTooltips", module.Invocations.Select(call => call.Identifier).Last());
    }

    [Fact]
    public void AGridThatSizesNothing_StillAsksNoLayoutOfTheScript()
    {
        // The content fit is its own call: a plain grid is still not given a height or a column layout.
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;

        RenderGrid(width: null, truncate: false);

        Assert.Empty(module.Invocations["applyLayout"]);
    }

    private IRenderedComponent<Host> RenderGrid(string? width, bool truncate, bool allowResize = false)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        return Render<Host>(parameters => parameters
            .Add(host => host.Width, width)
            .Add(host => host.Truncate, truncate)
            .Add(host => host.AllowResize, allowResize));
    }

    public sealed class Host : ComponentBase
    {
        private OmniDataGrid<Row>? _grid;

        [Parameter] public string? Width { get; set; }
        [Parameter] public bool Truncate { get; set; }
        [Parameter] public bool AllowResize { get; set; }

        public Task OnColumnResizedAsync(string key, double width) => _grid!.OnColumnResizedAsync(key, width);

        public Task DisposeGridAsync() => _grid!.DisposeAsync().AsTask();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<OmniDataGrid<Row>>(0);
            builder.AddComponentParameter(1, nameof(OmniDataGrid<Row>.Items), Rows);
            builder.AddComponentParameter(2, nameof(OmniDataGrid<Row>.AllowColumnResize), AllowResize);
            builder.AddComponentParameter(3, nameof(OmniDataGrid<Row>.Columns), (RenderFragment)(columns =>
            {
                columns.OpenComponent<OmniDataGridColumn<Row>>(0);
                columns.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "name");
                columns.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Nom");
                columns.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Name));
                columns.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.Width), Width);
                columns.AddComponentParameter(5, nameof(OmniDataGridColumn<Row>.Truncate), Truncate);
                columns.CloseComponent();
                columns.OpenComponent<OmniDataGridColumn<Row>>(10);
                columns.AddComponentParameter(11, nameof(OmniDataGridColumn<Row>.Key), "description");
                columns.AddComponentParameter(12, nameof(OmniDataGridColumn<Row>.Title), "Description");
                columns.AddComponentParameter(13, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.Description));
                columns.CloseComponent();
            }));
            builder.AddComponentReferenceCapture(10, component => _grid = (OmniDataGrid<Row>)component);
            builder.CloseComponent();
        }
    }
}
