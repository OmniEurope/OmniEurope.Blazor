namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-ink-canvas.js</c> can call: the finished strokes and the two keys of the
/// canvas. Kept apart from the component so that the callable surface is internal rather than public API.
/// Every value crossing is a primitive or an array of numbers, so nothing depends on reflection-based
/// serialization of a library type.
/// </summary>
internal sealed class InkCanvasInteropBridge(Func<string, double, double[], Task> onStroke, Func<Task> onUndo, Func<Task> onClear)
{
    [JSInvokable]
    public Task OnStroke(string color, double width, double[] points) => onStroke(color, width, points);

    [JSInvokable]
    public Task OnUndo() => onUndo();

    [JSInvokable]
    public Task OnClear() => onClear();
}
