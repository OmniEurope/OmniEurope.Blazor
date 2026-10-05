using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A JavaScript runtime a test drives by hand, for the timings bUnit's interop cannot produce: an import
/// that is still pending, or a module whose release fails because the circuit is gone. Every import
/// gets <see cref="Module"/>, or waits for <see cref="PendingImport"/> when <see cref="HoldImports"/> is
/// set; every other call answers its default value.
/// </summary>
internal sealed class ManualJSRuntime : IJSRuntime
{
    public RecordingModule Module { get; init; } = new();

    public bool HoldImports { get; init; }

    /// <summary>Thrown by every import instead of answering, as an import on a lost circuit is.</summary>
    public Exception? ImportFailure { get; init; }

    public TaskCompletionSource<IJSObjectReference> PendingImport { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier != "import")
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        if (ImportFailure is not null)
        {
            return ValueTask.FromException<TValue>(ImportFailure);
        }

        return HoldImports
            ? new ValueTask<TValue>(PendingImport.Task.ContinueWith(task => (TValue)task.Result, TaskScheduler.Default))
            : ValueTask.FromResult((TValue)(object)Module);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
}

/// <summary>A module that records its calls and its release, and can fail the release or a call.</summary>
internal sealed class RecordingModule : IJSObjectReference
{
    public List<string> Calls { get; } = [];

    public TaskCompletionSource Disposal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Exception? DisposeFailure { get; init; }

    /// <summary>Calls that fail, by function name, as a call on a lost circuit does.</summary>
    public Dictionary<string, Exception> CallFailures { get; } = new(StringComparer.Ordinal);

    /// <summary>Answers of calls, by function name; any other call answers its default value.</summary>
    public Dictionary<string, object?> Answers { get; } = new(StringComparer.Ordinal);

    public ValueTask DisposeAsync()
    {
        Disposal.TrySetResult();
        return DisposeFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(DisposeFailure);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add(identifier);
        if (CallFailures.TryGetValue(identifier, out var failure))
        {
            return ValueTask.FromException<TValue>(failure);
        }

        return ValueTask.FromResult(Answers.TryGetValue(identifier, out var answer) ? (TValue)answer! : default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<TValue>(identifier, args);
}
