using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Binds an input's <c>Value</c>, <c>ValueExpression</c> and, when given, <c>ValueChanged</c> to a
/// test's local variable in one call, as <c>@bind-Value</c> would.
/// </summary>
internal static class BoundValueParameters
{
    /// <summary>Passes the current value of <paramref name="value"/>, the expression itself and the optional change handler.</summary>
    public static ComponentParameterCollectionBuilder<TComponent> BindValue<TComponent, TValue>(
        this ComponentParameterCollectionBuilder<TComponent> parameters,
        Expression<Func<TValue>> value,
        Action<TValue>? changed = null)
        where TComponent : InputBase<TValue>
    {
        parameters.Add(component => component.Value, value.Compile()());
        if (changed is not null)
        {
            parameters.Add(component => component.ValueChanged, changed);
        }

        return parameters.Add(component => component.ValueExpression, value);
    }
}
