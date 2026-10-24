using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The request of a client application of 2026-10-12: a grid with FillAvailableHeight below settings
/// taller than the visible part of the area that scrolls it was measured 0px, its frame 2px, every row
/// cut. MinHeight is the floor of the whole grid and the frame is never squeezed under its content. The
/// layout itself (rows intersected with every clipping ancestor, at 390 and 1280 px, both scroll modes,
/// with and without a host rule of a flex basis of 0) is the showcase scripts probe's (requests, step 8).
/// </summary>
public sealed class GridFillFloorTests : OmniBunitContext
{
    public sealed record Row(int Number, string Name);

    [Fact]
    public void AFillingGridWithABar_PutsItsFrameRightUnderTheRoot_AndTheFillViewportInIt()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, [new Row(1, "Alpha"), new Row(2, "Beta")])
            .Add(component => component.FillAvailableHeight, true)
            .Add(component => component.MinHeight, "12rem")
            .Add(component => component.ShowHeaderBar, true)
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "name");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
                builder.CloseComponent();
            })));

        // The stylesheet's floor rules name these exact relations: the frame as a child of the filling
        // root, the filling viewport inside the frame, the header bar beside it.
        var frame = grid.Find(".omni-data-grid.omni-data-grid--fill > .omni-data-grid__frame");
        Assert.NotNull(frame.QuerySelector(":scope > .omni-data-grid__viewport.omni-data-grid__viewport--fill"));
        Assert.NotNull(frame.QuerySelector(":scope > .omni-data-grid__bar--header"));
    }

    [Fact]
    public void TheFrame_ClipsWithoutBecomingAScrollContainer()
    {
        var styles = StylesheetSource.Read();

        // overflow: hidden made the frame a scroll container, whose automatic minimum height is 0: a flex
        // or grid parent could then squeeze it to its 2px border over a viewport of 384px.
        Assert.Matches(@"\.omni-data-grid__frame \{[^}]*display: grid; min-inline-size: 0; overflow: clip; \}", styles);
        Assert.DoesNotMatch(@"\.omni-data-grid__frame \{[^}]*overflow: hidden;", styles);
    }

    [Fact]
    public void MinHeight_IsTheFloorOfTheWholeFillingGrid()
    {
        var styles = StylesheetSource.Read();

        // A measure of 0 (nothing left below the grid) leaves the grid at its content minimum, not at 0.
        Assert.Contains(".omni-data-grid--fill { block-size: var(--omni-grid-fill-height, 100%); display: flex; flex-direction: column; min-block-size: min-content; }", styles, StringComparison.Ordinal);
        // The frame of a filling grid hands the free height to its viewport...
        Assert.Contains(".omni-data-grid--fill > .omni-data-grid__frame { display: flex; flex: auto; flex-direction: column; }", styles, StringComparison.Ordinal);
        // ...which counts for MinHeight alone in that minimum: a flex basis of 0, never its 30rem
        // virtual height nor its whole page of rows.
        Assert.Contains(".omni-data-grid--fill .omni-data-grid__viewport--fill { flex: 1 1 0; min-block-size: var(--omni-grid-viewport-min, 75dvh); }", styles, StringComparison.Ordinal);
    }
}
