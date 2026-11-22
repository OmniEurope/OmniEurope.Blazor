namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-code-editor.js</c> can call. Kept apart from the component so that
/// the callable surface is internal rather than public API. It receives only the callbacks it forwards
/// to, so it does not depend on the component.
/// </summary>
internal sealed class CodeEditorInteropBridge(
    Func<string, Task> codeChanged,
    Func<int, int, Task> cursorChanged,
    Func<string, string, int, Task> linkActivated)
{
    [JSInvokable]
    public Task OnCodeChanged(string value) => codeChanged(value);

    [JSInvokable]
    public Task OnCursorChanged(int line, int column) => cursorChanged(line, column);

    [JSInvokable]
    public Task OnLinkActivated(string name, string target, int line) => linkActivated(name, target, line);
}
