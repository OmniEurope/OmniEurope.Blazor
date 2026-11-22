using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A theme scope disposed while one of its two scripts (the theme script, the canvas backdrop script) is still
/// loading releases the module that arrives later and makes no call on it, at each of the sites that load one.
/// </summary>
public sealed class ThemeScopeDisposedWhileLoadingTests : OmniBunitContext
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private const string ThemeScript = "omni-theme.js";
    private const string BlackHoleScript = "omni-black-hole.js";

    private static readonly OmniThemePreset BlackHole = OmniThemePresets.All.Single(theme => theme.Name == "Trou noir");

    private GatedImportRuntime Hold(params string[] held)
    {
        var runtime = new GatedImportRuntime(held);
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    /// <summary>The held <paramref name="script"/> arrives: it is released without a call, and was imported only once.</summary>
    private static async Task ReleasedOnceWithoutACallAsync(GatedImportRuntime runtime, string script)
    {
        runtime.Arrive(script);
        var module = runtime.ModuleOf(script);
        await module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(module.Calls);
        Assert.Equal(1, runtime.Imports(script));
    }

    /// <summary>The scope drawn over a page with what the test sets, once it has asked for <paramref name="script"/>, which is held loading.</summary>
    private async Task<IRenderedComponent<OmniThemeScope>> RenderWhileLoadingAsync(
        GatedImportRuntime runtime, string script, Action<ComponentParameterCollectionBuilder<OmniThemeScope>> configure)
    {
        var scope = Render<OmniThemeScope>(parameters =>
        {
            configure(parameters);
            parameters.AddChildContent("page");
        });
        await runtime.Requested(script).WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        return scope;
    }

    /// <summary>The dark black hole scope, drawn once it has asked for its canvas script, which is held loading.</summary>
    private async Task<(GatedImportRuntime Runtime, IRenderedComponent<OmniThemeScope> Scope)> RenderTheDarkBlackHoleWhileItsScriptLoadsAsync()
    {
        var runtime = Hold(BlackHoleScript);
        var scope = await RenderWhileLoadingAsync(runtime, BlackHoleScript, parameters => parameters
            .Add(component => component.Preset, BlackHole)
            .Add(component => component.Appearance, OmniAppearance.Dark));
        return (runtime, scope);
    }

    [Fact]
    public async Task Scope_DisposedWhileTheThemeScriptLoadsToApplyTokens_ReleasesItAndCallsNothing()
    {
        var runtime = Hold(ThemeScript);
        var scope = await RenderWhileLoadingAsync(runtime, ThemeScript, parameters => parameters
            .Add(component => component.Preset, OmniThemePresets.All[0])
            .Add(component => component.Appearance, OmniAppearance.Dark));

        await scope.Instance.DisposeAsync();

        await ReleasedOnceWithoutACallAsync(runtime, ThemeScript);
    }

    [Fact]
    public async Task Scope_DisposedWhileTheThemeScriptLoadsToFollowTheSystem_ReleasesItAndCallsNothing()
    {
        var runtime = Hold(ThemeScript);
        var scope = await RenderWhileLoadingAsync(runtime, ThemeScript, parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System));

        await scope.Instance.DisposeAsync();

        await ReleasedOnceWithoutACallAsync(runtime, ThemeScript);
    }

    [Fact]
    public async Task Scope_DisposedWhileTheThemeScriptLoadsToWriteTheSnapshot_ReleasesItAndCallsNothing()
    {
        var runtime = Hold(ThemeScript);
        var scope = await RenderWhileLoadingAsync(runtime, ThemeScript, parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.SnapshotKey, "disposal-snapshot"));

        await scope.Instance.DisposeAsync();

        await ReleasedOnceWithoutACallAsync(runtime, ThemeScript);
    }

    [Fact]
    public async Task Scope_DisposedWhileTheCanvasScriptLoads_ReleasesItAndCallsNothing()
    {
        var (runtime, scope) = await RenderTheDarkBlackHoleWhileItsScriptLoadsAsync();

        await scope.Instance.DisposeAsync();

        await ReleasedOnceWithoutACallAsync(runtime, BlackHoleScript);
    }

    [Fact]
    public async Task Scope_RenderedAgainWhileTheCanvasScriptLoadsThenDisposed_DoesNotLoadItAgain()
    {
        var (runtime, scope) = await RenderTheDarkBlackHoleWhileItsScriptLoadsAsync();
        scope.Render(parameters => parameters.Add(component => component.BackdropMotion, false));

        await scope.Instance.DisposeAsync();

        await ReleasedOnceWithoutACallAsync(runtime, BlackHoleScript);
    }

    [Fact]
    public async Task Scope_RenderedAgainAfterItsDisposal_ImportsAndCallsNothing()
    {
        var runtime = Hold();
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, OmniThemePresets.All[0])
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .AddChildContent("page"));
        await scope.Instance.DisposeAsync();
        var imports = runtime.Imports(ThemeScript);
        var calls = runtime.ModuleOf(ThemeScript).Calls.Count;

        scope.Render(parameters => parameters.Add(component => component.Appearance, OmniAppearance.Light));
        await Task.Delay(200, Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(imports, runtime.Imports(ThemeScript));
        Assert.Equal(calls, runtime.ModuleOf(ThemeScript).Calls.Count);
    }

    /// <summary>
    /// A runtime that answers each script path with a module of its own, and holds the imports of the scripts
    /// it was told to hold until <see cref="Arrive"/>.
    /// </summary>
    private sealed class GatedImportRuntime : IJSRuntime
    {
        private readonly Lock _lock = new();
        private readonly Dictionary<string, TaskCompletionSource> _gates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, TaskCompletionSource> _requests = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RecordingModule> _modules = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _imports = new(StringComparer.Ordinal);

        public GatedImportRuntime(IEnumerable<string> held)
        {
            foreach (var script in held)
            {
                _gates[script] = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public Task Requested(string script) => Entry(_requests, script).Task;

        public void Arrive(string script) => _gates[script].SetResult();

        public int Imports(string script)
        {
            lock (_lock)
            {
                return _imports.GetValueOrDefault(script);
            }
        }

        public RecordingModule ModuleOf(string script)
        {
            lock (_lock)
            {
                return _modules.TryGetValue(script, out var module) ? module : _modules[script] = new RecordingModule();
            }
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "import")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            var path = (string)args![0]!;
            var script = path[(path.LastIndexOf('/') + 1)..];
            lock (_lock)
            {
                _imports[script] = _imports.GetValueOrDefault(script) + 1;
            }

            var module = (TValue)(object)ModuleOf(script);
            Entry(_requests, script).TrySetResult();
            return _gates.TryGetValue(script, out var gate)
                ? new ValueTask<TValue>(gate.Task.ContinueWith(_ => module, TaskScheduler.Default))
                : ValueTask.FromResult(module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        private TaskCompletionSource Entry(Dictionary<string, TaskCompletionSource> map, string script)
        {
            lock (_lock)
            {
                return map.TryGetValue(script, out var entry) ? entry : map[script] = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
