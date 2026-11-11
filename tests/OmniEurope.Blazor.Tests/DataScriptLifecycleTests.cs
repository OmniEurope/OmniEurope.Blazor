using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Data components that load a script module: a component disposed while the module is on its way
/// or while a call is in flight releases what it holds, attaches nothing on a removed element, and
/// never lets a disposed-module exception escape its render.
/// </summary>
public sealed class DataScriptLifecycleTests : OmniBunitContext
{
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    private GatedRuntime UseRuntime(GatedRuntime? runtime = null)
    {
        runtime ??= new GatedRuntime();
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    private async Task SettleAsync()
    {
        // Lets the released continuations run, then surfaces any exception the renderer caught.
        await Task.Delay(150, Xunit.TestContext.Current.CancellationToken);
        var probe = Render<OmniTree<int>>();
        await probe.InvokeAsync(() => { });
    }

    private IRenderedComponent<OmniDataList<int>> RenderList() => Render<OmniDataList<int>>(parameters => parameters
        .Add(component => component.Items, Enumerable.Range(0, 200).ToArray())
        .Add(component => component.Virtualize, true)
        .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));

    [Fact]
    public async Task DataList_DisposedDuringTheImport_ReleasesTheModuleAndAttachesNothing()
    {
        var runtime = UseRuntime(new GatedRuntime { HoldImport = true });
        var list = RenderList();

        await list.Instance.DisposeAsync();
        runtime.ReleaseImport();

        await runtime.Module.Disposal.Task.WaitAsync(Limit, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attachList", runtime.Module.Calls);
    }

    [Fact]
    public async Task DataList_OnALostCircuit_DoesNotLetTheDisconnectionEscape()
    {
        var runtime = UseRuntime();
        runtime.Module.Failures["attachList"] = new JSDisconnectedException("perdu");

        RenderList();

        await runtime.WaitForCallAsync("attachList");
        await SettleAsync();
        Assert.Contains("attachList", runtime.Module.Calls);
    }

    [Fact]
    public async Task DataList_DisposedWhileTheViewportIsRead_DoesNotThrow()
    {
        var runtime = UseRuntime();
        runtime.Module.Gates["syncList"] = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var list = RenderList();

        await list.Instance.DisposeAsync();
        runtime.Module.Gates["syncList"].SetResult(null);

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task LogViewer_DisposedWhileTheViewportIsRead_DoesNotThrow()
    {
        var runtime = UseRuntime();
        runtime.Module.Gates["sync"] = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine("ligne", OmniLogLevel.Information)])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        await viewer.Instance.DisposeAsync();
        runtime.Module.Gates["sync"].SetResult(null);

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task Spreadsheet_DisposedWhileTheEditorTakesTheFocus_DoesNotCallTheReleasedScript()
    {
        var runtime = UseRuntime();
        var sheet = Render<OmniSpreadsheet>(parameters => parameters.Add(component => component.Value, OmniSpreadsheetData.Create(2, 2)));
        await runtime.WaitForCallAsync("attach");
        runtime.Module.Gates["focusEditor"] = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        sheet.Find("[data-omni-sheet-grid]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "F2" });
        await runtime.WaitForCallAsync("focusEditor");
        await sheet.Instance.DisposeAsync();
        runtime.Module.Gates["focusEditor"].SetResult(null);

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task Tree_DisposedDuringTheImport_ReleasesTheModuleAndAttachesNothing()
    {
        var runtime = UseRuntime(new GatedRuntime { HoldImport = true });
        var tree = Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));

        await tree.Instance.DisposeAsync();
        runtime.ReleaseImport();

        await runtime.Module.Disposal.Task.WaitAsync(Limit, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attachTreeDrag", runtime.Module.Calls);
    }

    [Fact]
    public async Task Tree_RenderedTwiceBeforeTheImportEnds_ImportsOnce()
    {
        var runtime = UseRuntime(new GatedRuntime { HoldImport = true });
        var tree = Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));
        tree.Render(parameters => parameters.Add(component => component.Label, "Arbre"));

        runtime.ReleaseImport();
        await runtime.WaitForCallAsync("attachTreeDrag");

        Assert.Equal(1, runtime.Imports);
        Assert.Single(runtime.Module.Calls, "attachTreeDrag");
    }

