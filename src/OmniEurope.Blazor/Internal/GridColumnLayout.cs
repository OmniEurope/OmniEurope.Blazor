using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The column widths of a grid: the widths the reader chose by dragging, fitting to content or with
/// the arrow keys, the handles that offer those gestures and the narrowest width the table may take.
/// </summary>
internal sealed class GridColumnLayout<TItem>(OmniDataGrid<TItem> grid)
{
    internal const double MinimumColumnWidth = 48d;

    /// <summary>Browser default root font size, used to turn rem/em widths into pixels.</summary>
    private const double RootFontSize = 16d;

    private readonly Dictionary<string, string?> _columnWidths = new(StringComparer.Ordinal);

    /// <summary>The widths chosen by the reader, by column key.</summary>
    internal IReadOnlyDictionary<string, string?> Widths => _columnWidths;

    /// <summary>The width of a column: the reader's choice, else its declared width.</summary>
    internal string? WidthOf(OmniDataGridColumnDefinition<TItem> column) =>
        _columnWidths.GetValueOrDefault(column.Key, column.Width);

    internal void Forget(string key) => _columnWidths.Remove(key);

    /// <summary>Replaces the widths with the ones restored from the persisted state.</summary>
    internal void Restore(IEnumerable<KeyValuePair<string, string?>> widths)
    {
        _columnWidths.Clear();
        foreach (var (columnKey, width) in widths)
        {
            _columnWidths[columnKey] = width;
        }
    }

    internal bool IsResizable(OmniDataGridColumnDefinition<TItem> column) => grid.AllowColumnResize && column.Resizable != false;

    internal bool IsAutoFit(OmniDataGridColumnDefinition<TItem> column) => column.AutoFit ?? grid.AllowColumnAutoFit;

    // The trailing-edge handle serves both gestures, so it is there as soon as either one is allowed.
    internal bool HasEdgeHandle(OmniDataGridColumnDefinition<TItem> column) => IsResizable(column) || IsAutoFit(column);

    internal int ResizeAriaValueNow(OmniDataGridColumnDefinition<TItem> column) => (int)Math.Round(Math.Clamp(
        ParseWidth(WidthOf(column)),
        MinimumColumnWidth,
        2000d));

    /// <summary>
    /// Narrowest the table may become, as a CSS length: every column's declared width, or its
    /// minimum, or the auto-column floor, plus the grid's own control columns. Under the fixed table
    /// layout the columns without a width share whatever the declared ones leave, which is nothing
    /// once the viewport is narrower than their sum: they collapsed to zero, titles and filters
    /// piled on top of each other. Below this width the viewport scrolls sideways instead.
    /// </summary>
    internal string TableMinimumWidth()
    {
        var terms = grid.ColumnSet.VisibleColumns
            .Select(column => WidthOf(column) ?? column.MinWidth)
            .Select(width => IsAbsoluteLength(width) ? width! : "var(--omni-data-grid-column-min-width)")
            .ToList();
        if (grid.ColumnSet.ShowDetailColumn)
        {
            terms.Add("var(--omni-data-grid-control-width)");
        }

        if (grid.SelectionMode != OmniDataGridSelectionMode.None)
        {
            terms.Add("var(--omni-data-grid-control-width)");
        }

        if (grid.ColumnSet.HasEditing)
        {
            terms.Add("var(--omni-data-grid-edit-width)");
        }

        return terms.Count == 0 ? "0px" : $"calc({string.Join(" + ", terms)})";
    }

