using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The freeze toggle of the column chooser (<see cref="OmniDataGrid{TItem}.AllowColumnFreeze"/>): a named
/// toggle button per column, by mouse and keyboard, the frozen columns kept contiguous at the start, the
/// change announced and saved, and the layout reset that brings the declared frozen columns back.
/// </summary>
public sealed class DataGridColumnFreezeTests : OmniBunitContext
{
    public sealed record Row(string Name, int Amount, string City);

    private static readonly IReadOnlyList<Row> Rows = [new("a", 1, "Lyon"), new("b", 2, "Gand")];

    private static readonly RenderFragment Columns = DataGridTestSetup.PropertyColumns<Row>(
        ("Name", [(nameof(OmniDataGridColumn<Row>.Frozen), true)]),
        ("Amount", []),
        ("City", []));

    private IRenderedComponent<OmniDataGrid<Row>> RenderGrid(bool freeze = true, bool reset = false) =>
        Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.KeyOf, row => row.Name)
            .Add(component => component.StateKey, "orders")
            .Add(component => component.AllowColumnFreeze, freeze)
            .Add(component => component.ShowResetLayout, reset)
            .Add(component => component.Columns, Columns));

    private static void Open(IRenderedComponent<OmniDataGrid<Row>> grid) => grid.Find(".omni-data-grid__chooser-toggle").Click();

    private static IElement Toggle(IRenderedComponent<OmniDataGrid<Row>> grid, string key) =>
        grid.Find($".omni-data-grid__chooser-freeze[data-omni-col='{key}']");

    private static string Announcement(IRenderedComponent<OmniDataGrid<Row>> grid) =>
        grid.Find(".omni-data-grid > [role=status][aria-live=polite]").TextContent.TrimEnd(' ');

    private static T InFrench<T>(Func<T> act)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
        try
        {
            return act();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void WithoutTheOption_NoToggle_AndNoChooser()
    {
        _ = new GridPreferenceHarness(Services);

        var grid = RenderGrid(freeze: false);

        Assert.Empty(grid.FindAll(".omni-data-grid__chooser"));
        Assert.Empty(grid.FindAll(".omni-data-grid__chooser-freeze"));
    }

    [Fact]
    public void EachColumn_HasANamedToggleButton_ThatTellsWhetherItIsFrozen()
    {
        _ = new GridPreferenceHarness(Services);

        var (name, pressed, city, type, label) = InFrench(() =>
        {
            var grid = RenderGrid();
            Open(grid);
            var toggle = Toggle(grid, "City");
            return (Toggle(grid, "Name").GetAttribute("aria-label"), Toggle(grid, "Name").GetAttribute("aria-pressed"),
                toggle.GetAttribute("aria-pressed"), toggle.GetAttribute("type"), toggle.GetAttribute("title"));
        });

        Assert.Equal("Figer la colonne Name", name);
        Assert.Equal("true", pressed);
        Assert.Equal("false", city);
        Assert.Equal("button", type);
        Assert.Equal("Figer la colonne City", label);
    }

    [Fact]
    public void AClick_FreezesAColumn_AtTheEndOfTheFrozenOnes_AnnouncesAndSavesIt()
    {
        var preferences = new GridPreferenceHarness(Services);

        var announced = InFrench(() =>
        {
            var grid = RenderGrid();
            Open(grid);
            Toggle(grid, "City").Click();

            Assert.Equal(["Name", "City", "Amount"], DataGridTestSetup.HeaderKeys(grid));
            Assert.Equal(["Name", "City"], DataGridTestSetup.FrozenHeaderKeys(grid));
            Assert.Equal("true", Toggle(grid, "City").GetAttribute("aria-pressed"));
            // The body cells follow the header.
            Assert.Equal(2, grid.FindAll("tbody td[data-omni-col='City'].omni-data-grid__column--frozen").Count);
            return Announcement(grid);
        });

        Assert.Equal("Colonne City figée", announced);
        Assert.True(preferences.Saved("orders")!.Value.GetProperty("columnFrozen").GetProperty("City").GetBoolean());
    }

    [Fact]
    public void AClick_UnfreezesADeclaredFrozenColumn_ThatJoinsTheOthers()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid();
        Open(grid);

        Toggle(grid, "Name").Click();

        Assert.Empty(DataGridTestSetup.FrozenHeaderKeys(grid));
        Assert.Equal(["Name", "Amount", "City"], DataGridTestSetup.HeaderKeys(grid));
        Assert.False(preferences.Saved("orders")!.Value.GetProperty("columnFrozen").GetProperty("Name").GetBoolean());
    }

    [Fact]
    public void TheToggle_IsANativeButtonOnTheKeyboardPath_AndKeepsTheFocusAfterTheRowMoved()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = RenderGrid();
        Open(grid);
        var toggle = Toggle(grid, "City");

        // A native button in the tab order: Enter and Space activate it as a click.
        Assert.Equal("BUTTON", toggle.TagName);
        Assert.Null(toggle.GetAttribute("tabindex"));
        Assert.False(toggle.HasAttribute("disabled"));

        toggle.Click();

        // The row took its new place among the frozen columns: the focus goes back to its toggle (step 0).
        var focus = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "focusMoveControl");
        Assert.Equal("City", focus.Arguments[1]);
        Assert.Equal(0, focus.Arguments[2]);
        Assert.Equal("0", Toggle(grid, "City").GetAttribute("data-omni-move"));
    }

    [Fact]
    public async Task TheReset_BringsTheDeclaredFrozenColumnsBack_AndSavesWithoutTheChoice()
    {
        var preferences = new GridPreferenceHarness(Services);
        var grid = RenderGrid(reset: true);
        Open(grid);
        Toggle(grid, "Name").Click();
        Toggle(grid, "City").Click();
        Assert.Equal(["City"], DataGridTestSetup.FrozenHeaderKeys(grid));

        grid.Find(".omni-data-grid__chooser-reset").Click();

        Assert.Equal(["Name"], DataGridTestSetup.FrozenHeaderKeys(grid));
        Assert.Equal(["Name", "Amount", "City"], DataGridTestSetup.HeaderKeys(grid));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, preferences.Saved("orders")!.Value.GetProperty("columnFrozen").ValueKind);

        // And from code as well.
        Toggle(grid, "City").Click();
        await grid.InvokeAsync(grid.Instance.ResetLayoutAsync);
        Assert.Equal(["Name"], DataGridTestSetup.FrozenHeaderKeys(grid));
    }

    [Fact]
    public void AMoveArrow_StaysWithinItsFrozenGroup_AfterAFreeze()
    {
        _ = new GridPreferenceHarness(Services);
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.AllowColumnFreeze, true)
            .Add(component => component.AllowColumnReorder, true)
            .Add(component => component.Columns, Columns));
        Open(grid);

        Toggle(grid, "City").Click();

        Assert.True(grid.Find("button[data-omni-col='City'][data-omni-move='1']").HasAttribute("disabled"));
        Assert.False(grid.Find("button[data-omni-col='City'][data-omni-move='-1']").HasAttribute("disabled"));
        Assert.True(grid.Find("button[data-omni-col='Amount'][data-omni-move='-1']").HasAttribute("disabled"));
    }
}
