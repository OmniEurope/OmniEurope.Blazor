using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders a data grid over in-memory rows, for the grid test classes.</summary>
internal static class DataGridRendering
{
    /// <summary>
    /// Renders a grid of <paramref name="rows"/> keyed by <paramref name="keyOf"/> with the given columns, then
    /// the parameters <paramref name="extra"/> adds.
    /// </summary>
    public static IRenderedComponent<OmniDataGrid<TRow>> RenderKeyedGrid<TRow>(
        this BunitContext context,
        IReadOnlyList<TRow> rows,
        Func<TRow, object> keyOf,
        RenderFragment columns,
        Action<ComponentParameterCollectionBuilder<OmniDataGrid<TRow>>>? extra = null) =>
        context.Render<OmniDataGrid<TRow>>(parameters =>
        {
            parameters
                .Add(component => component.Items, rows)
                .Add(component => component.KeyOf, keyOf)
                .Add(component => component.Columns, columns);
            extra?.Invoke(parameters);
        });

    /// <summary>
    /// Opens a column with its key, title and value at <paramref name="sequence"/> to <paramref name="sequence"/> + 3;
    /// the caller adds what else it needs from <paramref name="sequence"/> + 4, then closes it.
    /// </summary>
    public static void OpenValueColumn<TRow>(this RenderTreeBuilder builder, int sequence, string key, string title, Func<TRow, object?> value)
    {
        builder.OpenComponent<OmniDataGridColumn<TRow>>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniDataGridColumn<TRow>.Key), key);
        builder.AddComponentParameter(sequence + 2, nameof(OmniDataGridColumn<TRow>.Title), title);
        builder.AddComponentParameter(sequence + 3, nameof(OmniDataGridColumn<TRow>.Value), value);
    }

    /// <summary>
    /// One column per entry of <paramref name="columns"/>, bound to its property and given its value of the column
    /// parameter named <paramref name="parameter"/>, each column ten sequence numbers after the previous.
    /// </summary>
    public static RenderFragment PropertyColumns<TRow, TValue>(string parameter, params (string Property, TValue Value)[] columns) => builder =>
    {
        for (var index = 0; index < columns.Length; index++)
        {
            builder.OpenComponent<OmniDataGridColumn<TRow>>(index * 10);
            builder.AddComponentParameter(index * 10 + 1, nameof(OmniDataGridColumn<TRow>.Property), columns[index].Property);
            builder.AddComponentParameter(index * 10 + 2, parameter, columns[index].Value);
            builder.CloseComponent();
        }
    };

    /// <summary>A single column keyed <paramref name="key"/> showing <paramref name="value"/>, without a title.</summary>
    public static RenderFragment KeyedColumn<TRow>(string key, Func<TRow, object?> value) => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<TRow>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<TRow>.Key), key);
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<TRow>.Value), value);
        builder.CloseComponent();
    };
}
