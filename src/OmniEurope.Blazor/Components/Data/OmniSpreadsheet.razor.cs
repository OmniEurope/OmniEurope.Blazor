using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A basic spreadsheet: a grid of cells named by column letters and row numbers, one active cell
/// moved with the keyboard, inline editing, a formula bar, and formulas computed over the sheet.
/// </summary>
/// <remarks>
/// The content is an <see cref="OmniSpreadsheetData"/> bound with <c>@bind-Value</c>. A cell holds
/// what was typed: a number, a text, or a formula after <c>=</c> using references (<c>B3</c>),
/// ranges (<c>A1:A5</c>), <c>+ - * / ^ %</c>, parentheses and SUM, AVERAGE, MIN, MAX, COUNT, ROUND
/// and ABS; what it shows is computed (see <see cref="OmniSpreadsheetValue"/> for the error codes).
/// <para>
/// Keyboard: arrows move, Tab and Shift+Tab move along the row and leave the sheet at its edge,
/// Enter or F2 edit the active cell, typing replaces it, Enter commits and moves down, Tab commits
/// and moves right, Escape cancels, Delete empties the cell, Home and End reach the ends of the
/// row, Ctrl+Home and Ctrl+End the corners, Page Up and Page Down move ten rows. The sheet is one
/// focus stop; the active cell is announced through <c>aria-activedescendant</c>.
/// </para>
/// <para>
/// Strict CSP: no style attribute and no inline handler. <c>omni-spreadsheet.js</c> only decides
/// which keys keep their browser default (Blazor cannot cancel a key conditionally) and scrolls the
/// active cell into view. Column width and sheet height are the CSS variables
/// <c>--omni-spreadsheet-column-width</c> and <c>--omni-spreadsheet-height</c>.
/// </para>
/// </remarks>
public partial class OmniSpreadsheet
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-spreadsheet.js";
    private const int PageStep = 10;

    private ElementReference _root;
    private ElementReference _grid;
    private ElementReference _editor;
    private IJSObjectReference? _module;
    private bool _attached;

    private OmniSpreadsheetData? _evaluated;
    private SpreadsheetEvaluator? _evaluator;
    private int _row;
    private int _column;
    private EditSource _editing;
    private bool _replacing;
    private string _draft = string.Empty;
    private bool _focusEditor;
    private bool _focusGrid;
    private bool _reveal;

    private enum EditSource
    {
        None,
        Cell,
        FormulaBar
    }

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The sheet. Use <c>@bind-Value</c>: every edit produces a new sheet.</summary>
    [Parameter]
    public OmniSpreadsheetData? Value { get; set; }

    [Parameter]
    public EventCallback<OmniSpreadsheetData> ValueChanged { get; set; }

    /// <summary>Cells can be selected and read, not changed; the toolbar is hidden.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Shows the bar above the sheet with the active address and its raw input.</summary>
    [Parameter]
    public bool ShowFormulaBar { get; set; } = true;

    /// <summary>Draws the lines between cells.</summary>
    [Parameter]
    public bool ShowGridLines { get; set; } = true;

    /// <summary>Offers a button that adds a row at the bottom.</summary>
    [Parameter]
    public bool AllowAddRows { get; set; } = true;

    /// <summary>Offers a button that adds a column on the right.</summary>
    [Parameter]
    public bool AllowAddColumns { get; set; } = true;

    /// <summary>Accessible name of the sheet. Defaults to a localized "Spreadsheet".</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Raised with the A1 address of the active cell whenever it moves.</summary>
    [Parameter]
    public EventCallback<string> ActiveCellChanged { get; set; }

    private static readonly OmniSpreadsheetData NoSheet = OmniSpreadsheetData.Create(0, 0);

    private OmniSpreadsheetData Sheet => Value ?? NoSheet;

    private bool HasCells => Sheet.RowCount > 0 && Sheet.ColumnCount > 0;

    private string Label => string.IsNullOrWhiteSpace(AriaLabel) ? Localize("SpreadsheetLabel") : AriaLabel;

    private bool ShowToolbar => !ReadOnly && (AllowAddRows || AllowAddColumns);

    private string ActiveAddress => HasCells ? OmniSpreadsheetData.Address(_row, _column) : string.Empty;

    private string FormulaBarId => $"{Id ?? "omni-spreadsheet"}-formula";

    private string FormulaBarText => _editing == EditSource.None ? Sheet.GetInput(_row, _column) : _draft;

    private string RootClass() => Css(
        "omni-spreadsheet",
        ReadOnly ? "omni-spreadsheet--readonly" : null,
        ShowGridLines ? null : "omni-spreadsheet--no-lines");

    private string CellId(int row, int column) => $"{Id ?? "omni-spreadsheet"}-r{row.ToString(CultureInfo.InvariantCulture)}-c{column.ToString(CultureInfo.InvariantCulture)}";

    private bool IsActive(int row, int column) => row == _row && column == _column;

    private bool IsEditingCell(int row, int column) => _editing == EditSource.Cell && IsActive(row, column);

    private bool IsDraftShownIn(int row, int column) => _editing == EditSource.FormulaBar && IsActive(row, column);

    private string ColumnHeaderClass(int column) => column == _column
        ? "omni-spreadsheet__column-header omni-spreadsheet__column-header--active"
        : "omni-spreadsheet__column-header";

    private string RowHeaderClass(int row) => row == _row
        ? "omni-spreadsheet__row-header omni-spreadsheet__row-header--active"
        : "omni-spreadsheet__row-header";

    private string CellClass(int row, int column, OmniSpreadsheetValue value) => CssClassBuilder.Combine([
        "omni-spreadsheet__cell",
        value.Kind switch
        {
            OmniSpreadsheetValueKind.Number => "omni-spreadsheet__cell--number",
            OmniSpreadsheetValueKind.Error => "omni-spreadsheet__cell--error",
            _ => null
        },
        IsActive(row, column) ? "omni-spreadsheet__cell--active" : null,
        IsEditingCell(row, column) ? "omni-spreadsheet__cell--editing" : null
    ]);

    /// <summary>
    /// The computed value of a cell. One evaluator per sheet instance: every cell is computed once
    /// per edit, whatever the number of formulas reading it.
    /// </summary>
    private OmniSpreadsheetValue ValueOf(int row, int column)
    {
        if (!ReferenceEquals(_evaluated, Sheet) || _evaluator is null)
        {
            _evaluated = Sheet;
            _evaluator = new SpreadsheetEvaluator(Sheet);
        }

        return _evaluator.Evaluate(row, column);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A sheet that shrank under the active cell keeps it inside.
        _row = Math.Clamp(_row, 0, Math.Max(0, Sheet.RowCount - 1));
        _column = Math.Clamp(_column, 0, Math.Max(0, Sheet.ColumnCount - 1));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_attached)
        {
            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await _module.InvokeVoidAsync("attach", _root);
            _attached = true;
        }

        if (_focusEditor && _editing == EditSource.Cell)
        {
            _focusEditor = false;
            await _module!.InvokeVoidAsync("focusEditor", _editor);
        }
        else if (_focusGrid)
        {
            _focusGrid = false;
            await _grid.FocusAsync(preventScroll: true);
        }

        if (_reveal && HasCells)
        {
            _reveal = false;
            await _module!.InvokeVoidAsync("reveal", _root, CellId(_row, _column));
        }
    }

    // ---- selection --------------------------------------------------------------------------------

    private async Task SelectAsync(int row, int column)
    {
        if (_editing != EditSource.None && !IsActive(row, column))
        {
            await CommitAsync();
        }

        await MoveToAsync(row, column);
    }

    private async Task MoveToAsync(int row, int column)
    {
        if (!HasCells)
        {
            return;
        }

        var nextRow = Math.Clamp(row, 0, Sheet.RowCount - 1);
        var nextColumn = Math.Clamp(column, 0, Sheet.ColumnCount - 1);
        _reveal = true;
        if (nextRow == _row && nextColumn == _column)
        {
            return;
        }

        _row = nextRow;
        _column = nextColumn;
        await ActiveCellChanged.InvokeAsync(ActiveAddress);
    }

    // ---- keyboard ---------------------------------------------------------------------------------

    private async Task OnGridKeyDownAsync(KeyboardEventArgs args)
    {
        if (!HasCells || args.AltKey || args.MetaKey)
        {
            return;
        }

        var jump = args.CtrlKey;
        switch (args.Key)
        {
            case "ArrowUp":
                await MoveToAsync(jump ? 0 : _row - 1, _column);
                break;
            case "ArrowDown":
                await MoveToAsync(jump ? Sheet.RowCount - 1 : _row + 1, _column);
                break;
            case "ArrowLeft":
                await MoveToAsync(_row, jump ? 0 : _column - 1);
                break;
            case "ArrowRight":
                await MoveToAsync(_row, jump ? Sheet.ColumnCount - 1 : _column + 1);
                break;
            case "Tab":
                // At the edge of the row Tab leaves the sheet: the script let the browser move focus.
                if (args.ShiftKey ? _column > 0 : _column < Sheet.ColumnCount - 1)
                {
                    await MoveToAsync(_row, _column + (args.ShiftKey ? -1 : 1));
                }

                break;
            case "Home":
                await MoveToAsync(jump ? 0 : _row, 0);
                break;
            case "End":
                await MoveToAsync(jump ? Sheet.RowCount - 1 : _row, Sheet.ColumnCount - 1);
                break;
            case "PageUp":
                await MoveToAsync(_row - PageStep, _column);
                break;
            case "PageDown":
                await MoveToAsync(_row + PageStep, _column);
                break;
            case "Enter" when !jump:
            case "F2":
                await BeginCellEditAsync(_row, _column, keepInput: true);
                break;
            case "Delete" or "Backspace" when !jump:
                await WriteAsync(_row, _column, string.Empty);
                break;
            default:
                if (!jump && args.Key.Length == 1)
                {
                    await BeginTypingAsync(args.Key);
                }

                break;
        }
    }

    private async Task OnEditorKeyDownAsync(KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "Enter":
                await CommitAsync();
                await MoveToAsync(_row + (args.ShiftKey ? -1 : 1), _column);
                _focusGrid = true;
                break;
            case "Tab":
                await CommitAsync();
                await MoveToAsync(_row, _column + (args.ShiftKey ? -1 : 1));
                _focusGrid = true;
                break;
            case "Escape":
                CancelEdit();
                _focusGrid = true;
                break;
            case "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight" when _replacing:
                // Typing over a cell is the quick entry mode: an arrow commits and moves, as in any
                // spreadsheet. Once in edit mode (Enter, F2, double click) the arrows move the caret.
                await CommitAsync();
                await MoveToAsync(
                    _row + (args.Key == "ArrowDown" ? 1 : args.Key == "ArrowUp" ? -1 : 0),
                    _column + (args.Key == "ArrowRight" ? 1 : args.Key == "ArrowLeft" ? -1 : 0));
                _focusGrid = true;
                break;
        }
    }

    private async Task OnEditorBlurAsync()
    {
        if (_editing == EditSource.Cell)
        {
            await CommitAsync();
        }
    }

    private async Task OnBarKeyDownAsync(KeyboardEventArgs args)
    {
        if (_editing != EditSource.FormulaBar)
        {
            return;
        }

        switch (args.Key)
        {
            case "Enter":
                await CommitAsync();
                await MoveToAsync(_row + (args.ShiftKey ? -1 : 1), _column);
                _focusGrid = true;
                break;
            case "Escape":
                CancelEdit();
                _focusGrid = true;
                break;
        }
    }

    private async Task OnBarBlurAsync()
    {
        if (_editing == EditSource.FormulaBar)
        {
            await CommitAsync();
        }
    }

    // ---- editing ----------------------------------------------------------------------------------

    private void OnDraftInput(ChangeEventArgs args) => _draft = args.Value?.ToString() ?? string.Empty;

    private async Task BeginCellEditAsync(int row, int column, bool keepInput)
    {
        if (ReadOnly || !HasCells)
        {
            return;
        }

        await MoveToAsync(row, column);
        _editing = EditSource.Cell;
        _replacing = !keepInput;
        _draft = keepInput ? Sheet.GetInput(_row, _column) : string.Empty;
        _focusEditor = true;
    }

    /// <summary>
    /// A key typed on the sheet starts an edit that replaces the cell with it. A key arriving while
    /// that edit is starting (fast typing, before the editor has the focus) is appended to it.
    /// </summary>
    private async Task BeginTypingAsync(string key)
    {
        if (ReadOnly)
        {
            return;
        }

        if (_editing == EditSource.Cell)
        {
            _draft += key;
            return;
        }

        await BeginCellEditAsync(_row, _column, keepInput: false);
        _draft = key;
    }

    private void BeginBarEdit()
    {
        if (ReadOnly || !HasCells || _editing != EditSource.None)
        {
            return;
        }

        _editing = EditSource.FormulaBar;
        _replacing = false;
        _draft = Sheet.GetInput(_row, _column);
    }

    private void CancelEdit()
    {
        _editing = EditSource.None;
        _replacing = false;
        _draft = string.Empty;
    }

    private async Task CommitAsync()
    {
        if (_editing == EditSource.None)
        {
            return;
        }

        var draft = _draft;
        CancelEdit();
        await WriteAsync(_row, _column, draft);
    }

    private async Task WriteAsync(int row, int column, string input)
    {
        if (ReadOnly || string.Equals(Sheet.GetInput(row, column), input, StringComparison.Ordinal))
        {
            return;
        }

        await PublishAsync(Sheet.WithInput(row, column, input));
    }

    /// <summary>Adds an empty row at the bottom of the sheet.</summary>
    public Task AddRowAsync() => ReadOnly ? Task.CompletedTask : PublishAsync(Sheet.AddRow());

    /// <summary>Adds an empty column on the right of the sheet.</summary>
    public Task AddColumnAsync() => ReadOnly ? Task.CompletedTask : PublishAsync(Sheet.AddColumn());

    private async Task PublishAsync(OmniSpreadsheetData next)
    {
        Value = next;
        await ValueChanged.InvokeAsync(next);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            if (_attached)
            {
                await _module.InvokeVoidAsync("detach", _root);
            }

            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
