using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Wraps a handler so the renderer does not re-render its component after the event: the renderer routes
/// an event to the <see cref="IHandleEvent"/> that owns the delegate, and this receiver only invokes the
/// handler, where a component would also call <c>StateHasChanged</c>. For a handler that only records
/// state the markup does not show (keystroke by keystroke, a render would be pure waste, and on a server
/// circuit a diff sent back for nothing).
/// </summary>
internal sealed class NonRenderingHandler<TArgs> : IHandleEvent
{
    private readonly Action<TArgs> _handler;

    private NonRenderingHandler(Action<TArgs> handler) => _handler = handler;

    /// <summary>A delegate that runs <paramref name="handler"/> without re-rendering its component.</summary>
    /// <param name="handler">The handler to run.</param>
    /// <returns>The delegate to bind to the event.</returns>
    internal static Action<TArgs> For(Action<TArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return new NonRenderingHandler<TArgs>(handler).Invoke;
    }

    /// <inheritdoc />
    public Task HandleEventAsync(EventCallbackWorkItem item, object? arg) => item.InvokeAsync(arg);

    private void Invoke(TArgs args) => _handler(args);
}
