using System.Globalization;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The spreadsheet at its edges: a sheet without cells, a sheet emptied under an edit, the formula bar
/// and the cell editor taking turns, typing that outruns the editor, the script that arrives late or on a
/// lost circuit, and the model read from JSON a host wrote by hand.
/// </summary>
public sealed class SpreadsheetEdgeTests : OmniBunitContext
{
    private static OmniSpreadsheetData Budget() => OmniSpreadsheetData.FromRows([["Poste", "Janvier"], ["Loyer", "800"], ["Erreur", "=1/0"]]);

    private IRenderedComponent<OmniSpreadsheet> RenderSheet(OmniSpreadsheetData? value, List<OmniSpreadsheetData>? changes = null, Action<ComponentParameterCollectionBuilder<OmniSpreadsheet>>? extra = null) =>
        Render<OmniSpreadsheet>(parameters =>
        {
            parameters.Add(sheet => sheet.Value, value);
            if (changes is not null)
            {
                parameters.Add(sheet => sheet.ValueChanged, next => changes.Add(next));
            }

            extra?.Invoke(parameters);
        });

    [Fact]
    public void SheetWithoutCells_ShowsNoAddress_IgnoresKeys_AndTheBarDoesNotEdit()
    {
        var changes = new List<OmniSpreadsheetData>();
        var sheet = RenderSheet(null, changes);

        Assert.Equal(string.Empty, sheet.Find(".omni-spreadsheet__address").TextContent);
        sheet.Find(".omni-spreadsheet__viewport").KeyDown("ArrowDown");
        sheet.Find(".omni-spreadsheet__viewport").KeyDown("7");
        sheet.Find(".omni-spreadsheet__formula-input").Focus();
        sheet.Find(".omni-spreadsheet__formula-input").KeyDown("Enter");

        Assert.Empty(changes);
        Assert.Empty(sheet.FindAll("tbody td"));
        // Without an id the parts still get one of their own, unique to this sheet.
        Assert.Matches("^omni-spreadsheet-[0-9a-f]{32}-formula$", sheet.Find(".omni-spreadsheet__formula-input").Id);
    }

