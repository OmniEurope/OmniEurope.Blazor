using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The outermost <see cref="OmniThemeScope"/> keeps the boot copy of what it painted beside the user's appearance
/// preference, in the browser cache of <see cref="OmniPreferences"/> (<c>attach</c> of omni-preferences.js), for
/// omni-boot.js to replay before Blazor starts, and takes over from the copy that script laid in the call that first
/// paints its tokens. A nested scope does neither. The browser cache is bUnit's loose module.
/// </summary>
public sealed class ThemeScopeBootCopyTests : OmniBunitContext
{
    private const string StorageKey = "omni:pref:local:" + OmniAppearanceState.PreferenceKey;

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private BunitJSModuleInterop PreferencesModule { get; }

    private BunitJSModuleInterop ThemeModule { get; }

    private BunitJSModuleInterop AppearanceModule { get; }

    public ThemeScopeBootCopyTests()
    {
        PreferencesModule = JSInterop.SetupModule(OmniModules.Preferences);
        PreferencesModule.Mode = JSRuntimeMode.Loose;
        PreferencesModule.Setup<string[][]>("entries", _ => true).SetResult([]);
        var stored = JsonSerializer.Serialize(new { version = 1, appearance = "Dark", preset = "Halo", density = "Compact", textSize = 7, controlSize = 3 }, Web);
        PreferencesModule.Setup<string?>("get", invocation => invocation.Arguments[0] as string == StorageKey)
            .SetResult(JsonSerializer.Serialize(new PreferenceCacheEntry(stored, DateTimeOffset.UtcNow, false)));
        AppearanceModule = JSInterop.SetupModule(OmniModules.Appearance);
        AppearanceModule.Mode = JSRuntimeMode.Loose;
        ThemeModule = JSInterop.SetupModule(OmniModules.Theme);
        ThemeModule.Mode = JSRuntimeMode.Loose;
    }

    private OmniAppearanceState State => Services.GetRequiredService<OmniAppearanceState>();

    private IReadOnlyList<JSRuntimeInvocation> Attached => PreferencesModule.Invocations["attach"];

    [Fact]
    public void The_outermost_scope_that_follows_the_state_keeps_the_copy_of_what_it_painted_beside_the_preference()
    {
        var scope = Render<OmniThemeScope>(parameters => parameters.AddChildContent("Contenu"));

        scope.WaitForAssertion(() => Assert.Single(Attached));
        var attach = Attached[0];
        Assert.Equal(StorageKey, attach.Arguments[0]);
        var copy = JsonDocument.Parse((string)attach.Arguments[1]!).RootElement;
        Assert.Equal(OmniThemeScope.SnapshotVersion, copy.GetProperty("version").GetInt32());
        Assert.Equal(State.PreferenceValue, copy.GetProperty("value").GetString());
        Assert.Equal("dark", copy.GetProperty("appearance").GetString());
        Assert.Equal("compact", copy.GetProperty("density").GetString());
        Assert.True(copy.GetProperty("backdropMotion").GetBoolean());
        Assert.Equal(7, copy.GetProperty("textSize").GetInt32());
        Assert.Equal(3, copy.GetProperty("controlSize").GetInt32());

        // The halves are the very tokens the scope painted, and its first paint took over the boot copy.
        var apply = Assert.Single(ThemeModule.Invocations["apply"]);
        var dark = (IReadOnlyDictionary<string, string>)apply.Arguments[2]!;
        Assert.Equal(dark["--omni-color-surface"], copy.GetProperty("dark").GetProperty("--omni-color-surface").GetString());
        Assert.Equal(dark.Count, copy.GetProperty("dark").EnumerateObject().Count());
        Assert.False((bool)apply.Arguments[4]!, "no host snapshot");
        Assert.True((bool)apply.Arguments[5]!, "the outermost scope takes over the package copy");
    }

    [Fact]
    public async Task The_copy_is_kept_again_only_when_the_look_changes()
    {
        var scope = Render<OmniThemeScope>(parameters => parameters.AddChildContent("Contenu"));
        scope.WaitForAssertion(() => Assert.Single(Attached));

        scope.Render();
        Assert.Single(Attached);

        await scope.InvokeAsync(() => State.SetDensityAsync(OmniDensity.Spacious));
        scope.WaitForAssertion(() => Assert.Equal(2, Attached.Count));
        Assert.Equal("spacious", JsonDocument.Parse((string)Attached[1].Arguments[1]!).RootElement.GetProperty("density").GetString());
    }

