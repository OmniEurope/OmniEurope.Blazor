using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The helpers that import a script module and then wire listeners: when their owner leaves the page
/// while the import is still on its way, the module that arrives late is released and nothing is
/// wired on it, so no listener and no .NET reference outlive the component.
/// </summary>
public sealed class LateJsAcquisitionTests
{
    [Fact]
    public async Task PickerPopup_ReleasedDuringTheImport_AttachesNothingAndReleasesTheModule()
    {
        var runtime = new PendingImportRuntime();
        var popup = new PickerPopup(runtime, _ => Task.CompletedTask);
        popup.Open(".omni-picker__item");

        var rendering = popup.AfterRenderAsync(default, default, default);
        popup.Release();
        runtime.Complete();
        await rendering;

        Assert.DoesNotContain("attachPicker", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposed);
    }

    [Fact]
    public async Task DisclosureDismissal_DisposedDuringTheImport_ConfiguresNothingAndReleasesTheModule()
    {
        var runtime = new PendingImportRuntime();
        var dismissal = new OmniDisclosureDismissal(runtime);

        var applying = dismissal.ApplyAsync(new ElementReference("details-1"), closeOnOutsideClick: true, closeOnItem: false);
        await dismissal.DisposeAsync();
        runtime.Complete();
        await applying;

        Assert.DoesNotContain("configureDisclosure", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposed);
    }

    [Fact]
    public async Task ClipboardCopy_DisposedDuringTheImport_CopiesNothingAndReleasesTheModule()
    {
        var runtime = new PendingImportRuntime();
        var copy = new OmniClipboardCopy(runtime, () => Task.CompletedTask);

        var copying = copy.CopyAsync("texte", TimeSpan.FromSeconds(1), TimeProvider.System);
        await copy.DisposeAsync();
        runtime.Complete();

        Assert.False(await copying);
        Assert.DoesNotContain("copyText", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposed);
    }

    /// <summary>A runtime whose module import stays pending until the test completes it.</summary>
    private sealed class PendingImportRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> _import = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal RecordingModule Module { get; } = new();

        internal void Complete() => _import.SetResult(Module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Assert.Equal("import", identifier);
            return (TValue)await _import.Task;
        }
    }

    private sealed class RecordingModule : IJSObjectReference
    {
        internal List<string> Calls { get; } = [];

        internal bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