    [Fact]
    public void SheetEmptiedUnderABarEdit_CommitsNothing_AndMovesNowhere()
    {
        var changes = new List<OmniSpreadsheetData>();
        var sheet = RenderSheet(Budget(), changes);
        sheet.Find(".omni-spreadsheet__formula-input").Focus();
        sheet.Find(".omni-spreadsheet__formula-input").Input(string.Empty);

        sheet.Render(parameters => parameters.Add(component => component.Value, OmniSpreadsheetData.Create(0, 0)));
        sheet.Find(".omni-spreadsheet__formula-input").KeyDown("Enter");

        Assert.Empty(changes);
        Assert.Equal(string.Empty, sheet.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void ReadOnlySheet_BarDoesNotEdit_AndTheAddMethodsDoNothing()
    {
        var changes = new List<OmniSpreadsheetData>();
        var sheet = RenderSheet(Budget(), changes, parameters => parameters.Add(component => component.ReadOnly, true));

        sheet.Find(".omni-spreadsheet__formula-input").Focus();
        sheet.Find(".omni-spreadsheet__formula-input").KeyDown("Enter");
        sheet.InvokeAsync(sheet.Instance.AddRowAsync);
        sheet.InvokeAsync(sheet.Instance.AddColumnAsync);

        Assert.Empty(changes);
        Assert.Equal("A1", sheet.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void FocusingTheBarDuringACellEdit_LeavesTheCellEditing()
    {
        var sheet = RenderSheet(Budget(), extra: parameters => parameters.Add(component => component.Id, "budget"));
        sheet.Find("#budget-r1-c1").DoubleClick();

        sheet.Find(".omni-spreadsheet__formula-input").Focus();

        Assert.Equal("edit", sheet.Find("#budget-r1-c1 .omni-spreadsheet__editor").GetAttribute("data-omni-sheet-mode"));
    }

    [Fact]
    public void BarBlur_CommitsItsDraft()
    {
        var changes = new List<OmniSpreadsheetData>();
        var sheet = RenderSheet(Budget(), changes);

        sheet.Find(".omni-spreadsheet__formula-input").Focus();
        sheet.Find(".omni-spreadsheet__formula-input").Input("Charges");
        sheet.Find(".omni-spreadsheet__formula-input").Blur();

        Assert.Equal("Charges", Assert.Single(changes).GetInput("A1"));
    }

    [Fact]
    public void ClickOnAnotherCell_CommitsTheEdit_AndOnTheSameCellKeepsIt()
    {
        var changes = new List<OmniSpreadsheetData>();
        var sheet = RenderSheet(Budget(), changes, parameters => parameters.Add(component => component.Id, "budget"));
        sheet.Find("#budget-r1-c1").DoubleClick();
        sheet.Find(".omni-spreadsheet__editor").Input("900");

        sheet.Find("#budget-r1-c1").Click();
        Assert.Empty(changes);
        Assert.Single(sheet.FindAll(".omni-spreadsheet__editor"));

        sheet.Find("#budget-r0-c0").Click();
        Assert.Equal("900", Assert.Single(changes).GetInput("B2"));
        Assert.Equal("A1", sheet.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public void KeysTypedBeforeTheEditorHasTheFocus_AreAppended_AndANullInputIsEmpty()
    {
        var changes = new List<OmniSpreadsheetData>();
        var sheet = RenderSheet(Budget(), changes);

        sheet.Find(".omni-spreadsheet__viewport").KeyDown("4");
        sheet.Find(".omni-spreadsheet__viewport").KeyDown("2");
        Assert.Equal("42", sheet.Find(".omni-spreadsheet__editor").GetAttribute("value"));

        sheet.Find(".omni-spreadsheet__editor").Input((object?)null);
        sheet.Find(".omni-spreadsheet__editor").KeyDown("Enter");
        Assert.Equal(string.Empty, Assert.Single(changes).GetInput("A1"));
    }

    [Fact]
    public void ErrorCell_IsMarked_AndTheLabelNamesTheToolbar()
    {
        var sheet = RenderSheet(Budget(), extra: parameters => parameters
            .Add(component => component.Id, "budget")
            .Add(component => component.Label, "Budget du foyer"));

        Assert.Contains("omni-spreadsheet__cell--error", sheet.Find("#budget-r2-c1").ClassList);
        Assert.Equal("Budget du foyer", sheet.Find(".omni-spreadsheet__toolbar").GetAttribute("aria-label"));
    }

    [Fact]
    public void NoAdditionAllowed_HidesTheToolbar_AndColumnsOnlyKeepsItsButton()
    {
        var none = RenderSheet(Budget(), extra: parameters => parameters
            .Add(component => component.AllowAddRows, false)
            .Add(component => component.AllowAddColumns, false));
        Assert.Empty(none.FindAll(".omni-spreadsheet__toolbar"));

        var columns = RenderSheet(Budget(), extra: parameters => parameters.Add(component => component.AllowAddRows, false));
        Assert.Empty(columns.FindAll(".omni-spreadsheet__add-row"));
        Assert.Single(columns.FindAll(".omni-spreadsheet__add-column"));
    }

    [Fact]
    public async Task SheetGoneWhileItsScriptLoads_ReleasesItOnArrival_AndARenderAfterItDoesNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        var sheet = RenderSheet(Budget());

        await sheet.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        sheet.Render();

        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public void LostCircuitWhileAttaching_IsIgnored()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["attach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);

        var sheet = RenderSheet(Budget());

        Assert.Equal(["attach"], runtime.Module.Calls);
        Assert.Equal("A1", sheet.Find(".omni-spreadsheet__address").TextContent);
    }

    [Fact]
    public async Task Dispose_OnALostCircuit_DetachesAndIsQuiet()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var sheet = RenderSheet(Budget());

        await sheet.Instance.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    // ---- model ------------------------------------------------------------------------------------

    [Fact]
    public void FromRows_WithoutRows_GrowsToTheAskedSize()
    {
        var sheet = OmniSpreadsheetData.FromRows([], rowCount: 2, columnCount: 3);

        Assert.Equal((2, 3), (sheet.RowCount, sheet.ColumnCount));
        Assert.Equal(string.Empty, sheet.GetInput(1, 2));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(5, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 5)]
    [InlineData(1, 1)]
    public void GetInput_OutsideTheCellsOfARow_IsEmpty(int row, int column)
    {
        // Read as is from JSON, the second row is shorter than the column count.
        var sheet = JsonSerializer.Deserialize<OmniSpreadsheetData>("""{"ColumnCount":2,"Rows":[["a","b"],["c"]]}""")!;

        Assert.Equal(string.Empty, sheet.GetInput(row, column));
    }

    [Fact]
    public void NullCellsReadAsIs_AreEmpty_InEveryReadingAndInJson()
    {
        var sheet = JsonSerializer.Deserialize<OmniSpreadsheetData>("""{"ColumnCount":1,"Rows":[[null]]}""")!;

        Assert.Equal(string.Empty, sheet.GetInput(0, 0));
        Assert.Equal("""{"columnCount":1,"rows":[[""]]}""", sheet.ToJson());
        Assert.Equal(string.Empty, sheet.WithInput(0, 0, null).GetInput("A1"));
    }

    [Fact]
    public void UnreadableAddresses_GiveEmptyInputsAndValues()
    {
        var sheet = Budget();

        Assert.Equal(string.Empty, sheet.GetInput("1A"));
        Assert.Equal(OmniSpreadsheetValue.Empty, sheet.Evaluate("??"));
    }

    [Theory]
    [InlineData("""{"rows":[["a"]]}""", 1)]
    [InlineData("""{"columnCount":"trois","rows":[["a"]]}""", 1)]
    [InlineData("""{"columnCount":-2,"rows":[["a"]]}""", 1)]
    [InlineData("""{"columnCount":3,"rows":[["a"],"pas une ligne"]}""", 3)]
    public void FromJson_ToleratesWhatAHostWroteByHand(string json, int columns)
    {
        var sheet = OmniSpreadsheetData.FromJson(json);

        Assert.Equal(columns, sheet.ColumnCount);
        Assert.Equal("a", sheet.GetInput("A1"));
    }

    [Fact]
    public void Value_NullTextIsEmpty_AndANumberFollowsTheGivenCulture()
    {
        Assert.Equal(string.Empty, OmniSpreadsheetValue.FromText(null!).Text);
        Assert.Equal("1,234.5", OmniSpreadsheetValue.FromNumber(1234.5).ToDisplayString(CultureInfo.InvariantCulture));
    }
}
