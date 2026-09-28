using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A text cell keeps to one line and ends in an ellipsis instead of spilling over the next column;
/// a templated cell stays unclipped so its badges, buttons and edit inputs are not cut.
/// </summary>
public sealed class DataGridCellClipTests : OmniBunitContext
{
    private const string TextCell = "omni-data-grid__cell--text";

    [Fact]
    public void TextCells_CarryTheClipClass_TemplatedCellsDoNot()
    {
        var text = Render<DataGridLoopColumnsTestHost>(parameters => parameters.Add(component => component.CaptureValue, true));
        var templated = Render<DataGridLoopColumnsTestHost>();

        Assert.All(text.FindAll("tbody td[data-omni-col]"), cell => Assert.Contains(TextCell, cell.ClassList));
        Assert.All(templated.FindAll("tbody td[data-omni-col]"), cell => Assert.DoesNotContain(TextCell, cell.ClassList));
    }

    [Fact]
    public void ARowInEdit_UnclipsTheCellsThatShowAnEditInput()
    {
        var grid = Render<DataGridSurfaceTestHost>(parameters => parameters
            .Add(component => component.EditMode, OmniDataGridEditMode.Single));

        grid.FindAll(".omni-data-grid__actions button")[0].Click();

        var editing = grid.Find(".surface-edit").Closest("td")!;
        Assert.DoesNotContain(TextCell, editing.ClassList);
        // The row's other column has no edit template: it still shows its text, clipped.
        Assert.Contains(TextCell, editing.ParentElement!.QuerySelector("td[data-omni-col='Total']")!.ClassList);
        // The header and the footer keep their own rules (HeaderWrap).
        Assert.Empty(grid.FindAll($"thead .{TextCell}, tfoot .{TextCell}"));
    }

    [Fact]
    public void Stylesheet_ClipsTextCellsWithAnEllipsis()
    {
        var body = ShippedLookTests.Body(".omni-data-grid tbody td.omni-data-grid__cell--text");

        Assert.Equal("hidden", ShippedLookTests.Value(body, "overflow"));
        Assert.Equal("ellipsis", ShippedLookTests.Value(body, "text-overflow"));
        Assert.Equal("nowrap", ShippedLookTests.Value(body, "white-space"));
    }

    [Fact]
    public void Stylesheet_ClipsNoTemplatedCellAndLetsResponsiveCardsWrap()
    {
        // Only the text-cell class clips: no rule hides the overflow of every body cell, which would
        // cut the focus rings, badges and popups of templated cells.
        Assert.DoesNotContain(
            ShippedLookTests.Rules(),
            rule => rule.Selector.StartsWith(".omni-data-grid", StringComparison.Ordinal)
                && rule.Selector.EndsWith("tbody td", StringComparison.Ordinal)
                && rule.Body.Contains("overflow", StringComparison.Ordinal));

        var card = ShippedLookTests.Body(".omni-data-grid--responsive .omni-data-grid__table td.omni-data-grid__cell--text");
        Assert.Equal("normal", ShippedLookTests.Value(card, "white-space"));
        Assert.Equal("anywhere", ShippedLookTests.Value(card, "overflow-wrap"));
    }
}
