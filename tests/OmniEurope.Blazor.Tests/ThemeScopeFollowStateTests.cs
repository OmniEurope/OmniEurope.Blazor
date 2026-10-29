using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A bare <see cref="OmniThemeScope"/> (no appearance parameter) follows the persisted appearance: what it
/// draws before the preference is read, what a prerender with a slow store really does, and the failures of
/// the components that follow the state, which are observed and logged instead of lost.
/// </summary>
public sealed class ThemeScopeFollowStateTests : OmniBunitContext
{
    private static readonly DateTimeOffset Later = DateTimeOffset.UtcNow.AddHours(1);

    private BunitJSModuleInterop PreferencesModule { get; }

    private BunitJSModuleInterop AppearanceModule { get; }

    public ThemeScopeFollowStateTests()
    {
        Services.Configure<OmniPreferencesOptions>(options => options.SyncInterval = Timeout.InfiniteTimeSpan);
        PreferencesModule = JSInterop.SetupModule(OmniModules.Preferences);
        PreferencesModule.Mode = JSRuntimeMode.Loose;
        PreferencesModule.Setup<string[][]>("entries", _ => true).SetResult([]);
        AppearanceModule = JSInterop.SetupModule(OmniModules.Appearance);
        AppearanceModule.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(OmniModules.Theme).Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment Inside => builder => builder.AddMarkupContent(0, "<p id='inside'>Contenu</p>");

    [Fact]
    public void A_bare_scope_without_a_store_renders_at_once_when_the_browser_answers_at_once()
    {
        var scope = Render<OmniThemeScope>(parameters => parameters.Add(component => component.ChildContent, Inside));

        Assert.Equal("system", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme"));
        Assert.Single(scope.FindAll("#inside"));
    }

    [Fact]
    public void A_bare_scope_without_a_store_draws_nothing_until_the_browser_read_answers()
    {
        var read = PreferencesModule.Setup<string?>("get", _ => true);

        var scope = Render<OmniThemeScope>(parameters => parameters.Add(component => component.ChildContent, Inside));
        Assert.Empty(scope.FindAll("#inside"));

        read.SetResult(null);
        scope.WaitForAssertion(() => Assert.Single(scope.FindAll("#inside")));
    }

    [Fact]
    public void A_scope_given_any_appearance_parameter_renders_at_once_and_never_reads_the_preference()
    {
        var read = PreferencesModule.Setup<string?>("get", _ => true);

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Density, OmniDensity.Comfortable)
            .Add(component => component.ChildContent, Inside));

        Assert.Single(scope.FindAll("#inside"));
        Assert.Empty(PreferencesModule.Invocations["get"]);
        read.SetResult(null);
    }

    [Fact]
    public async Task A_prerender_of_a_bare_scope_waits_for_a_slow_store_then_draws_the_stored_look()
    {
        // A static prerender: the browser script cannot be reached, the preferences read the store directly.
        var browser = new PreferenceBrowser { ImportFailure = new InvalidOperationException("statically rendered") };
        var store = new GatedStore();
        store.Inner.Put(OmniAppearanceState.PreferenceKey, """{"version":1,"appearance":"Dark"}""", Later);
        Services.AddScoped(_ => new OmniPreferences(browser, store, new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan },
            NullLogger<OmniPreferences>.Instance, TimeProvider.System));
        Services.AddScoped(provider => new OmniAppearanceState(provider.GetRequiredService<OmniPreferences>(), browser,
            NullLogger<OmniAppearanceState>.Instance));
        await using var renderer = new HtmlRenderer(Services, Services.GetRequiredService<ILoggerFactory>());
        var token = Xunit.TestContext.Current.CancellationToken;

        var rendering = renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(OmniThemeScope.ChildContent)] = Inside });
            return (await renderer.RenderComponentAsync<OmniThemeScope>(parameters)).ToHtmlString();
        });

        // The prerender is held for as long as the store takes to answer: it has no deadline of its own.
        await store.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
        await Task.Delay(200, token);
        Assert.False(rendering.IsCompleted);

        store.Release();
        var html = await rendering.WaitAsync(TimeSpan.FromSeconds(5), token);
        Assert.Contains("data-omni-theme=\"dark\"", html, StringComparison.Ordinal);
        Assert.Contains("<p id='inside'>Contenu</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_prerender_of_a_bare_scope_without_a_store_draws_the_defaults_at_once()
    {
        var browser = new PreferenceBrowser { ImportFailure = new InvalidOperationException("statically rendered") };
        Services.AddScoped(_ => new OmniPreferences(browser, null, new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan },
            NullLogger<OmniPreferences>.Instance, TimeProvider.System));
        Services.AddScoped(provider => new OmniAppearanceState(provider.GetRequiredService<OmniPreferences>(), browser,
            NullLogger<OmniAppearanceState>.Instance));
        await using var renderer = new HtmlRenderer(Services, Services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(OmniThemeScope.ChildContent)] = Inside });
            return (await renderer.RenderComponentAsync<OmniThemeScope>(parameters)).ToHtmlString();
        }).WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

        Assert.Contains("data-omni-theme=\"system\"", html, StringComparison.Ordinal);
        Assert.Contains("<p id='inside'>Contenu</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_follower_whose_refresh_fails_logs_it_and_ignores_a_disposed_component()
    {
        var logger = new RecordingLogger();
        var state = new OmniAppearanceState(Services.GetRequiredService<OmniPreferences>(), JSInterop.JSRuntime, logger);
        Exception failure = new InvalidOperationException("refresh failed");
        using var follower = AppearanceFollower.Start(state, hostControls: false, () => Task.FromException(failure))!;

        // Raised by the state, the refresh is observed: its failure is logged, not lost.
        await state.SetAppearanceAsync(OmniAppearance.Dark);
        var logged = Assert.Single(logger.Entries);
        Assert.Equal((LogLevel.Error, failure), logged);

        failure = new ObjectDisposedException("scope");
        await follower.RefreshAsync();
        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task The_document_root_finishes_detaching_when_the_script_refuses()
    {
        var logger = new RecordingLogger();
        var root = new AppearanceDocumentRoot(JSInterop.JSRuntime, logger);
        root.Attach();
        await root.SyncAsync(7, 7);
        var refusal = new JSException("clear refused");
        AppearanceModule.SetupVoid("clearTextSizeLevel", _ => true).SetException(refusal);

        await root.DetachAsync();

        Assert.Equal((LogLevel.Warning, refusal), Assert.Single(logger.Entries));
    }

    private sealed class RecordingLogger : ILogger<OmniAppearanceState>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, exception));
    }

    /// <summary>A store whose reads wait for <see cref="Release"/>, as a slow database does.</summary>
    private sealed class GatedStore : IOmniPreferenceStore
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MemoryPreferenceStore Inner { get; } = new();

        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Release() => _gate.TrySetResult();

        public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            ReadStarted.TrySetResult();
            await _gate.Task.WaitAsync(cancellationToken);
            return await Inner.GetAsync(key, cancellationToken);
        }

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            Inner.GetChangedSinceAsync(since, cancellationToken);

        public Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Inner.SaveAsync(preference, cancellationToken);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => Inner.GetScopeAsync(cancellationToken);
    }
}
