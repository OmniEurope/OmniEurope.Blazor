using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Covers the defects found while reworking the grid for the Atlas recette: columns collapsing on a
/// narrow screen, clicks and keys of the control cells leaking to the row, the header checkbox and
/// expand button acting on the rows on screen only, a page left past the end, and the filter
/// editors that stretched the header.
/// </summary>
public sealed class DataGridRowsTests : OmniBunitContext
{
    private const string GridModule = "./_content/OmniEurope.Blazor/omni-grid.js";

    [Fact]
    public void SelectAll_TicksTheSelectableRowsOnScreenThenClearsThem()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.RowRender, args => args.Selectable = args.Item.Id != 1));

        var selectAll = host.Find(".omni-data-grid__select-all");
        Assert.False(selectAll.HasAttribute("checked"));

        selectAll.Change(true);

        // Row 1 is vetoed by RowRender and row 3 is on the next page: only row 2 is taken.
        Assert.Equal([2], host.Instance.SelectedKeys);
        Assert.True(host.Find(".omni-data-grid__select-all").HasAttribute("checked"));

        host.Find(".omni-data-grid__select-all").Change(false);

        Assert.Empty(host.Instance.SelectedKeys);
    }

    [Fact]
    public void SelectAll_LeavesTheSelectionOfOtherPagesAlone()
    {
        var host = Render<DataGridRowsTestHost>();
        host.Find(".omni-pager button[aria-label=\"Page suivante\"]").Click();
        host.Find("tbody input.omni-checkbox").Change(true);
        Assert.Equal([3], host.Instance.SelectedKeys);

        host.Find(".omni-pager button[aria-label=\"Page précédente\"]").Click();
        host.Find(".omni-data-grid__select-all").Change(true);
        host.Find(".omni-data-grid__select-all").Change(false);

        Assert.Equal([3], host.Instance.SelectedKeys);
    }

    [Fact]
    public void SelectAll_IsOnlyOfferedForMultipleSelection()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.SelectionMode, OmniDataGridSelectionMode.Single));

        Assert.Empty(host.FindAll(".omni-data-grid__select-all"));
    }

    [Fact]
    public void ControlCells_KeepTheirClicksFromTheRow()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.SelectOnRowClick, true));

        // The tick itself selects; the click that carries it must not reach the row and toggle the
        // selection straight back, which is what happened with a row that selects on click.
        // bUnit bubbles a click the way the browser does and refuses one that no handler hears:
        // the checkbox's own click reaching nothing is the proof the row never sees it.
        var checkbox = host.Find("tbody td[data-omni-control=\"select\"] input");
        Assert.Throws<MissingEventHandlerException>(() => checkbox.Click());
        checkbox.Change(true);
        host.Find("tbody td[data-omni-control=\"expand\"] button").Click();
        host.FindAll("tbody td.omni-data-grid__actions button")[0].Click();

        Assert.Equal(0, host.Instance.RowClicks);
        Assert.Equal([1], host.Instance.SelectedKeys);

        // A click on a data cell still is a row click, and selects the row.
        host.FindAll("tbody tr[data-omni-row-index]")[1].QuerySelector("td[data-omni-col=\"city\"]")!.Click();
        Assert.Equal(1, host.Instance.RowClicks);
        Assert.Equal([1, 2], host.Instance.SelectedKeys);
    }

    [Fact]
    public void RowBeingEdited_KeepsItsKeysAndClicksInTheEditor()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.SelectOnRowClick, true));

        host.FindAll("tbody td.omni-data-grid__actions button")[0].Click();
        var editor = host.Find(".rows-edit");

        // A space typed in the editor, or a click placing the caret, is not a request for the row.
        Assert.Throws<MissingEventHandlerException>(() => editor.KeyDown(" "));
        Assert.Throws<MissingEventHandlerException>(() => editor.Click());

        Assert.Equal(0, host.Instance.RowClicks);
        Assert.Empty(host.Instance.SelectedKeys);
    }

    [Fact]
    public void EditColumn_UsesLabelledIconButtonsInsideARealTableCell()
    {
        var host = Render<DataGridRowsTestHost>();

        var edit = host.Find("tbody td.omni-data-grid__actions .omni-data-grid__actions-group button");
        Assert.Equal("Modifier", edit.GetAttribute("aria-label"));
        Assert.Equal("Modifier", edit.GetAttribute("title"));
        Assert.NotNull(edit.QuerySelector("svg.omni-icon"));

        edit.Click();

        var buttons = host.FindAll("tbody tr[data-omni-row-index]")[0].QuerySelectorAll(".omni-data-grid__actions button");
        Assert.Equal(["Enregistrer", "Annuler"], buttons.Select(button => button.GetAttribute("aria-label")));
    }

    [Fact]
    public void ExpandAll_OpensTheRowsOnScreenAndClosesThemAgain()
    {
        var host = Render<DataGridRowsTestHost>();
        host.Find(".omni-pager button[aria-label=\"Page suivante\"]").Click();
        host.Find("tbody td[data-omni-control=\"expand\"] button").Click();
        Assert.Equal([3], host.Instance.ExpandedKeys);
        host.Find(".omni-pager button[aria-label=\"Page précédente\"]").Click();

        // Row 3, open on another page, used to turn the button into a no-op on this one.
        host.Find("thead .omni-data-grid__expand").Click();
        Assert.Equal([1, 2, 3], host.Instance.ExpandedKeys.Order());
        Assert.Equal(2, host.FindAll(".omni-data-grid__detail").Count);

        host.Find("thead .omni-data-grid__expand").Click();
        Assert.Equal([3], host.Instance.ExpandedKeys);
    }

    [Fact]
    public void ExpandAll_IsNotOfferedWhenOnlyOneRowMayBeOpen()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.ExpandMode, OmniDataGridExpandMode.Single));

        Assert.Empty(host.FindAll("thead .omni-data-grid__expand"));
    }

    [Fact]
    public void PagePastTheEnd_IsBroughtBackToTheLastPageAndReported()
    {
        var host = Render<DataGridRowsTestHost>();
        host.Find(".omni-pager button[aria-label=\"Dernière page\"]").Click();
        Assert.Equal(3, host.Instance.Page);

        host.Render(parameters => parameters.Add(component => component.Items, host.Instance.Items.Take(3).ToArray()));

        Assert.Equal(2, host.Instance.Page);
        Assert.Contains("Page 2 sur 2", host.Find(".omni-pager").TextContent, StringComparison.Ordinal);
        Assert.Contains("Chloe", host.Find("tbody").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TableMinimumWidth_AddsDeclaredWidthsFloorsAndControlColumns()
    {
        var module = JSInterop.SetupModule(GridModule);
        var host = Render<DataGridRowsTestHost>();

        var minimum = host.FindComponent<OmniDataGrid<DataGridRowsTestHost.Row>>().Instance.TableMinimumWidth();

        // Name declares 10rem, City nothing (the auto floor), Score only a minimum; then the expand,
        // selection and edit columns.
        Assert.Equal(
            "calc(10rem + var(--omni-data-grid-column-min-width) + 6rem + var(--omni-data-grid-control-width) + var(--omni-data-grid-control-width) + var(--omni-data-grid-edit-width))",
            minimum);
        var applied = Assert.Single(module.Invocations["applyColumns"]);
        Assert.Equal(minimum, applied.Arguments[2]);
    }

    [Fact]
    public void FrozenColumn_FreezesTheControlColumnsBeforeItAndIsReanchoredAfterEachRender()
    {
        var module = JSInterop.SetupModule(GridModule);
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.FreezeName, true));

        Assert.All(
            host.FindAll("[data-omni-control]"),
            cell => Assert.Contains("omni-data-grid__column--frozen", cell.ClassName, StringComparison.Ordinal));
        Assert.Equal("omni-data-grid__header-row", host.Find("thead tr").ClassName);

        var before = module.Invocations["applyFrozen"].Count;
        host.Find(".omni-pager button[aria-label=\"Page suivante\"]").Click();

        Assert.True(module.Invocations["applyFrozen"].Count > before, "The new page's cells were not re-anchored.");
    }

    [Fact]
    public void ControlColumns_AreNotFrozenWhileNoColumnIs()
    {
        var host = Render<DataGridRowsTestHost>();

        Assert.All(
            host.FindAll("[data-omni-control]"),
            cell => Assert.DoesNotContain("omni-data-grid__column--frozen", cell.ClassName, StringComparison.Ordinal));
    }

    [Fact]
    public void MultiSelectFilter_FoldsIntoOneLineInTheFilterRowAndOpensInTheMenu()
    {
        var inline = Render<DataGridRowsTestHost>();

        var folded = inline.Find("td[data-omni-col=\"city\"] details.omni-data-grid__multi");
        Assert.Equal("Filtrer", folded.QuerySelector(".omni-data-grid__multi-text")!.TextContent);
        folded.QuerySelectorAll(".omni-multi-select__checkbox")[1].Change(true);

        // Suggestions are ordered: Liege, Mons, Namur.
        Assert.Equal("Mons", inline.Find("td[data-omni-col=\"city\"] .omni-data-grid__multi-text").TextContent);
        Assert.Single(inline.FindAll("tbody tr[data-omni-row-index]"));

        var menu = Render<DataGridRowsTestHost>(parameters => parameters.Add(component => component.ShowHeaderFilterMenu, true));
        Assert.Empty(menu.FindAll(".omni-data-grid__filters"));
        Assert.Empty(menu.FindAll("details.omni-data-grid__multi"));
        Assert.Equal(3, menu.FindAll("th[data-omni-col=\"city\"] .omni-data-grid__popover-panel .omni-multi-select__checkbox").Count);
    }

    [Fact]
    public void FilterReset_OnlyShowsWhileTheColumnIsFilteredAndClearsIt()
    {
        var host = Render<DataGridRowsTestHost>();
        Assert.Empty(host.FindAll(".omni-data-grid__filter-reset"));
        // The inline row no longer repeats a text "clear" button under every filter.
        Assert.Empty(host.FindAll(".omni-data-grid__filters .omni-data-grid__filter-clear"));

        host.Find("td[data-omni-col=\"name\"] .omni-data-grid__filter").Input("bo");
        Assert.Single(host.FindAll("tbody tr[data-omni-row-index]"));

        var reset = host.Find("td[data-omni-col=\"name\"] .omni-data-grid__filter-reset");
        Assert.Equal("Effacer", reset.GetAttribute("aria-label"));
        reset.Click();

        Assert.Empty(host.FindAll(".omni-data-grid__filter-reset"));
        Assert.Equal(2, host.FindAll("tbody tr[data-omni-row-index]").Count);
    }

    [Fact]
    public void AdvancedFilter_EditsInAPopoverAndSummarisesTheAppliedCondition()
    {
        var host = Render<DataGridRowsTestHost>(parameters => parameters
            .Add(component => component.FilterMode, OmniDataGridFilterMode.Advanced));

        var cell = host.Find("td[data-omni-col=\"name\"]");
        Assert.NotNull(cell.QuerySelector("details[data-omni-popover] .omni-data-grid__popover-panel .omni-data-grid__filter-apply"));
        Assert.Equal("Filtrer", cell.QuerySelector(".omni-data-grid__advanced-summary")!.TextContent);

        host.Find("td[data-omni-col=\"name\"] .omni-data-grid__filter").Input("e");
        // Nothing applies before the apply action, and nothing offers to be cleared either.
        Assert.Equal(2, host.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Empty(host.FindAll("td[data-omni-col=\"name\"] .omni-data-grid__filter-reset"));

        host.Find("td[data-omni-col=\"name\"] .omni-data-grid__filter-apply").Click();

        Assert.Equal("Contient e", host.Find("td[data-omni-col=\"name\"] .omni-data-grid__advanced-summary").TextContent);
        Assert.Equal(["Alice", "Chloe"], host.FindAll("tbody tr[data-omni-row-index] td[data-omni-col=\"name\"]").Select(td => td.TextContent));
        Assert.NotNull(host.Find("td[data-omni-col=\"name\"] .omni-data-grid__filter-reset"));
    }

    [Fact]
    public void Popovers_AreWiredOnlyWhenAFilterOpensSomething()
    {
        var module = JSInterop.SetupModule(GridModule);
        Render<DataGridRowsTestHost>();
        Assert.Single(module.Invocations["attachFilterMenus"]);

        var plain = JSInterop.SetupModule(GridModule);
        Render<OmniDataGrid<int>>(parameters => parameters.Add(component => component.Items, new[] { 1, 2 }));
        Assert.Empty(plain.Invocations["attachFilterMenus"]);
    }

    [Fact]
    public void FilterTemplate_DeclaredInAChildComponentStillRefiltersTheGrid()
    {
        var host = Render<DataGridNestedFilterTestHost>();
        Assert.Equal(3, host.FindAll("tbody tr[data-omni-row-index]").Count);

        // Suggestions are ordered: Liege, Namur. The callback runs in the columns component, whose
        // own re-render never reached the grid: the tick was taken and nothing was filtered.
        host.FindAll("td[data-omni-col=\"city\"] .omni-multi-select__checkbox")[0].Change(true);

        var row = Assert.Single(host.FindAll("tbody tr[data-omni-row-index]"));
        Assert.Contains("Bob", row.TextContent, StringComparison.Ordinal);
        Assert.Equal("Liege", host.Find("td[data-omni-col=\"city\"] .omni-data-grid__multi-text").TextContent);
    }
}
