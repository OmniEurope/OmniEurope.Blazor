using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Localization;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The export bar of a grid: which formats it offers, the reading of every row the current filters
/// and sorts select (not the page or window on screen), the file handed to the browser, and what the
/// bar says meanwhile and afterwards. Exporting changes nothing of what the grid shows.
/// </summary>
internal sealed class GridExport<TItem>(OmniDataGrid<TItem> grid) : IAsyncDisposable
{
    /// <summary>Rows asked per request of a remote grid.</summary>
    internal const int PageSize = 200;

    private CancellationTokenSource? _cancellation;
    private IJSObjectReference? _module;

    /// <summary>The format being written, or null.</summary>
    internal OmniTableExportFormat? Running { get; private set; }

    /// <summary>The last export failed; cleared by the next one.</summary>
    internal bool Failed { get; private set; }

    /// <summary>What the last export left out (a row limit, data that changed), or null when it was complete.</summary>
    internal string? Notice { get; private set; }

    private OmniTableExporter? Exporter => grid.Services.GetService(typeof(OmniTableExporter)) as OmniTableExporter;

    /// <summary>
    /// The formats asked for that somebody can write, once each, in the order asked. A grid that asks for
    /// none, the usual case, does not even look for the exporter.
    /// </summary>
    internal IReadOnlyList<OmniTableExportFormat> Formats => grid.ExportFormats.Count > 0 && Exporter is { } exporter
        ? grid.ExportFormats.Distinct().Where(exporter.Supports).ToArray()
        : [];

    internal bool ShowTop => Formats.Count > 0 && grid.ExportPosition is OmniDataGridPosition.Top or OmniDataGridPosition.TopAndBottom;

    internal bool ShowBottom => Formats.Count > 0 && grid.ExportPosition is OmniDataGridPosition.Bottom or OmniDataGridPosition.TopAndBottom;

    internal string FormatName(OmniTableExportFormat format) => grid.Text($"GridExportFormat{format}");

    /// <summary>The variant of a format's button: the host's choice (<c>ExportVariants</c>), else Ghost.</summary>
    internal OmniButtonVariant VariantOf(OmniTableExportFormat format) =>
        grid.ExportVariants is { } variants && variants.TryGetValue(format, out var variant) ? variant : OmniButtonVariant.Ghost;

    /// <summary>The file icon of a format's button.</summary>
    internal static OmniIconName IconOf(OmniTableExportFormat format) => format switch
    {
        OmniTableExportFormat.Markdown => OmniIconName.FileMd,
        OmniTableExportFormat.Csv => OmniIconName.FileCsv,
        OmniTableExportFormat.Excel => OmniIconName.FileXls,
        OmniTableExportFormat.Pdf => OmniIconName.FilePdf,
        _ => OmniIconName.Download
    };

    /// <summary>The columns an export writes: the visible ones that read a value, unless they opted out.</summary>
    internal IReadOnlyList<OmniDataGridColumnDefinition<TItem>> Columns() => grid.ColumnSet.VisibleColumns
        .Where(column => column.Exportable && (column.ExportValue is not null || column.HasValueSource))
        .ToArray();

    /// <summary>Reads the rows, writes the file and hands it to the browser. One export at a time.</summary>
    internal async Task RunAsync(OmniTableExportFormat format)
    {
        if (Running is not null || Exporter is not { } exporter)
        {
            return;
        }

        Running = format;
        Failed = false;
        Notice = null;
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        var token = cancellation.Token;
        OmniTableExportDocument? delivered = null;
        try
        {
            var document = await BuildDocumentAsync(token);
            var file = await exporter.RenderAsync(document, format, token);
            _module ??= await grid.JavaScript.InvokeAsync<IJSObjectReference>("import", token, OmniModules.DocumentEditor);
            await _module.InvokeVoidAsync("download", token, FileName(document.GeneratedAt, file.Extension), file.ContentType, file.Content.ToArray());
            Notice = NoticeOf(document);
            delivered = document;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Cancelled by the reader, or the grid left the page: no file and nothing to report.
        }
        catch (Exception exception)
        {
            Failed = true;
            await grid.OnExportError.InvokeAsync(exception);
        }
        finally
        {
            _cancellation = null;
            Running = null;
        }

        // Outside the try: the file is already with the browser, so an exception of the host's handler
        // must not report the export as failed. It propagates to the caller like any event handler's.
        if (delivered is not null)
        {
            await grid.OnExport.InvokeAsync(delivered);
        }
    }

    internal void Cancel() => _cancellation?.Cancel();

    /// <summary>Every row the current filters and sorts select, up to the row limit, as a neutral document.</summary>
    internal async Task<OmniTableExportDocument> BuildDocumentAsync(CancellationToken cancellationToken)
    {
        var columns = Columns();
        if (columns.Count == 0)
        {
            throw new InvalidOperationException("OmniDataGrid has no column to export: every visible column is a template without Value, Property or ExportValue.");
        }

        var limit = Math.Max(1, grid.ExportRowLimit);
        var read = await ReadRowsAsync(limit, cancellationToken);
        var rows = read.Rows.Select(item => (IReadOnlyList<OmniTableExportCell>)columns.Select(column => CellOf(column, item)).ToArray()).ToArray();
        return new OmniTableExportDocument
        {
            Title = Title(),
            Fields = [.. grid.ExportFields, .. FilterFields()],
            Columns = [.. columns.Select((column, index) => new OmniTableExportColumn(column.Title, KindOf(column, rows, index)))],
            Rows = rows,
            GeneratedAt = grid.Now(),
            TotalCount = read.TotalCount,
            RowLimit = limit,
            TotalIsLowerBound = read.TotalIsLowerBound,
            Culture = CultureInfo.CurrentCulture.Name
        };
    }

