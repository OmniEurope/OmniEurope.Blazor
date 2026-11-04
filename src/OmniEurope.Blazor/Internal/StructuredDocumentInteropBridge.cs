namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-structured-document.js</c> can call: the text of a run the reader edited. Kept apart
/// from the component so that the callable surface is internal rather than public API.
/// </summary>
internal sealed class StructuredDocumentInteropBridge(Func<string, string, Task> onRunText)
{
    /// <summary>The text of a run now differs from the one .NET was last told.</summary>
    /// <param name="runId">The <c>data-omni-run</c> identifier of the run.</param>
    /// <param name="text">The plain text now in the run.</param>
    /// <returns>A task that completes once the component has taken the text.</returns>
    [JSInvokable]
    public Task OnRunText(string runId, string text) => onRunText(runId, text);
}
