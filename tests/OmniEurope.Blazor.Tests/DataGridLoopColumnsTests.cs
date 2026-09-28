using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Columns declared in a <c>@foreach</c> get new delegates on every render because each one
/// captures the loop variable. An equivalent delegate must not re-register the column, or the
/// grid re-renders its columns, which re-register, without end.
/// </summary>
public sealed class DataGridLoopColumnsTests : OmniBunitContext
{
    // Two columns by two rows: one grid render calls the column templates four times.
    private const int CellsPerRender = 4;

    [Fact]
    public void LoopTemplate_DoesNotReRenderTheGridWithoutEnd()
    {
        var host = Render<DataGridLoopColumnsTestHost>();

        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        Assert.Equal(["Alice", "Bruxelles", "Bob", "Namur"], host.FindAll(".loop-cell").Select(cell => cell.TextContent));
        var afterFirstRender = host.Instance.CellRenders;
        Assert.InRange(afterFirstRender, CellsPerRender, CellsPerRender * 3);

        host.InvokeAsync(host.Instance.Rerender);

        // One parent render re-renders the grid at most twice (its own render, then the one the adopted
        // delegates queue in the same batch), not in a loop.
        Assert.InRange(host.Instance.CellRenders - afterFirstRender, 0, CellsPerRender * 2);
    }

    [Fact]
    public void LoopValue_DoesNotReRenderTheGridWithoutEnd()
    {
        var host = Render<DataGridLoopColumnsTestHost>(parameters => parameters.Add(component => component.CaptureValue, true));

        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        Assert.Contains("Bruxelles", host.Find("tbody").TextContent, StringComparison.Ordinal);
        var afterFirstRender = host.Instance.CellRenders;
        Assert.True(afterFirstRender < DataGridLoopColumnsTestHost.RenderBound, $"{afterFirstRender} value reads after the first render.");

        host.InvokeAsync(host.Instance.Rerender);

        // At most two grid renders: the parent's, and the one the adopted delegates queue in the same batch.
        Assert.True(
            host.Instance.CellRenders - afterFirstRender <= afterFirstRender * 2,
            $"A parent render read {host.Instance.CellRenders - afterFirstRender} values after a first render that read {afterFirstRender}.");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void LoopFilterLists_OfTheSameContent_DoNotReRenderTheGridWithoutEnd(bool literalValues, bool literalOperators)
    {
        var host = Render<DataGridLoopColumnsTestHost>(parameters => parameters
            .Add(component => component.LiteralFilterValues, literalValues)
            .Add(component => component.LiteralFilterOperators, literalOperators));

        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        var afterFirstRender = host.Instance.CellRenders;
        Assert.InRange(afterFirstRender, CellsPerRender, CellsPerRender * 3);

        host.InvokeAsync(host.Instance.Rerender);

        Assert.InRange(host.Instance.CellRenders - afterFirstRender, 0, CellsPerRender * 2);
    }

    [Fact]
    public void LoopTemplate_ShowsCapturedStateOfTheParentRenderInThatRender()
    {
        var host = Render<DataGridLoopColumnsTestHost>();
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        var afterFirstRender = host.Instance.CellRenders;

        // Same key, title, property and width: the column keeps its registration, only the state its
        // template captures changes.
        host.InvokeAsync(() => host.Instance.ChangeColumn(0, host.Instance.Columns[0] with { Suffix = " (modifié)" }));

        // No wait: the cells show the new state once the parent render is done, not one render later.
        Assert.Equal(["Alice (modifié)", "Bruxelles", "Bob (modifié)", "Namur"], host.FindAll(".loop-cell").Select(cell => cell.TextContent));
        Assert.InRange(host.Instance.CellRenders - afterFirstRender, 0, CellsPerRender * 2);

        // The render the adoption queued does not queue another.
        var afterChange = host.Instance.CellRenders;
        host.InvokeAsync(host.Instance.Rerender);
        Assert.InRange(host.Instance.CellRenders - afterChange, 0, CellsPerRender * 2);
    }

    [Fact]
    public void LoopColumn_StillUpdatesWhenItsTitlePropertyOrWidthChange()
    {
        var host = Render<DataGridLoopColumnsTestHost>();
        host.WaitForAssertion(() => Assert.Equal(2, host.FindAll("tbody tr").Count));
        var grid = host.FindComponent<OmniDataGrid<DataGridLoopColumnsTestHost.Row>>();
        Assert.DoesNotContain("240px", grid.Instance.TableMinimumWidth(), StringComparison.Ordinal);

        host.InvokeAsync(() => host.Instance.ChangeColumn(1, new("city", "Localité", nameof(DataGridLoopColumnsTestHost.Row.Name), "240px")));

        host.WaitForAssertion(() =>
        {
            Assert.Contains("Localité", host.Find("thead").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("Ville", host.Find("thead").TextContent, StringComparison.Ordinal);
            Assert.Equal(["Alice", "Alice", "Bob", "Bob"], host.FindAll(".loop-cell").Select(cell => cell.TextContent));
            Assert.Contains("240px", grid.Instance.TableMinimumWidth(), StringComparison.Ordinal);
        });
    }
}
