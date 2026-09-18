namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The copy button behaviour shared by the code components: puts a text on the clipboard through
/// <c>omniInterop.js</c>, remembers whether it worked for as long as the feedback lasts, then asks
/// its owner to redraw the idle button.
/// </summary>
internal sealed class OmniClipboardCopy(IJSRuntime javaScript, Func<Task> redraw) : IAsyncDisposable
{
    private const string InteropModulePath = "./_content/OmniEurope.Blazor/omniInterop.js";

    private IJSObjectReference? _module;
    private CancellationTokenSource? _feedback;
    private bool _disposed;

    /// <summary>The outcome of the last copy while its feedback lasts; null when idle.</summary>
    internal bool? Result { get; private set; }

    /// <summary>Copies <paramref name="text"/>; true when the clipboard accepted it.</summary>
    internal async Task<bool> CopyAsync(string text, TimeSpan feedbackDuration, TimeProvider timeProvider)
    {
        bool copied;
        try
        {
            _module ??= await javaScript.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
            copied = await _module.InvokeAsync<bool>("copyText", text);
        }
        catch (JSException)
        {
            copied = false;
        }

        Result = copied;
        _ = ClearLaterAsync(feedbackDuration, timeProvider);
        return copied;
    }

    /// <summary>Goes back to idle once the feedback has been seen; a new copy restarts the wait.</summary>
    private async Task ClearLaterAsync(TimeSpan duration, TimeProvider timeProvider)
    {
        _feedback?.Cancel();
        _feedback?.Dispose();
        _feedback = new CancellationTokenSource();
        var token = _feedback.Token;
        try
        {
            await Task.Delay(duration, timeProvider, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!_disposed)
        {
            Result = null;
            await redraw();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _feedback?.Cancel();
        _feedback?.Dispose();
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
