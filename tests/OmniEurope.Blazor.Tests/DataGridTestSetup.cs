using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The grid setups the data grid suites share: a grid over a list with its columns, and the columns
/// written by hand into a render tree, each numbered after its own sequence.
/// </summary>
internal static class DataGridTestSetup
{
    /// <summary>A grid over <paramref name="items"/> with <paramref name="columns"/>; <paramref name="configure"/> adds the rest.</summary>
    internal static IRenderedComponent<OmniDataGrid<TItem>> RenderDataGrid<TItem>(
        this BunitContext context,
        IReadOnlyList<TItem> items,
        RenderFragment columns,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<TItem>>>? configure = null) =>
        context.Render<OmniDataGrid<TItem>>(parameters =>
        {
            parameters.Add(grid => grid.Items, items).Add(grid => grid.Columns, columns);
            configure?.Invoke(parameters);
        });

    /// <summary>A virtual grid over <paramref name="items"/> with rows estimated at <paramref name="rowHeight"/>; <paramref name="configure"/> adds the rest.</summary>
    internal static IRenderedComponent<OmniDataGrid<int>> RenderVirtualNumbers(
        this BunitContext context,
        int[] items,
        double rowHeight,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<int>>>? configure = null) =>
        context.Render<OmniDataGrid<int>>(parameters =>
        {
            parameters
                .Add(component => component.Items, items)
                .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
                .Add(component => component.EstimatedRowHeight, rowHeight);
            configure?.Invoke(parameters);
        });

    /// <summary>
    /// A virtual grid of numbers loaded remotely in blocks of <paramref name="blockSize"/> out of
    /// <paramref name="total"/>, every request kept in <paramref name="requests"/>.
    /// </summary>
    internal static IRenderedComponent<OmniDataGrid<int>> RenderVirtualRemoteNumbers(
        this BunitContext context,
        List<OmniDataGridLoadRequest> requests,
        int blockSize,
        int total) =>
        context.Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual)
            .Add(component => component.EstimatedRowHeight, 40d)
            .Add(component => component.VirtualBlockSize, blockSize)
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<int>(Enumerable.Range(request.Skip, request.Top).ToArray(), total));
            }));

    /// <summary>The filter kinds host loading remotely, every request kept in <paramref name="requests"/> and answered with no row.</summary>
    internal static IRenderedComponent<DataGridFilterKindsTestHost> RenderRemoteFilterKinds(
        this BunitContext context,
        List<OmniDataGridLoadRequest> requests) =>
        context.Render<DataGridFilterKindsTestHost>(parameters => parameters
            .Add(component => component.Load, request =>
            {
                requests.Add(request);
                return Task.FromResult(new OmniDataGridResult<DataGridFilterKindsTestHost.Ticket>([], 0));
            }));

    /// <summary>The footer bar with only its CSV export, whose document goes to <paramref name="onExport"/>.</summary>
    internal static ComponentParameterCollectionBuilder<OmniDataGrid<TItem>> ExportingCsvOnlyTo<TItem>(
        this ComponentParameterCollectionBuilder<OmniDataGrid<TItem>> parameters,
        Action<OmniTableExportDocument> onExport) =>
        parameters
            .Add(component => component.ShowFooterBar, true)
            .Add(component => component.ExportMarkdown, false)
            .Add(component => component.ExportExcel, false)
            .Add(component => component.ExportPdf, false)
            .Add(component => component.OnExport, onExport);

    /// <summary>The column keys of the header row, in their order on screen.</summary>
    internal static string[] HeaderKeys<TItem>(IRenderedComponent<OmniDataGrid<TItem>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col]").Select(cell => cell.GetAttribute("data-omni-col")!)];

    /// <summary>The column keys of the frozen header cells, in their order on screen.</summary>
    internal static string[] FrozenHeaderKeys<TItem>(IRenderedComponent<OmniDataGrid<TItem>> grid) =>
        [.. grid.FindAll("thead tr:first-child th[data-omni-col].omni-data-grid__column--frozen").Select(cell => cell.GetAttribute("data-omni-col")!)];

    /// <summary>One column per entry, the n-th at sequence n * 10, bound to its property and then given its other parameters.</summary>
    internal static RenderFragment PropertyColumns<TItem>(params (string Property, (string Name, object? Value)[] More)[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            ChartRenderTreeHelpers.Child<OmniDataGridColumn<TItem>>(
                builder,
                index * 10,
                [(nameof(OmniDataGridColumn<TItem>.Property), columns[index].Property), .. columns[index].More]);
        }
    };

    /// <summary>One column per entry, keyed and bound to its property, the n-th at sequence n * 3.</summary>
    internal static RenderFragment KeyedPropertyColumns<TItem>(params (string Key, string Property)[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            ChartRenderTreeHelpers.Child<OmniDataGridColumn<TItem>>(
                builder,
                index * 3,
                (nameof(OmniDataGridColumn<TItem>.Key), columns[index].Key),
                (nameof(OmniDataGridColumn<TItem>.Property), columns[index].Property));
        }
    };

    /// <summary>A column bound to <paramref name="property"/> and titled <paramref name="title"/>, then <paramref name="more"/>.</summary>
    internal static void PropertyColumn<TItem>(
        RenderTreeBuilder builder,
        int sequence,
        string property,
        string title,
        params (string Name, object? Value)[] more) =>
        ChartRenderTreeHelpers.Child<OmniDataGridColumn<TItem>>(
            builder,
            sequence,
            [(nameof(OmniDataGridColumn<TItem>.Property), property), (nameof(OmniDataGridColumn<TItem>.Title), title), .. more]);

    /// <summary>A column keyed <paramref name="key"/> that reads <paramref name="value"/>, then <paramref name="more"/>.</summary>
    internal static void ValueColumn<TItem>(
        RenderTreeBuilder builder,
        int sequence,
        string key,
        Func<TItem, object?> value,
        params (string Name, object? Value)[] more) =>
        ChartRenderTreeHelpers.Child<OmniDataGridColumn<TItem>>(
            builder,
            sequence,
            [(nameof(OmniDataGridColumn<TItem>.Key), key), (nameof(OmniDataGridColumn<TItem>.Value), value), .. more]);
}
