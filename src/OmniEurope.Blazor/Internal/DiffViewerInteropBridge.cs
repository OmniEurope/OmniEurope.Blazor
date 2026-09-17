namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object the diff side of <c>omni-code-editor.js</c> can call, kept apart from the
/// component so that the callable surface is internal rather than public API.
/// </summary>
internal sealed class DiffViewerInteropBridge(Func<string, Task> onModifiedChanged)
{
    [JSInvokable]
    public Task OnModifiedChanged(string value) => onModifiedChanged(value);
}