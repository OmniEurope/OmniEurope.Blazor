using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-019 lot 2: <see cref="OmniAppearanceState"/> keeps the user's appearance as a preference, and the
/// scope, the appearance window and settings, the app menu and the outermost main area follow it when
/// the host leaves them uncontrolled; a host that controls them keeps them as before and never touches
/// the preference. The browser cache is bUnit's loose module (it keeps nothing), the store is in memory.
/// </summary>
public sealed class AppearanceStateTests : OmniBunitContext
{
    private static readonly DateTimeOffset Later = DateTimeOffset.UtcNow.AddHours(1);

    private readonly MemoryPreferenceStore _store = new();

    private BunitJSModuleInterop PreferencesModule { get; }

    private BunitJSModuleInterop AppearanceModule { get; }

    private BunitJSModuleInterop ThemeModule { get; }

    public AppearanceStateTests()
    {
        Services.Configure<OmniPreferencesOptions>(options => options.SyncInterval = Timeout.InfiniteTimeSpan);
        PreferencesModule = JSInterop.SetupModule(OmniModules.Preferences);
        PreferencesModule.Mode = JSRuntimeMode.Loose;
        PreferencesModule.Setup<string[][]>("entries", _ => true).SetResult([]);
        AppearanceModule = JSInterop.SetupModule(OmniModules.Appearance);
        AppearanceModule.Mode = JSRuntimeMode.Loose;
        ThemeModule = JSInterop.SetupModule(OmniModules.Theme);
        ThemeModule.Mode = JSRuntimeMode.Loose;
    }

    private OmniAppearanceState State => Services.GetRequiredService<OmniAppearanceState>();

