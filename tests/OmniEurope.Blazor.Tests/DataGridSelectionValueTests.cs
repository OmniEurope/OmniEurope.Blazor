using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniDataGrid{TItem}.Value"/> as the selection when the host binds rows rather than
/// <see cref="OmniDataGrid{TItem}.SelectedKeys"/>: it preselects, and a change keeps the selected
/// rows of the other pages.
/// </summary>
public sealed class DataGridSelectionValueTests : OmniBunitContext
{
    [Fact]
    public void Value_Alone_PreselectsItsRows()
    {
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2, 3])
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Multiple)
            .Add(component => component.Value, [2])
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<int>>(this, _ => { })));

        var boxes = grid.FindAll("tbody input[type=checkbox]");
        Assert.Equal([false, true, false], boxes.Select(box => box.HasAttribute("checked")));
    }

    [Fact]
    public void ValueChanged_KeepsTheSelectedRowsOfOtherPages()
    {
        IReadOnlyList<int>? reported = null;
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2, 3, 4])
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Paged)
            .Add(component => component.PageSize, 2)
            .Add(component => component.Page, 2)
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Multiple)
            .Add(component => component.Value, [1])
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<IReadOnlyList<int>>(this, value => reported = value)));

        grid.FindAll("tbody input[type=checkbox]")[0].Change(true);

        Assert.Equal([1, 3], reported);
    }
}
