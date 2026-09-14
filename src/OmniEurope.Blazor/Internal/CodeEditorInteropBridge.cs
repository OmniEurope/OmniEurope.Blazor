using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-code-editor.js</c> can call. Kept apart from the component so that
/// the callable surface is internal rather than public API.
/// </summary>
internal sealed class CodeEditorInteropBridge(OmniCodeEditor owner)
{
    [JSInvokable]
    public Task OnCodeChanged(string value) => owner.DispatchAsync(() => owner.HandleCodeChangedAsync(value));

    [JSInvokable]
    public Task OnCursorChanged(int line, int column) => owner.DispatchAsync(() => owner.HandleCursorChanged(line, column));

    [JSInvokable]
    public Task OnLinkActivated(string name, string target, int line) =>
        owner.DispatchAsync(() => owner.HandleLinkActivatedAsync(new OmniCodeEditorLinkEventArgs(name, target, line)));
}
