using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDataList beyond its first render: the viewport the script reports, the host's own states, a
/// loader replaced or removed, a reload without loader, and a script released on a lost circuit.
/// </summary>
public sealed class DataListBehaviourTests : OmniBunitContext
{
    private static readonly RenderFragment<int> ItemTemplate = item => builder => builder.AddContent(0, $"Élément {item}");

    private static readonly int[] Many = [.. Enumerable.Range(0, 1_000)];

    private IRenderedComponent<OmniDataList<int>> RenderList(Action<ComponentParameterCollectionBuilder<OmniDataList<int>>> parameters) =>
        Render<OmniDataList<int>>(builder =>
        {
            builder.Add(list => list.ItemTemplate, ItemTemplate);
            parameters(builder);
        });

    [Fact]
    public void Snapshot_MovesTheWindow_ToTheScrollAndMeasuredHeightsTheScriptReports()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<GridViewportSnapshot?>("syncList", _ => true).SetResult(new GridViewportSnapshot
        {
            ScrollTop = 3_200,
            ViewportHeight = 320,
            Rows = [new GridRowMeasurement { Index = 0, Height = 64 }]
        });

        var list = RenderList(parameters => parameters.Add(list => list.Items, Many).Add(list => list.Virtualize, true));

        list.WaitForAssertion(() => Assert.DoesNotContain(">Élément 0<", list.Markup, StringComparison.Ordinal));
        Assert.Contains(">Élément 100<", list.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void SnapshotThatChangesNothing_DoesNotRenderAgain()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<GridViewportSnapshot?>("syncList", _ => true).SetResult(new GridViewportSnapshot());

        var list = RenderList(parameters => parameters.Add(list => list.Items, Many).Add(list => list.Virtualize, true));

        Assert.Equal(1, list.RenderCount);
    }

    [Fact]
    public async Task ViewportReportThatKeepsTheWindow_DoesNotRenderAgain()
    {
        JSInterop.SetupModule(OmniModules.Grid).Mode = JSRuntimeMode.Loose;
        var list = RenderList(parameters => parameters.Add(list => list.Items, Many).Add(list => list.Virtualize, true));
        await list.InvokeAsync(() => list.Instance.OnViewportChangedAsync(0, 320));
        var renders = list.RenderCount;

        await list.InvokeAsync(() => list.Instance.OnViewportChangedAsync(1, 320));

        Assert.Equal(renders, list.RenderCount);
    }

    [Fact]
    public void HostStates_ReplaceTheDefaultTexts()
    {
        var empty = RenderList(parameters => parameters
            .Add(list => list.Items, [])
            .Add(list => list.EmptyContent, builder => builder.AddContent(0, "Rien ici")));
        Assert.Contains("Rien ici", empty.Markup, StringComparison.Ordinal);

        var pending = new TaskCompletionSource<IReadOnlyList<int>>();
        var loading = RenderList(parameters => parameters
            .Add(list => list.Load, _ => pending.Task)
            .Add(list => list.LoadingContent, builder => builder.AddContent(0, "Patience")));
        Assert.Contains("Patience", loading.Markup, StringComparison.Ordinal);

        var failed = RenderList(parameters => parameters
            .Add(list => list.Load, _ => Task.FromException<IReadOnlyList<int>>(new InvalidOperationException("panne")))
            .Add(list => list.ErrorContent, error => builder => builder.AddContent(0, $"Erreur : {error.Message}")));
        Assert.Contains("Erreur : panne", failed.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReloadWithoutLoader_ShowsTheItems()
    {
        var list = RenderList(parameters => parameters.Add(list => list.Items, [1, 2]));

        await list.InvokeAsync(list.Instance.ReloadAsync);

        Assert.Equal(2, list.FindAll(".omni-data-list__item").Count);
    }

    [Fact]
    public void ReplacedLoader_CancelsTheLoadInProgress_AndRemovingItGoesBackToTheItems()
    {
        var cancelled = false;
        var pending = new TaskCompletionSource<IReadOnlyList<int>>();
        var list = RenderList(parameters => parameters
            .Add(list => list.Items, [7])
            .Add(list => list.Load, async token =>
            {
                using var registration = token.Register(() => { cancelled = true; pending.TrySetCanceled(token); });
                return await pending.Task;
            }));

        list.Render(parameters => parameters.Add(component => component.Load, _ => Task.FromResult<IReadOnlyList<int>>([1, 2, 3])));
        Assert.True(cancelled);
        Assert.Equal(3, list.FindAll(".omni-data-list__item").Count);

        list.Render(parameters => parameters.Add(component => component.Load, null));
        Assert.Single(list.FindAll(".omni-data-list__item"));
    }

    [Fact]
    public void SameLoader_IsNotRunAgain_WhileLoadingNorAfterAFailure()
    {
        var calls = 0;
        var pending = new TaskCompletionSource<IReadOnlyList<int>>();
        Func<CancellationToken, Task<IReadOnlyList<int>>> slow = _ => { calls++; return pending.Task; };
        var loading = RenderList(parameters => parameters.Add(list => list.Load, slow));
        loading.Render(parameters => parameters.Add(component => component.Load, slow));
        Assert.Equal(1, calls);

        var failures = 0;
        Func<CancellationToken, Task<IReadOnlyList<int>>> failing = _ => { failures++; return Task.FromException<IReadOnlyList<int>>(new InvalidOperationException("panne")); };
        var failed = RenderList(parameters => parameters.Add(list => list.Load, failing));
        failed.Render(parameters => parameters.Add(component => component.Load, failing));
        Assert.Equal(1, failures);
    }

    [Fact]
    public void LeavingVirtualization_DetachesTheScript_EvenOnALostCircuit()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["detachList"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var list = RenderList(parameters => parameters.Add(list => list.Items, Many).Add(list => list.Virtualize, true));

        list.Render(parameters => parameters.Add(component => component.Virtualize, false));

        Assert.Contains("detachList", runtime.Module.Calls);
        Assert.Equal(Many.Length, list.FindAll(".omni-data-list__item").Count);
    }

    [Fact]
    public async Task Dispose_ReleasesTheScript_EvenOnALostCircuit()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        Services.AddSingleton<IJSRuntime>(runtime);
        var list = RenderList(parameters => parameters.Add(list => list.Items, Many).Add(list => list.Virtualize, true));

        await list.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
        Assert.Contains("detachList", runtime.Module.Calls);
    }
}