    /// <summary>
    /// A percentage, or any expression the table itself resolves, cannot be added to a minimum
    /// width without referring back to that width; such a column counts as an auto one.
    /// </summary>
    private static bool IsAbsoluteLength(string? width)
    {
        if (string.IsNullOrWhiteSpace(width) || width.Contains('%', StringComparison.Ordinal))
        {
            return false;
        }

        var trimmed = width.Trim();
        var digits = trimmed.AsSpan().TrimEnd("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        return digits.Length > 0
            && double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            && trimmed[digits.Length..].ToLowerInvariant() is "px" or "rem" or "em" or "ch" or "pt" or "vw";
    }

    /// <summary>Every width input of the visible columns in one value, so a change re-runs the layout interop.</summary>
    internal string Signature() => string.Join(
        '|',
        grid.ColumnSet.VisibleColumns.Select(column =>
            $"{column.Key}:{WidthOf(column)}:{column.MinWidth}:{column.Frozen}"));

    /// <summary>The column specifications the script applies to the column elements.</summary>
    internal object Specs() => grid.ColumnSet.VisibleColumns.Select(column => new
    {
        key = column.Key,
        width = WidthOf(column),
        minWidth = column.MinWidth,
        frozen = column.Frozen
    }).ToArray();

    private async Task ResizeColumnAsync(OmniDataGridColumnDefinition<TItem> column, int step)
    {
        var current = ParseWidth(WidthOf(column));
        await ApplyColumnWidthAsync(column.Key, current + (step * 32d));
    }

    /// <summary>
    /// Keyboard equivalents of the edge handle: one drag step per arrow press, and Enter for the
    /// double click's fit to content. Each follows its own option.
    /// </summary>
    internal Task ResizeKeyDownAsync(OmniDataGridColumnDefinition<TItem> column, string key) => key switch
    {
        "ArrowLeft" when IsResizable(column) => ResizeColumnAsync(column, -1),
        "ArrowRight" when IsResizable(column) => ResizeColumnAsync(column, 1),
        "Enter" when IsAutoFit(column) && grid.Script.CanAutoFit => grid.Script.AutoFitColumnAsync(column.Key),
        _ => Task.CompletedTask
    };

    /// <summary>The distinct texts of a column over every loaded row, for the script's fit to content.</summary>
    internal string[]? AutoFitTexts(string key)
    {
        var column = grid.ColumnSet.FindVisible(key);
        if (column is null || !IsAutoFit(column) || column.Template is not null)
        {
            return null;
        }

        return grid.View.LoadedItems()
            .Select(item => CellText(column, item))
            .Where(text => !string.IsNullOrEmpty(text))
            .Distinct(StringComparer.Ordinal)
            .ToArray()!;
    }

    /// <summary>Applies the width a fit to content chose, when the column fits to content.</summary>
    internal async Task AutoFitAsync(string key, double width)
    {
        var column = grid.ColumnSet.FindVisible(key);
        if (column is null || !IsAutoFit(column))
        {
            return;
        }

        await ApplyColumnWidthAsync(key, width);
        grid.Render();
    }

    /// <summary>Applies the final width of a drag, when resizing is on and the column is visible.</summary>
    internal async Task ResizedAsync(string key, double width)
    {
        if (!grid.AllowColumnResize || grid.ColumnSet.VisibleColumns.All(column => column.Key != key))
        {
            return;
        }

        await ApplyColumnWidthAsync(key, width);
        grid.Render();
    }

    private async Task ApplyColumnWidthAsync(string key, double width)
    {
        var clamped = Math.Max(MinimumColumnWidth, width);
        var value = $"{clamped.ToString(CultureInfo.InvariantCulture)}px";
        _columnWidths[key] = value;
        grid.LayoutInterop.InvalidateColumnLayout();
        var change = new OmniDataGridColumnWidthChange(key, value);
        await grid.OnColumnResize.InvokeAsync(change);
        await grid.Persistence.SaveAsync();
    }

    /// <summary>
    /// Reads a declared CSS width back into pixels for the keyboard resize steps. Only absolute
    /// units can be resolved without measuring the document, so a percentage or any other relative
    /// unit falls back to the default estimate rather than silently pretending to be pixels.
    /// </summary>
    private static double ParseWidth(string? width)
    {
        const double fallback = 160d;
        if (string.IsNullOrWhiteSpace(width))
        {
            return fallback;
        }

        var trimmed = width.Trim();
        var digits = trimmed.AsSpan().TrimEnd("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ%");
        if (digits.Length == 0
            || !double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return fallback;
        }

        var unit = trimmed.AsSpan(digits.Length).Trim().ToString().ToLowerInvariant();
        return unit switch
        {
            "" or "px" => parsed,
            "rem" or "em" => parsed * RootFontSize,
            "pt" => parsed * 4d / 3d,
            _ => fallback
        };
    }

    /// <summary>What a column without a template shows for an item; the fit to content measures the same text.</summary>
    internal static string? CellText(OmniDataGridColumnDefinition<TItem> column, TItem item)
    {
        var value = column.Value(item);
        return !string.IsNullOrWhiteSpace(column.FormatString)
            ? string.Format(CultureInfo.CurrentCulture, column.FormatString, value)
            : value?.ToString();
    }
}