    [Fact]
    public async Task DataList_RenderedAfterItsDisposal_AttachesNothing()
    {
        var runtime = UseRuntime();
        var list = RenderList();
        await runtime.WaitForCallAsync("attachList");

        await list.Instance.DisposeAsync();
        list.Render(parameters => parameters.Add(component => component.Items, Enumerable.Range(0, 300).ToArray()));
        await list.Instance.OnViewportChangedAsync(10d, 100d);

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task DataList_WhoseModuleIsReleasedUnderACall_DoesNotThrowOnceDisposed()
    {
        var runtime = UseRuntime();
        runtime.Module.Gates["syncList"] = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var list = RenderList();
        await runtime.WaitForCallAsync("syncList");

        await list.Instance.DisposeAsync();
        runtime.Module.Gates["syncList"].SetException(new ObjectDisposedException("module"));

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task DataList_WhoseImportFailed_ImportsAgainOnTheNextRender()
    {
        var runtime = UseRuntime();
        runtime.ImportFailures.Enqueue(new InvalidOperationException("import failed"));

        var list = Render<OmniDataList<int>>(parameters => parameters
            .Add(component => component.Items, Enumerable.Range(0, 200).ToArray())
            .Add(component => component.Virtualize, true)
            .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attachList", runtime.Module.Calls);

        // The renderer reports the failed import on the next render; the render after it imports again.
        Assert.NotNull(Record.Exception(() => list.Render(parameters => parameters.Add(component => component.Items, Enumerable.Range(0, 201).ToArray()))));
        list.Render(parameters => parameters.Add(component => component.Items, Enumerable.Range(0, 202).ToArray()));

        await runtime.WaitForCallAsync("attachList");
    }

    [Fact]
    public async Task LogViewer_DisposedWhileTheLayoutIsApplied_DoesNotCallTheReleasedScript()
    {
        var runtime = UseRuntime();
        runtime.Module.Gates["applyLayout"] = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine("ligne", OmniLogLevel.Information)])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));
        await runtime.WaitForCallAsync("applyLayout");

        await viewer.Instance.DisposeAsync();
        runtime.Module.Gates["applyLayout"].SetResult(null);

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task Spreadsheet_DisposedWhileTheGridTakesTheFocus_DoesNotCallTheReleasedScript()
    {
        var runtime = UseRuntime();
        var sheet = Render<OmniSpreadsheet>(parameters => parameters.Add(component => component.Value, OmniSpreadsheetData.Create(2, 2)));
        await runtime.WaitForCallAsync("attach");
        sheet.Find("[data-omni-sheet-grid]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "F2" });
        await runtime.WaitForCallAsync("focusEditor");
        var focus = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.Gates["Blazor._internal.domWrapper.focus"] = focus;

        sheet.Find("[data-omni-sheet-editor]").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        // The grid takes the focus back on the renderer: the sheet is disposed while that call waits.
        for (var wait = 0; wait < 500 && !runtime.DirectCalls.Contains("Blazor._internal.domWrapper.focus"); wait++)
        {
            await Task.Delay(10, Xunit.TestContext.Current.CancellationToken);
        }

        await sheet.Instance.DisposeAsync();
        focus.SetResult(null);

        await SettleAsync();
        Assert.Contains("Blazor._internal.domWrapper.focus", runtime.DirectCalls);
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task Tree_WhoseImportIsLost_DoesNotLetTheDisconnectionEscape()
    {
        var runtime = UseRuntime();
        runtime.ImportFailures.Enqueue(new JSDisconnectedException("perdu"));

        Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));

        await SettleAsync();
        Assert.DoesNotContain("attachTreeDrag", runtime.Module.Calls);
    }

