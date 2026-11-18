namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-ink-canvas.js</c> can call: the finished strokes, the strokes the eraser touched, the
/// two keys of the canvas and the theme colour the script resolved when it changes. Kept apart from the component so
/// that the callable surface is internal rather than public API.
/// Every value crossing is a primitive or an array of numbers, so nothing depends on reflection-based
/// serialization of a library type.
/// </summary>
internal sealed class InkCanvasInteropBridge(Func<string, double, double[], Task> onStroke, Func<Task> onUndo, Func<Task> onClear, Func<string, Task> onThemeColor, Func<int[], int, Task> onErase)
{
    [JSInvokable]
    public Task OnStroke(string color, double width, double[] points) => onStroke(color, width, points);

    [JSInvokable]
    public Task OnUndo() => onUndo();

    [JSInvokable]
    public Task OnClear() => onClear();

    [JSInvokable]
    public Task OnThemeColor(string color) => onThemeColor(color);

    /// <summary>The eraser touched the strokes at these indices of a drawing the script holds <paramref name="count"/> strokes of.</summary>
    [JSInvokable]
    public Task OnErase(int[] indices, int count) => onErase(indices, count);
}
