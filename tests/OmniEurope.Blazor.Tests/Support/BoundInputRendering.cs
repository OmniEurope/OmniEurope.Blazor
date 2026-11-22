using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Renders an input component bound as a form binds it: <see cref="InputBase{TValue}.Value"/> and its
/// <see cref="InputBase{TValue}.ValueExpression"/>, plus the parameters the test adds. The test still says
/// everything that makes its scenario; only the binding every input needs is written once.
/// </summary>
internal static class BoundInputRendering
{
    /// <summary>The input bound to <paramref name="value"/>, its expression reading that value.</summary>
    public static IRenderedComponent<TComponent> RenderBound<TComponent, TValue>(
        this BunitContext context, TValue value, Action<ComponentParameterCollectionBuilder<TComponent>>? configure = null)
        where TComponent : InputBase<TValue> =>
        context.RenderBound<TComponent, TValue>(value, () => value, configure);

    /// <summary>The input showing <paramref name="value"/>, bound to the member <paramref name="valueExpression"/> reads.</summary>
    public static IRenderedComponent<TComponent> RenderBound<TComponent, TValue>(
        this BunitContext context, TValue value, Expression<Func<TValue>> valueExpression, Action<ComponentParameterCollectionBuilder<TComponent>>? configure = null)
        where TComponent : InputBase<TValue> =>
        context.Render<TComponent>(parameters =>
        {
            configure?.Invoke(parameters);
            parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, valueExpression);
        });

    /// <summary>The input bound to <paramref name="value"/>, every change it raises recorded in <paramref name="changes"/>.</summary>
    public static IRenderedComponent<TComponent> RenderBoundRecording<TComponent, TValue>(
        this BunitContext context, TValue value, List<TValue> changes, Action<ComponentParameterCollectionBuilder<TComponent>>? configure = null)
        where TComponent : InputBase<TValue> =>
        context.RenderBound<TComponent, TValue>(value, parameters =>
        {
            configure?.Invoke(parameters);
            parameters.Add(component => component.ValueChanged, next => changes.Add(next));
        });
}