    [Fact]
    public async Task Tree_WhoseImportFailed_ImportsAgainOnTheNextRender()
    {
        var runtime = UseRuntime();
        runtime.ImportFailures.Enqueue(new InvalidOperationException("import failed"));
        var tree = Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attachTreeDrag", runtime.Module.Calls);

        // The renderer reports the failed import on the next render; the render after it imports again.
        Assert.NotNull(Record.Exception(() => tree.Render(parameters => parameters.Add(component => component.Label, "Arbre"))));
        tree.Render(parameters => parameters.Add(component => component.Label, "Autre"));

        await runtime.WaitForCallAsync("attachTreeDrag");
    }

    [Fact]
    public async Task Tree_WhoseAttachIsLost_DoesNotLetTheDisconnectionEscape()
    {
        var runtime = UseRuntime();
        runtime.Module.Failures["attachTreeDrag"] = new JSDisconnectedException("perdu");

        Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));

        await SettleAsync();
        Assert.Contains("attachTreeDrag", runtime.Module.Calls);
    }

    [Fact]
    public async Task Tree_WhoseModuleIsReleasedUnderTheAttach_DoesNotThrowOnceDisposed()
    {
        var runtime = UseRuntime();
        runtime.Module.Gates["attachTreeDrag"] = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tree = Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));
        await runtime.WaitForCallAsync("attachTreeDrag");

        await tree.Instance.DisposeAsync();
        runtime.Module.Gates["attachTreeDrag"].SetException(new ObjectDisposedException("module"));

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }

    [Fact]
    public async Task Tree_RenderedAfterItsDisposal_AttachesNothing()
    {
        var runtime = UseRuntime();
        var tree = Render<OmniTree<int>>(parameters => parameters.Add(component => component.AllowDragDrop, true));
        await runtime.WaitForCallAsync("attachTreeDrag");

        await tree.Instance.DisposeAsync();
        tree.Render(parameters => parameters.Add(component => component.Label, "Arbre"));

        await SettleAsync();
        Assert.Empty(runtime.Module.CallsAfterDisposal);
    }


    /// <summary>A runtime whose import can be held, with a module that refuses calls once released.</summary>
    private sealed class GatedRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> _import = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _imports;

        internal GatedModule Module { get; } = new();

        internal bool HoldImport { get; init; }

        internal Queue<Exception> ImportFailures { get; } = new();

        internal Dictionary<string, TaskCompletionSource<object?>> Gates { get; } = new(StringComparer.Ordinal);

        internal List<string> DirectCalls { get; } = [];

        internal int Imports => _imports;

        internal void ReleaseImport() => _import.SetResult(Module);

        internal Task WaitForCallAsync(string name) => Module.Called(name).WaitAsync(Limit);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier != "import")
            {
                DirectCalls.Add(identifier);
                if (Gates.TryGetValue(identifier, out var gate))
                {
                    await gate.Task;
                }

                return default!;
            }

            if (ImportFailures.TryDequeue(out var failure))
            {
                await Task.Yield();
                throw failure;
            }

            Interlocked.Increment(ref _imports);
            return HoldImport ? (TValue)await _import.Task : (TValue)(object)Module;
        }
    }

    private sealed class GatedModule : IJSObjectReference
    {
        private bool _disposed;

        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource> _signals = new(StringComparer.Ordinal);

        internal List<string> Calls { get; } = [];

        internal List<string> CallsAfterDisposal { get; } = [];

        internal Task Called(string name) => Signal(name).Task;

        internal Dictionary<string, Exception> Failures { get; } = new(StringComparer.Ordinal);

        internal Dictionary<string, TaskCompletionSource<object?>> Gates { get; } = new(StringComparer.Ordinal);

        internal TaskCompletionSource Disposal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private TaskCompletionSource Signal(string name) => _signals.GetOrAdd(name, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            Disposal.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (_disposed)
            {
                CallsAfterDisposal.Add(identifier);
                throw new ObjectDisposedException(nameof(GatedModule));
            }

            Calls.Add(identifier);
            Signal(identifier).TrySetResult();
            if (Failures.TryGetValue(identifier, out var failure))
            {
                throw failure;
            }

            if (Gates.TryGetValue(identifier, out var gate))
            {
                await gate.Task;
            }

            return default!;
        }
    }
}