    [Fact]
    public void A_nested_scope_keeps_no_copy_and_takes_over_nothing()
    {
        var halo = OmniThemePresets.All.Single(entry => entry.Name == "Halo");
        var outer = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniThemeScope>(0);
                builder.AddComponentParameter(1, nameof(OmniThemeScope.Appearance), OmniAppearance.Dark);
                builder.AddComponentParameter(2, nameof(OmniThemeScope.Preset), halo);
                builder.AddComponentParameter(3, nameof(OmniThemeScope.ChildContent), (RenderFragment)(inner => inner.AddContent(0, "Dedans")));
                builder.CloseComponent();
            }));

        var apply = Assert.Single(ThemeModule.Invocations["apply"]);
        Assert.False((bool)apply.Arguments[5]!, "only the outermost scope takes over the package copy");
        Assert.Empty(Attached);
        Assert.Equal(2, outer.FindAll(".omni-theme-scope").Count);
    }

    private IReadOnlyList<JSRuntimeInvocation> TextSizeWrites => AppearanceModule.Invocations["setTextSizeLevel"];

    [Fact]
    public void A_scope_that_stays_alive_goes_on_to_attach_the_root_text_size_once_the_copy_is_kept()
    {
        var scope = Render<OmniThemeScope>(parameters => parameters.AddChildContent("Contenu"));

        scope.WaitForAssertion(() => Assert.Single(TextSizeWrites));
        Assert.Single(Attached);
    }

    [Fact]
    public async Task A_scope_disposed_while_its_boot_copy_is_being_kept_stops_there_and_leaves_the_document_root_alone()
    {
        var holding = PreferencesModule.SetupVoid("attach", _ => true);
        var scope = Render<OmniThemeScope>(parameters => parameters.AddChildContent("Contenu"));
        scope.WaitForAssertion(() => Assert.Single(Attached));

        await scope.Instance.DisposeAsync();
        holding.SetVoidResult();
        await Task.Delay(200, Xunit.TestContext.Current.CancellationToken);

        Assert.Empty(TextSizeWrites);
        Assert.Empty(AppearanceModule.Invocations["clearTextSizeLevel"]);
    }

    /// <summary>
    /// omni-boot.js reads what .NET writes, a contract the compiler cannot see: the cache key format, the preference
    /// key, the class of the scope it paints, the member that holds the copy and the hand-over entry points.
    /// </summary>
    [Fact]
    public void The_boot_script_reads_the_cache_the_package_writes()
    {
        var wwwroot = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot");
        var boot = File.ReadAllText(Path.Combine(wwwroot, "omni-boot.js"));
        var preferences = File.ReadAllText(Path.Combine(wwwroot, "omni-preferences.js"));

        Assert.Contains($"var preferencePrefix = '{PreferenceCache.KeyPrefix}';", boot, StringComparison.Ordinal);
        Assert.Contains($"var appearanceSuffix = ':{OmniAppearanceState.PreferenceKey}';", boot, StringComparison.Ordinal);
        Assert.Contains("'.omni-theme-scope'", boot, StringComparison.Ordinal);
        Assert.Contains("entry.Boot", boot, StringComparison.Ordinal);
        Assert.Contains("best.entry.Value", boot, StringComparison.Ordinal);
        Assert.Contains("const bootMember = 'Boot';", preferences, StringComparison.Ordinal);
        Assert.Contains("omniBoot.handOverTheme(true)", preferences, StringComparison.Ordinal);
        Assert.Contains("settleTheme: settleTheme", boot, StringComparison.Ordinal);
        Assert.Contains("OmniBoot?.settleTheme?.()", File.ReadAllText(Path.Combine(wwwroot, "omniInterop.js")), StringComparison.Ordinal);
        Assert.Contains("class=\"@Css(\"omni-theme-scope\"", File.ReadAllText(Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "Components", "Layout", "OmniThemeScope.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_outermost_scope_its_host_binds_keeps_the_copy_once_the_preference_was_read_and_never_before()
    {
        var halo = OmniThemePresets.All.Single(entry => entry.Name == "Halo");
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.Preset, halo)
            .AddChildContent("Contenu"));
        Assert.Empty(Attached);
        Assert.True((bool)Assert.Single(ThemeModule.Invocations["apply"]).Arguments[5]!);

        await State.LoadAsync(Xunit.TestContext.Current.CancellationToken);
        scope.Render(parameters => parameters.Add(component => component.Density, OmniDensity.Compact));

        scope.WaitForAssertion(() => Assert.Single(Attached));
        var copy = JsonDocument.Parse((string)Attached[0].Arguments[1]!).RootElement;
        Assert.Equal(State.PreferenceValue, copy.GetProperty("value").GetString());
        Assert.Equal("dark", copy.GetProperty("appearance").GetString());
    }
}
