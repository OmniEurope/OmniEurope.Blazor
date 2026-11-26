using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A script runtime written by a test: it answers the call without a cancellation token, and the call
/// with one is handed to it unchanged.
/// </summary>
internal abstract class ForwardingJSRuntime : IJSRuntime
{
    public abstract ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}

/// <summary>
/// A script runtime written by a test that reads the cancellation token: the call without one is handed to it
/// with <see cref="CancellationToken.None"/>.
/// </summary>
internal abstract class CancellableJSRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public abstract ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args);
}

/// <summary>
/// A script module written by a test, answering its calls as <see cref="ForwardingJSRuntime"/> does.
/// </summary>
internal abstract class ForwardingJSObjectReference : ForwardingJSRuntime, IJSObjectReference
{
    public abstract ValueTask DisposeAsync();
}

/// <summary>
/// A runtime whose imports are all held until <see cref="Arrive"/>, which counts the imports asked and
/// answers every other call with a default.
/// </summary>
internal sealed class HeldImportRuntime<TModule>(TModule module) : ForwardingJSRuntime
    where TModule : IJSObjectReference
{
    private readonly TaskCompletionSource<IJSObjectReference> _arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TModule Module { get; } = module;

    public int Imports { get; private set; }

    public void Arrive() => _arrival.SetResult(Module);

    public override ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier == "import")
        {
            Imports++;
            return new(_arrival.Task.ContinueWith(task => (TValue)task.Result, TaskScheduler.Default));
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}

/// <summary>A module that records its calls and counts its releases, from any thread, and answers every call with a default.</summary>
internal sealed class DisposalCountingModule : ForwardingJSObjectReference
{
    private int _disposals;

    public List<string> Calls { get; } = [];

    public int Disposals => Volatile.Read(ref _disposals);

    public override ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposals);
        return ValueTask.CompletedTask;
    }

    public override ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        lock (Calls)
        {
            Calls.Add(identifier);
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}
