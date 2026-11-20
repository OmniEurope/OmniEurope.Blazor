using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// An <see cref="OmniMap"/> disposed while its script is still loading must leave nothing behind: the late module is
/// released at once, the script is never attached nor given anything, and the disposal does not hang.
/// </summary>
public sealed class MapScriptLoadTests : OmniBunitContext
{
    [Fact]
    public async Task AMapDisposedWhileItsScriptLoads_NeverAttachesIt_AndReleasesTheLateModule()
    {
        var runtime = new HeldImportRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        var map = Render<OmniMap>();

        var disposal = map.Instance.DisposeAsync().AsTask();
        runtime.Arrive();
        await disposal;

        Assert.Equal(1, runtime.Imports);
        Assert.Empty(runtime.Module.Calls);
        Assert.Equal(1, runtime.Module.Disposals);
    }

    /// <summary>A runtime whose imports are held until <see cref="Arrive"/>, and which answers every other call with a default.</summary>
    private sealed class HeldImportRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> _arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingModule Module { get; } = new();

        public int Imports { get; private set; }

        public void Arrive() => _arrival.SetResult(Module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                Imports++;
                return new(_arrival.Task.ContinueWith(task => (TValue)task.Result, TaskScheduler.Default));
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class RecordingModule : IJSObjectReference
    {
        private int _disposals;

        public List<string> Calls { get; } = [];

        public int Disposals => Volatile.Read(ref _disposals);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposals);
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            lock (Calls)
            {
                Calls.Add(identifier);
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
