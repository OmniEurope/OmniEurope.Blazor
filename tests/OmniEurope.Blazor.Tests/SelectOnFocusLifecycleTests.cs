using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The lifecycle of <see cref="OmniSelectOnFocus"/> used on its own: nothing is imported while the parameter was
/// never on, a field disposed meanwhile attaches nothing, and a lost circuit while the script is released is ignored.
/// (<see cref="SelectOnFocusTests"/> drives it through the four text fields.)
/// </summary>
public sealed class SelectOnFocusLifecycleTests
{
    private static readonly ElementReference Field = new("field-1");

    [Fact]
    public async Task AFieldWhoseParameterWasNeverOn_ImportsNothing_WhenSyncedOff()
    {
        var runtime = new CountingRuntime();
        var selectOnFocus = new OmniSelectOnFocus(runtime);

        await selectOnFocus.SyncAsync(false, Field);

        Assert.Equal(0, runtime.Imports);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task AFieldDisposedWithoutEverImportingTheScript_ReleasesNothing_AndAttachesNothingAfterwards()
    {
        var runtime = new CountingRuntime();
        var selectOnFocus = new OmniSelectOnFocus(runtime);

        await selectOnFocus.DisposeAsync();
        await selectOnFocus.SyncAsync(true, Field);

        Assert.Equal(0, runtime.Imports);
        Assert.Empty(runtime.Module.Calls);
        Assert.False(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task AFieldDisposedAfterAttaching_DetachesAndReleasesTheModule_AndAttachesNothingAfterwards()
    {
        var runtime = new CountingRuntime();
        var selectOnFocus = new OmniSelectOnFocus(runtime);
        await selectOnFocus.SyncAsync(true, Field);
        Assert.Equal(["attach"], runtime.Module.Calls);

        await selectOnFocus.DisposeAsync();
        await selectOnFocus.SyncAsync(true, new ElementReference("field-2"));

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
        Assert.Equal(1, runtime.Imports);
    }

    [Fact]
    public async Task ALostCircuit_WhileDetachingOnDisposal_DoesNotLetTheDisconnectionEscape_AndTheNextDisposalReleasesTheModule()
    {
        var runtime = new CountingRuntime();
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("lost");
        var selectOnFocus = new OmniSelectOnFocus(runtime);
        await selectOnFocus.SyncAsync(true, Field);

        await selectOnFocus.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.False(runtime.Module.Disposal.Task.IsCompleted);

        // The lock was released: a second disposal runs, with nothing left to detach.
        await selectOnFocus.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public async Task ALostCircuit_WhileReleasingTheModule_DoesNotLetTheDisconnectionEscape()
    {
        var runtime = new CountingRuntime(new RecordingModule { DisposeFailure = new JSDisconnectedException("lost") });
        var selectOnFocus = new OmniSelectOnFocus(runtime);
        await selectOnFocus.SyncAsync(true, Field);

        await selectOnFocus.DisposeAsync();

        Assert.Equal(["attach", "detach"], runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    private sealed class CountingRuntime(RecordingModule? module = null) : IJSRuntime
    {
        private int _imports;

        public RecordingModule Module { get; } = module ?? new();

        public int Imports => _imports;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                Interlocked.Increment(ref _imports);
                return ValueTask.FromResult((TValue)(object)Module);
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
