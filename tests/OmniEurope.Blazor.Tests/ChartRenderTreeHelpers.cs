using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Builders shared by the chart suites that compose a chart from its parts by hand.</summary>
internal static class ChartRenderTreeHelpers
{
    /// <summary>One point per value, at the value's index on the X axis.</summary>
    public static OmniChartPoint[] Points(params double[] values) => [.. values.Select((value, index) => new OmniChartPoint(index, value))];

    /// <summary>Writes a child component with its parameters, numbering them after <paramref name="sequence"/>.</summary>
    public static void Child<TComponent>(RenderTreeBuilder builder, int sequence, params (string Name, object? Value)[] parameters)
        where TComponent : IComponent
    {
        builder.OpenComponent<TComponent>(sequence);
        for (var index = 0; index < parameters.Length; index++)
        {
            builder.AddComponentParameter(sequence + index + 1, parameters[index].Name, parameters[index].Value);
        }

        builder.CloseComponent();
    }
}