    private Task<TableExportRows<TItem>> ReadRowsAsync(int limit, CancellationToken cancellationToken)
    {
        if ((grid.ExportLoad ?? grid.Load) is not { } loader)
        {
            // The whole filtered and sorted local set, whatever page or window is on screen.
            var all = grid.View.VirtualLocalItems;
            return Task.FromResult(new TableExportRows<TItem>(all.Take(limit).ToArray(), all.Count, false));
        }

        var sorts = grid.Query.CurrentSorts();
        var filters = grid.Query.CurrentFilters();
        return TableExportReader.ReadAsync(
            // WaitAsync: a cancelled export stops being awaited even when the loader ignores its token.
            (page, pageSize, token) => loader(new OmniDataGridLoadRequest(page, pageSize, sorts, filters, token)).WaitAsync(token),
            limit, PageSize, grid.KeyOf, cancellationToken);
    }

    private string Title() =>
        !string.IsNullOrWhiteSpace(grid.ExportTitle) ? grid.ExportTitle
        : !string.IsNullOrWhiteSpace(grid.Caption) ? grid.Caption
        : grid.Text("GridExportBar");

    /// <summary>One header line per active column filter, so the file says which rows it answers to.</summary>
    private IEnumerable<OmniTableExportField> FilterFields() => grid.ColumnSet.EffectiveColumns
        .Where(grid.Query.HasActiveFilter)
        .Select(column => new OmniTableExportField(grid.Text("GridFilterColumn", column.Title), grid.FilterEditor.FilterSummary(column)));

    // The host's name as given; else what the table holds, from its export title or caption.
    private string FileName(DateTimeOffset generatedAt, string extension) => ExportFileName.Stamp(
        !string.IsNullOrWhiteSpace(grid.ExportFileName) ? grid.ExportFileName
            : ExportFileName.Slug(!string.IsNullOrWhiteSpace(grid.ExportTitle) ? grid.ExportTitle : grid.Caption),
        generatedAt, extension);

    private string? NoticeOf(OmniTableExportDocument document)
    {
        static PluralCount Count(int value) => new(value, "N0");
        if (document.TotalIsLowerBound)
        {
            return grid.Text("MarkdownExportLimitReachedWithoutTotal", Count(document.RowLimit));
        }

        if (document.IsComplete)
        {
            return null;
        }

        return document.Truncated
            ? grid.Text("MarkdownExportTruncated", Count(document.RowCount), Count(document.TotalCount), Count(document.RowLimit))
            : grid.Text("MarkdownExportIncomplete", Count(document.RowCount), Count(document.TotalCount));
    }

    private static OmniTableExportCell CellOf(OmniDataGridColumnDefinition<TItem> column, TItem item)
    {
        var value = (column.ExportValue ?? column.Value)(item);
        var text = (!string.IsNullOrWhiteSpace(column.FormatString)
            ? string.Format(CultureInfo.CurrentCulture, column.FormatString, value)
            : Convert.ToString(value, CultureInfo.CurrentCulture)) ?? string.Empty;
        return value switch
        {
            bool boolean => new OmniTableExportCell(text) { Boolean = boolean },
            DateTimeOffset date => new OmniTableExportCell(text) { Date = date },
            // An unspecified time is written as it is, without shifting it by the machine's zone.
            DateTime date => new OmniTableExportCell(text) { Date = date.Kind == DateTimeKind.Local ? new DateTimeOffset(date) : new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Utc)) },
            DateOnly date => new OmniTableExportCell(text) { Date = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)) },
            sbyte or byte or short or ushort or int or uint or long or ulong or decimal => new OmniTableExportCell(text) { Number = Convert.ToDecimal(value, CultureInfo.InvariantCulture) },
            // A value a decimal cannot hold (NaN, an infinity, a magnitude out of range) stays a text.
            float or double when Convert.ToDouble(value, CultureInfo.InvariantCulture) is var number && double.IsFinite(number) && Math.Abs(number) < (double)decimal.MaxValue
                => new OmniTableExportCell(text) { Number = (decimal)number },
            _ => new OmniTableExportCell(text)
        };
    }

    /// <summary>
    /// What a column holds: the one kind its filled cells share, or text when they differ. A column with
    /// no filled cell takes the kind of the property it reads.
    /// </summary>
    private static OmniTableExportValueKind KindOf(OmniDataGridColumnDefinition<TItem> column, IReadOnlyList<IReadOnlyList<OmniTableExportCell>> rows, int index)
    {
        var kinds = rows.Select(row => row[index]).Where(cell => cell.Text.Length > 0).Select(KindOf).Distinct().Take(2).ToArray();
        return kinds.Length switch
        {
            1 => kinds[0],
            0 => KindOf(column.ExportValue is null ? column.ValueType : null),
            _ => OmniTableExportValueKind.Text
        };
    }

    private static OmniTableExportValueKind KindOf(OmniTableExportCell cell) =>
        cell.Number is not null ? OmniTableExportValueKind.Number
        : cell.Date is not null ? OmniTableExportValueKind.Date
        : cell.Boolean is not null ? OmniTableExportValueKind.Boolean
        : OmniTableExportValueKind.Text;

    private static OmniTableExportValueKind KindOf(Type? type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Boolean => OmniTableExportValueKind.Boolean,
        TypeCode.DateTime => OmniTableExportValueKind.Date,
        >= TypeCode.SByte and <= TypeCode.Decimal when type is { IsEnum: false } => OmniTableExportValueKind.Number,
        _ => type == typeof(DateTimeOffset) || type == typeof(DateOnly) ? OmniTableExportValueKind.Date : OmniTableExportValueKind.Text
    };

    public async ValueTask DisposeAsync()
    {
        _cancellation?.Cancel();
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is already gone, and the module with it.
            }
        }
    }
}