    private static string Json(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private string? StoredJson => _store[OmniAppearanceState.PreferenceKey]?.Value;

    /// <summary>The value of a theme's option in the window's list: its index in the catalogue.</summary>
    private static string ThemeOption(string name) =>
        OmniThemePresets.All.Select((theme, index) => (theme, index)).Single(entry => entry.theme.Name == name).index.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void UseStore() => Services.AddSingleton<IOmniPreferenceStore>(_store);

    [Fact]
    public void A_window_change_without_host_binding_repaints_the_scope_and_writes_the_preference()
    {
        UseStore();
        var host = Render<AppearanceStateTestHost>(parameters => parameters.Add(component => component.WindowOpen, true));

        // Every row shows: the state applies each setting.
        Assert.Single(host.FindAll("#host-window select[aria-label='Police']"));
        Assert.Equal(2, host.FindAll("#host-window [role='group'] .omni-badge").Count);
        host.Find("#host-window .omni-appearance-window__mode[title='Sombre']").Click();
        host.Find("#host-window select[aria-label='Thème']").Change(ThemeOption("Givre"));

        host.WaitForAssertion(() => Assert.Equal("dark", host.Find("#host-scope").GetAttribute("data-omni-theme")));
        Assert.Equal("Givre", State.Preset?.Name);
        Assert.Contains(ThemeModule.Invocations["apply"], invocation => invocation.Arguments[3] as string == "dark");
        var stored = JsonDocument.Parse(StoredJson!).RootElement;
        Assert.Equal("Dark", stored.GetProperty("appearance").GetString());
        Assert.Equal("Givre", stored.GetProperty("preset").GetString());
        Assert.Equal(1, stored.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task A_preference_from_the_store_repaints_without_any_action()
    {
        UseStore();
        var host = Render<AppearanceStateTestHost>();
        Assert.Equal("system", host.Find("#host-scope").GetAttribute("data-omni-theme"));

        _store.Put(OmniAppearanceState.PreferenceKey,
            Json(new { version = 1, appearance = "Dark", density = "Compact", fullWidth = true, backdropMotion = false }), Later);
        await Services.GetRequiredService<OmniPreferences>().SyncAsync(Xunit.TestContext.Current.CancellationToken);

        host.WaitForAssertion(() =>
        {
            var scope = host.Find("#host-scope");
            Assert.Equal("dark", scope.GetAttribute("data-omni-theme"));
            Assert.Equal("compact", scope.GetAttribute("data-omni-density"));
            Assert.Equal("off", scope.GetAttribute("data-omni-backdrop-motion"));
            Assert.DoesNotContain("omni-main--content", host.Find("#host-main").ClassList);
        });
    }

    [Fact]
    public async Task A_preference_from_another_tab_repaints_without_any_action()
    {
        const string StorageKey = "omni:pref:local:" + OmniAppearanceState.PreferenceKey;
        var host = Render<AppearanceStateTestHost>();
        var entry = JsonSerializer.Serialize(new PreferenceCacheEntry(Json(new { appearance = "Light", density = "Spacious" }), Later, false));
        PreferencesModule.Setup<string?>("get", invocation => invocation.Arguments[0] as string == StorageKey).SetResult(entry);
        var bridge = ((DotNetObjectReference<PreferenceInteropBridge>)PreferencesModule.Invocations["watch"][0].Arguments[0]!).Value;

        await bridge.OnStored(StorageKey);

        host.WaitForAssertion(() =>
        {
            Assert.Equal("light", host.Find("#host-scope").GetAttribute("data-omni-theme"));
            Assert.Equal("spacious", host.Find("#host-scope").GetAttribute("data-omni-density"));
        });
    }

    [Fact]
    public async Task A_host_that_binds_keeps_its_components_and_never_writes_the_preference()
    {
        UseStore();
        var picked = new List<OmniAppearance>();
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.AppearanceChanged, value => picked.Add(value)));
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .AddChildContent("Contenu"));

        // Only the bound row shows, as before.
        Assert.Empty(window.FindAll("select[aria-label='Thème']"));
        window.Find(".omni-appearance-window__mode[title='Sombre']").Click();
        Assert.Equal([OmniAppearance.Dark], picked);
        Assert.Equal(OmniAppearance.System, State.Appearance);
        Assert.Empty(_store.Saved);

        // A change of the state does not reach a scope the host controls.
        await scope.InvokeAsync(() => State.SetAppearanceAsync(OmniAppearance.Dark));
        Assert.Equal("light", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme"));
        Assert.Empty(AppearanceModule.Invocations);
    }

    [Fact]
    public async Task Text_and_control_sizes_are_laid_on_the_document_root_while_a_scope_follows_the_state()
    {
        var host = Render<AppearanceStateTestHost>(parameters => parameters.Add(component => component.SnapshotKey, "app:theme"));

        // Attached, both levels are written once: level 5 leaves the attribute out.
        Assert.Single(AppearanceModule.Invocations["clearTextSizeLevel"]);
        Assert.Single(AppearanceModule.Invocations["clearControlSizeLevel"]);
        Assert.Equal(10, Assert.Single(ThemeModule.Invocations["snapshot"]).Arguments.Count);

        await host.InvokeAsync(() => State.SetTextSizeLevelAsync(8));
        await host.InvokeAsync(() => State.SetControlSizeLevelAsync(12));

        host.WaitForAssertion(() => Assert.Equal(10, AppearanceModule.Invocations["setControlSizeLevel"].Single().Arguments[0]));
        Assert.Equal(8, AppearanceModule.Invocations["setTextSizeLevel"].Single().Arguments[0]);
        var snapshot = ThemeModule.Invocations["snapshot"][^1].Arguments;
        Assert.Equal((8, 10), ((int)snapshot[8]!, (int)snapshot[9]!));

        // The last scope that follows the state removes both from the root.
        await DisposeComponentsAsync();
        Assert.Equal(2, AppearanceModule.Invocations["clearTextSizeLevel"].Count);
        Assert.Equal(2, AppearanceModule.Invocations["clearControlSizeLevel"].Count);
    }

    [Fact]
    public async Task Full_width_widens_the_outermost_main_area_only()
    {
        var host = Render<AppearanceStateTestHost>();
        Assert.Contains("omni-main--content", host.Find("#host-main").ClassList);

        await host.InvokeAsync(() => State.SetFullWidthAsync(true));

        host.WaitForAssertion(() => Assert.DoesNotContain("omni-main--content", host.Find("#host-main").ClassList));
        Assert.Contains("omni-main--content", host.Find("#host-nested-main").ClassList);
    }

    [Fact]
    public void The_defaults_are_the_package_defaults()
    {
        var state = State;
        var scope = new OmniThemeScope();
        var window = new OmniAppearanceWindow();
        Assert.Equal(
            (scope.Appearance, scope.Density, scope.BackdropMotion, window.TextSizeLevel, window.ControlSizeLevel, window.FullWidth, window.Appearance),
            (state.Appearance, state.Density, state.BackdropMotion, state.TextSizeLevel, state.ControlSizeLevel, state.FullWidth, state.Appearance));
        Assert.Null(state.Preset);
        Assert.Null(state.Palette);
        Assert.Null(state.Font);

        var host = Render<AppearanceStateTestHost>();
        var element = host.Find("#host-scope");
        Assert.Equal(("system", "comfortable", false), (element.GetAttribute("data-omni-theme"), element.GetAttribute("data-omni-density"),
            element.HasAttribute("data-omni-backdrop-motion")));
        Assert.Empty(ThemeModule.Invocations["apply"]);
        Assert.Contains("omni-main--content", host.Find("#host-main").ClassList);
        Assert.Null(StoredJson);
    }

    [Fact]
    public void A_stored_look_is_drawn_from_the_first_frame_with_content()
    {
        var gated = new GatedPreferenceStore(_store);
        Services.AddSingleton<IOmniPreferenceStore>(gated);
        _store.Put(OmniAppearanceState.PreferenceKey, Json(new { appearance = "Dark" }), Later);

        var scope = Render<OmniThemeScope>(parameters => parameters.AddChildContent("<p id='inside'>Contenu</p>"));

        // While the preference is read, the scope draws nothing rather than the defaults.
        Assert.Empty(scope.FindAll(".omni-theme-scope"));
        Assert.Empty(ThemeModule.Invocations);
        gated.Release();

        scope.WaitForAssertion(() => Assert.Equal("dark", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme")));
        Assert.Single(scope.FindAll("#inside"));
    }

    [Fact]
    public async Task The_app_menu_and_the_settings_follow_the_state()
    {
        var host = Render<AppearanceStateTestHost>();
        Assert.EndsWith("Contrôles 5", host.Find(".omni-appearance-settings__summary").TextContent.Trim(), StringComparison.Ordinal);

        // A later render of the menu keeps it following the state.
        host.FindComponent<OmniAppMenu>().Render(parameters => parameters.Add(component => component.Version, "1.8.0"));
        host.Find(".omni-app-menu__trigger").Click();
        // The Mode row is three buttons: one click on dark reaches dark, without passing through light.
        host.Find(".omni-app-menu__mode [role=radio][aria-label=Sombre]").Click();

        host.WaitForAssertion(() => Assert.Equal("dark", host.Find("#host-scope").GetAttribute("data-omni-theme")));
        Assert.Equal(OmniAppearance.Dark, State.Appearance);
        Assert.Equal(OmniAppearance.Dark, host.FindComponent<OmniAppearanceSettings>().Instance.Appearance);

        await host.InvokeAsync(() => State.SetControlSizeLevelAsync(7));
        host.WaitForAssertion(() => Assert.EndsWith("Contrôles 7", host.Find(".omni-appearance-settings__summary").TextContent.Trim(), StringComparison.Ordinal));
    }

    [Fact]
    public void Restore_in_a_window_that_follows_the_state_puts_back_the_look_it_opened_on()
    {
        var host = Render<AppearanceStateTestHost>(parameters => parameters.Add(component => component.WindowOpen, true));
        host.Find("#host-window .omni-appearance-window__mode[title='Sombre']").Click();
        host.Find("#host-window select[aria-label='Thème']").Change(ThemeOption("Galet"));
        Assert.Equal((OmniAppearance.Dark, "Galet"), (State.Appearance, State.Preset?.Name));

        host.Find("#host-window .omni-appearance-window__cancel").Click();

        Assert.Equal(OmniAppearance.System, State.Appearance);
        Assert.Null(State.Preset);
        Assert.Empty(host.FindAll("#host-window"));
    }

    [Fact]
    public async Task The_state_drops_the_palette_and_font_of_a_new_theme_and_brings_levels_within_range()
    {
        UseStore();
        var state = State;
        var givre = OmniThemePresets.All.Single(theme => theme.Name == "Givre");
        await state.SetPresetAsync(givre);
        await state.SetPaletteAsync(OmniThemePalettes.All[3]);
        await state.SetFontAsync(OmniThemeFonts.All[2]);

        await state.SetPresetAsync(givre);
        Assert.NotNull(state.Palette);
        Assert.NotNull(state.Font);
        await state.SetPresetAsync(null);
        Assert.Null(state.Palette);
        Assert.Null(state.Font);

        await state.SetTextSizeLevelAsync(-4);
        await state.SetDensityAsync(OmniDensity.Spacious);
        await state.SetBackdropMotionAsync(false);
        Assert.Equal((1, OmniDensity.Spacious, false), (state.TextSizeLevel, state.Density, state.BackdropMotion));
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = state.SetAppearanceAsync((OmniAppearance)42); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = state.SetDensityAsync((OmniDensity)42); });

        // A value equal to the current one writes nothing.
        var writes = _store.Saved.Count;
        await state.SetDensityAsync(OmniDensity.Spacious);
        Assert.Equal(writes, _store.Saved.Count);
    }

    [Fact]
    public async Task The_preference_is_read_once_and_a_change_made_meanwhile_wins_over_it()
    {
        var gated = new GatedPreferenceStore(_store);
        Services.AddSingleton<IOmniPreferenceStore>(gated);
        _store.Put(OmniAppearanceState.PreferenceKey, Json(new { appearance = "Dark", textSize = 9 }), Later);
        var state = State;

        var first = state.LoadAsync(Xunit.TestContext.Current.CancellationToken);
        var second = state.LoadAsync(Xunit.TestContext.Current.CancellationToken);
        // The change applies at once; its write to the store waits for the read in flight (the store is
        // never called twice at once), so it is awaited after the read is released.
        var change = state.SetAppearanceAsync(OmniAppearance.Light);
        Assert.Equal(OmniAppearance.Light, state.Appearance);
        gated.Release();
        await Task.WhenAll(first, second, change);

        Assert.Equal(OmniAppearance.Light, state.Appearance);
        Assert.Equal(5, state.TextSizeLevel);
        Assert.Equal(1, gated.Reads);
    }

    [Fact]
    public async Task A_preference_that_cannot_be_read_keeps_the_defaults()
    {
        PreferencesModule.Setup<string?>("get", _ => true).SetException(new JSException("lecture refusée"));
        var state = State;

        await state.LoadAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(AppearanceLook.Default, AppearanceLook.Parse(null));
        Assert.Equal(OmniAppearance.System, state.Appearance);
    }

    [Fact]
    public async Task A_lost_circuit_neither_fails_the_read_nor_the_change()
    {
        PreferencesModule.SetupVoid("set", _ => true).SetException(new JSDisconnectedException("circuit perdu"));
        var state = State;
        await state.SetAppearanceAsync(OmniAppearance.Dark);
        Assert.Equal(OmniAppearance.Dark, state.Appearance);

        var other = new OmniAppearanceState(new FailingPreferences().Create(), JSInterop.JSRuntime,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OmniAppearanceState>.Instance);
        await other.LoadAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(OmniAppearance.System, other.Appearance);
    }

    [Fact]
    public async Task A_state_follows_the_store_without_any_component_and_stops_once_disposed()
    {
        UseStore();
        var state = State;
        var preferences = Services.GetRequiredService<OmniPreferences>();
        var token = Xunit.TestContext.Current.CancellationToken;
        await state.LoadAsync(token);

        // Another preference, written here or arriving from the store, leaves the appearance alone.
        await preferences.SetAsync("autre", 1, token);
        _store.Put("autre", "2", Later);
        _store.Put(OmniAppearanceState.PreferenceKey, Json(new { appearance = "Light" }), Later);
        await preferences.SyncAsync(token);
        // The same value again changes nothing.
        await preferences.SyncAsync(token);
        Assert.Equal(OmniAppearance.Light, state.Appearance);

        state.Dispose();

        _store.Put(OmniAppearanceState.PreferenceKey, Json(new { appearance = "Dark" }), Later);
        await Services.GetRequiredService<OmniPreferences>().SyncAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(OmniAppearance.Light, state.Appearance);
    }

    [Fact]
    public async Task A_cancelled_read_keeps_the_defaults_and_an_unexpected_fault_reaches_the_components()
    {
        PreferencesModule.Setup<string?>("get", _ => true).SetException(new OperationCanceledException());
        var state = State;
        await state.LoadAsync(Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(OmniAppearance.System, state.Appearance);

        var faulty = new OmniAppearanceState(new FailingPreferences(new NotSupportedException("panne")).Create(), JSInterop.JSRuntime,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OmniAppearanceState>.Instance);
        await Assert.ThrowsAsync<NotSupportedException>(() => faulty.LoadAsync(Xunit.TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ pas du json")]
    [InlineData("null")]
    [InlineData("{\"appearance\":\"1\",\"density\":\"Huge\",\"preset\":\"Inconnu\",\"textSize\":11,\"controlSize\":0}")]
    public void A_value_that_does_not_read_keeps_the_defaults(string? json)
    {
        Assert.Equal(AppearanceLook.Default, AppearanceLook.Parse(json));
    }

    [Fact]
    public void The_preference_names_its_values_and_reads_back_each_setting_on_its_own()
    {
        var look = AppearanceLook.Parse(
            "{\"appearance\":\"dark\",\"preset\":\"Givre\",\"palette\":\"Inconnue\",\"font\":\"Inter\",\"density\":\"Compact\",\"controlSize\":3,\"fullWidth\":true}");

        Assert.Equal(AppearanceLook.Default with
        {
            Appearance = OmniAppearance.Dark,
            Preset = OmniThemePresets.All.Single(theme => theme.Name == "Givre"),
            Font = OmniThemeFonts.All.Single(font => font.Name == "Inter"),
            Density = OmniDensity.Compact,
            ControlSizeLevel = 3,
            FullWidth = true
        }, look);
        Assert.Equal(new AppearancePreference(1, "Dark", "Givre", null, "Inter", "Compact", true, 5, 3, true), look.ToPreference());
    }

    [Fact]
    public async Task The_document_root_is_left_alone_until_a_scope_attaches_and_kept_while_another_still_follows()
    {
        var root = new AppearanceDocumentRoot(JSInterop.JSRuntime, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        await root.SyncAsync(7, 7);
        await root.DetachAsync();
        Assert.Empty(AppearanceModule.Invocations);

        root.Attach();
        root.Attach();
        await root.SyncAsync(7, 5);
        await root.SyncAsync(7, 5);
        await root.DetachAsync();
        Assert.Single(AppearanceModule.Invocations["setTextSizeLevel"]);
        Assert.Empty(AppearanceModule.Invocations["clearTextSizeLevel"]);

        AppearanceModule.SetupVoid("clearTextSizeLevel", _ => true).SetException(new JSDisconnectedException("circuit perdu"));
        await root.DetachAsync();
        Assert.Single(AppearanceModule.Invocations["clearTextSizeLevel"]);
    }

    /// <summary>Preferences over a browser whose script cannot be reached any more, as after a lost circuit.</summary>
    private sealed class FailingPreferences(Exception? failure = null)
    {
        public OmniPreferences Create()
        {
            var browser = new PreferenceBrowser();
            browser.Failures["get"] = failure ?? new JSDisconnectedException("circuit perdu");
            return new OmniPreferences(browser, null, new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan },
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OmniPreferences>.Instance, TimeProvider.System);
        }
    }

    [Fact]
    public async Task One_window_action_that_changes_several_settings_writes_the_preference_once()
    {
        UseStore();
        var host = Render<AppearanceStateTestHost>(parameters => parameters.Add(component => component.WindowOpen, true));
        host.Find("#host-window .omni-appearance-window__mode[title='Sombre']").Click();
        host.Find("#host-window select[aria-label='Thème']").Change(ThemeOption("Givre"));
        host.WaitForAssertion(() => Assert.Equal(2, _store.Saved.Count));

        host.Find("#host-window .omni-appearance-window__reset").Click();

        // Back to the defaults is two settings changed (mode and theme) and one write, not one per setting.
        host.WaitForAssertion(() => Assert.Equal(OmniAppearance.System, State.Appearance));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(3, _store.Saved.Count);
        Assert.Equal("System", JsonDocument.Parse(StoredJson!).RootElement.GetProperty("appearance").GetString());
    }

    [Fact]
    public async Task A_preference_that_cannot_be_written_keeps_the_look_for_the_session_instead_of_failing_the_click()
    {
        // A scope that is neither empty nor valid makes the preference service refuse every use.
        _store.Scope = "  ";
        UseStore();

        await State.SetAppearanceAsync(OmniAppearance.Dark);

        Assert.Equal(OmniAppearance.Dark, State.Appearance);
        Assert.Null(StoredJson);
    }

    [Fact]
    public async Task A_browser_that_refuses_the_cache_keeps_the_look_for_the_session_instead_of_failing_the_click()
    {
        UseStore();
        PreferencesModule.SetupVoid("set", _ => true).SetException(new JSException("quota"));

        await State.SetAppearanceAsync(OmniAppearance.Dark);

        Assert.Equal(OmniAppearance.Dark, State.Appearance);
    }

    /// <summary>A store whose reads of one key wait until the test releases them.</summary>
    private sealed class GatedPreferenceStore(MemoryPreferenceStore inner) : IOmniPreferenceStore
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Reads;

        public void Release() => _gate.TrySetResult();

        public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Reads);
            // The value is the one held when the read starts: a write made while it waits does not change it.
            var value = await inner.GetAsync(key, cancellationToken);
            await _gate.Task.WaitAsync(cancellationToken);
            return value;
        }

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
            inner.GetChangedSinceAsync(since, cancellationToken);

        public Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => inner.SaveAsync(preference, cancellationToken);

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => inner.GetScopeAsync(cancellationToken);
    }
}
