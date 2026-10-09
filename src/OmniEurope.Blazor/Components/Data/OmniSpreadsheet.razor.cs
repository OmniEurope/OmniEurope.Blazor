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
    private const string ModulePath = OmniModules.Spreadsheet;
    private const int PageStep = 10;

    // Two sheets without an Id never share the ids of their formula bar and cells.
    private readonly string _generatedId = $"omni-spreadsheet-{Guid.NewGuid():N}";

    private ElementReference _root;
    private ElementReference _grid;
    private ElementReference _editor;
    private IJSObjectReference? _module;
    private bool _attached;
    private bool _disposed;

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

    /// <summary>
    /// Raised with the new sheet after each change: a cell input committed with a different value, or a
    /// row or column added. Never raised while <see cref="ReadOnly"/>.
    /// </summary>
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
    public string? Label { get; set; }

    /// <summary>Raised with the A1 address of the active cell whenever it moves.</summary>
    [Parameter]
    public EventCallback<string> ActiveCellChanged { get; set; }

    private static readonly OmniSpreadsheetData NoSheet = OmniSpreadsheetData.Create(0, 0);

    private OmniSpreadsheetData Sheet => Value ?? NoSheet;

    private bool HasCells => Sheet.RowCount > 0 && Sheet.ColumnCount > 0;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("SpreadsheetLabel") : Label;

    private bool ShowToolbar => !ReadOnly && (AllowAddRows || AllowAddColumns);

    private string ActiveAddress => HasCells ? OmniSpreadsheetData.Address(_row, _column) : string.Empty;

    private string FormulaBarId => $"{Id ?? _generatedId}-formula";

    private string FormulaBarText => _editing == EditSource.None ? Sheet.GetInput(_row, _column) : _draft;

    private string RootClass() => Css(
        "omni-spreadsheet",
        ReadOnly ? "omni-spreadsheet--readonly" : null,
        ShowGridLines ? null : "omni-spreadsheet--no-lines");

    private string CellId(int row, int column) => $"{Id ?? _generatedId}-r{row.ToString(CultureInfo.InvariantCulture)}-c{column.ToString(CultureInfo.InvariantCulture)}";

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

    /// <summary>Keeps the active cell inside the sheet when the new <see cref="Value"/> has fewer rows or columns.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A sheet that shrank under the active cell keeps it inside.
        _row = Math.Clamp(_row, 0, Math.Max(0, Sheet.RowCount - 1));
        _column = Math.Clamp(_column, 0, Math.Max(0, Sheet.ColumnCount - 1));
    }

    /// <summary>
    /// Attaches the sheet script once, then moves the focus where the last action asked (the cell editor
    /// or the grid) and scrolls the active cell into view when requested. Does nothing once disposed;
    /// a lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">Unused: the script is attached on any render that finds it not attached yet.</param>
    /// <returns>A task that completes when the script calls are done.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await ApplyRenderRequestsAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task ApplyRenderRequestsAsync()
    {
        if (!_attached)
        {
            _module ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
            if (_disposed)
            {
                // Disposed while the script loaded: DisposeAsync had no module to release yet.
                await _module.DisposeAsync();
                return;
            }

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

    private Task OnGridKeyDownAsync(KeyboardEventArgs args)
    {
        if (!HasCells || args.AltKey || args.MetaKey)
        {
            return Task.CompletedTask;
        }

        return TargetOf(args) is { } target ? MoveToAsync(target.Row, target.Column) : EditFromKeyAsync(args);
    }

    /// <summary>The cell a navigation key goes to (Ctrl jumps to the edge), or null for a key that does not move.</summary>
    private (int Row, int Column)? TargetOf(KeyboardEventArgs args) => args.Key switch
    {
        "Tab" => TabTarget(args.ShiftKey),
        "Home" => (args.CtrlKey ? 0 : _row, 0),
        "End" => (args.CtrlKey ? Sheet.RowCount - 1 : _row, Sheet.ColumnCount - 1),
        "PageUp" => (_row - PageStep, _column),
        "PageDown" => (_row + PageStep, _column),
        _ => ArrowTarget(args.Key, args.CtrlKey)
    };

    /// <summary>The cell an arrow goes to: the next one that way, or with Ctrl the edge of the sheet.</summary>
    private (int Row, int Column)? ArrowTarget(string key, bool jump) => key switch
    {
        "ArrowUp" => (jump ? 0 : _row - 1, _column),
        "ArrowDown" => (jump ? Sheet.RowCount - 1 : _row + 1, _column),
        "ArrowLeft" => (_row, jump ? 0 : _column - 1),
        "ArrowRight" => (_row, jump ? Sheet.ColumnCount - 1 : _column + 1),
        _ => null
    };

    // At the edge of the row Tab leaves the sheet: the script let the browser move focus.
    private (int Row, int Column)? TabTarget(bool backwards) =>
        (backwards ? _column > 0 : _column < Sheet.ColumnCount - 1) ? (_row, _column + (backwards ? -1 : 1)) : null;

    /// <summary>Enter or F2 edits the cell, Delete empties it, a printable key starts typing over it.</summary>
    private Task EditFromKeyAsync(KeyboardEventArgs args)
    {
        var jump = args.CtrlKey;
        return args.Key switch
        {
            "Enter" when !jump => BeginCellEditAsync(_row, _column, keepInput: true),
            "F2" => BeginCellEditAsync(_row, _column, keepInput: true),
            "Delete" or "Backspace" when !jump => WriteAsync(_row, _column, string.Empty),
            _ when !jump && args.Key.Length == 1 => BeginTypingAsync(args.Key),
            _ => Task.CompletedTask
        };
    }

    private Task OnEditorKeyDownAsync(KeyboardEventArgs args) => args.Key switch
    {
        "Enter" => CommitAndMoveAsync(args.ShiftKey ? -1 : 1, 0),
        "Tab" => CommitAndMoveAsync(0, args.ShiftKey ? -1 : 1),
        "Escape" => CancelToGridAsync(),
        // Typing over a cell is the quick entry mode: an arrow commits and moves, as in any spreadsheet.
        // Once in edit mode (Enter, F2, double click) the arrows move the caret.
        _ when _replacing && ArrowSteps.TryGetValue(args.Key, out var step) => CommitAndMoveAsync(step.Rows, step.Columns),
        _ => Task.CompletedTask
    };

    private static readonly Dictionary<string, (int Rows, int Columns)> ArrowSteps = new(StringComparer.Ordinal)
    {
        ["ArrowUp"] = (-1, 0),
        ["ArrowDown"] = (1, 0),
        ["ArrowLeft"] = (0, -1),
        ["ArrowRight"] = (0, 1),
    };

    /// <summary>Commits the edit, moves by the given steps and gives the focus back to the grid.</summary>
    private async Task CommitAndMoveAsync(int rows, int columns)
    {
        await CommitAsync();
        await MoveToAsync(_row + rows, _column + columns);
        _focusGrid = true;
    }

    private Task CancelToGridAsync()
    {
        CancelEdit();
        _focusGrid = true;
        return Task.CompletedTask;
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

        await (args.Key switch
        {
            "Enter" => CommitAndMoveAsync(args.ShiftKey ? -1 : 1, 0),
            "Escape" => CancelToGridAsync(),
            _ => Task.CompletedTask
        });
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

    /// <summary>Detaches and releases the sheet script.</summary>
    /// <returns>A task that completes when the script is released.</returns>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
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
