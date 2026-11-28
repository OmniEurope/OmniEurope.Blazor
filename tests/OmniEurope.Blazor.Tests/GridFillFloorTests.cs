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
            .Add(component => component.Columns, DataGridRendering.KeyedColumn<Row>("name", row => row.Name)));

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
        Assert.Matches(@"\.omni-data-grid__frame \{[^}]*display: flex; flex-direction: column; min-inline-size: 0; overflow: clip; \}", styles);
        Assert.DoesNotMatch(@"\.omni-data-grid__frame \{[^}]*overflow: hidden;", styles);
    }

    [Fact]
    public void AStretchedFrame_GivesItsExtraHeightToTheViewportAlone()
    {
        var styles = StylesheetSource.Read();

        // A grid frame shared the extra height of a grid stretched in a flex card between its rows: the
        // header bar of the column chooser stood 182px high around its 24px button (Aetheus, 2026-11-28).
        // The bars keep their height; the viewport takes the rest and never shrinks below its rows. The
        // layout itself is measured in a real browser by eng/Test-ShowcaseGridBarProbe.mjs.
        Assert.Contains(".omni-data-grid__frame > .omni-data-grid__bar { flex: none; }", styles, StringComparison.Ordinal);
        Assert.Contains(".omni-data-grid__frame > .omni-data-grid__viewport { border: 0; border-radius: 0; box-shadow: none; flex: 1 0 auto; }", styles, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\.omni-data-grid__frame \{[^}]*display: grid;", styles);
    }

    [Fact]
    public void AStretchedRoot_GivesItsExtraHeightToTheTableAlone()
    {
        var styles = StylesheetSource.Read();

        // A grid root shared the extra height the same way: the group panel of a paged grid went from 19px
        // to 94px, the paging summary and the pager grew bands of their own (Aetheus, 2026-11-28).
        Assert.Matches(@"\.omni-data-grid \{[^}]*display: flex; flex-direction: column;[^}]*gap: var\(--omni-space-md\); min-inline-size: 0; \}", styles);
        Assert.Contains(".omni-data-grid > * { flex: none; }", styles, StringComparison.Ordinal);
        Assert.Contains(".omni-data-grid > :is(.omni-data-grid__frame, .omni-data-grid__viewport) { flex: 1 0 auto; }", styles, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"\.omni-data-grid \{[^}]*display: grid;", styles);
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
        // virtual height nor its whole page of rows. MinHeight is a true floor, never capped by the room
        // left under the blocks above the grid (owner rule of 2026-11-08): the page scrolls past it.
        Assert.Contains(".omni-data-grid--fill .omni-data-grid__viewport--fill { box-sizing: border-box; flex: 1 1 0; min-block-size: var(--omni-grid-viewport-min, 60dvh); }", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("--omni-grid-viewport-room", styles, StringComparison.Ordinal);
    }
}
