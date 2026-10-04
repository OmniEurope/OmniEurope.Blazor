using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The scope carries the mode it draws (<c>data-omni-theme-resolved</c>), and the scope that keeps a
/// snapshot writes what it painted for <c>omni-boot.js</c> to replay before Blazor starts, then takes
/// over from that boot copy in the call that paints it.
/// </summary>
public sealed class ThemeScopeBootSnapshotTests : OmniBunitContext
{
    private const string ThemeModule = Internal.OmniModules.Theme;

    [Theory]
    [InlineData(OmniAppearance.Light, "light")]
    [InlineData(OmniAppearance.Dark, "dark")]
    public void A_light_or_dark_scope_renders_the_resolved_mode_without_any_script(OmniAppearance appearance, string expected)
    {
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, appearance)
            .AddChildContent("Contenu"));

        var element = scope.Find(".omni-theme-scope");
        Assert.Equal(expected, element.GetAttribute("data-omni-theme"));
        Assert.Equal(expected, element.GetAttribute("data-omni-theme-resolved"));
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import");
    }

    [Fact]
    public void A_system_scope_leaves_the_resolved_mode_to_the_script_that_follows_the_system()
    {
        var module = JSInterop.SetupModule(ThemeModule);

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System)
            .AddChildContent("Contenu"));

        var element = scope.Find(".omni-theme-scope");
        Assert.Equal("system", element.GetAttribute("data-omni-theme"));
        Assert.False(element.HasAttribute("data-omni-theme-resolved"));
        Assert.Single(module.Invocations["followSystem"]);
        Assert.Empty(module.Invocations["apply"]);

        // A render that keeps the system mode does not subscribe again.
        scope.Render(parameters => parameters.Add(component => component.Density, OmniDensity.Compact));
        Assert.Single(module.Invocations["followSystem"]);
    }

    [Fact]
    public void Leaving_the_system_mode_stops_following_and_the_markup_carries_the_mode_again()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System)
            .AddChildContent("Contenu"));

        scope.Render(parameters => parameters.Add(component => component.Appearance, OmniAppearance.Dark));

        Assert.Single(module.Invocations["unfollowSystem"]);
        Assert.Equal("dark", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme-resolved"));

        scope.Render(parameters => parameters.Add(component => component.Appearance, OmniAppearance.System));
        Assert.Equal(2, module.Invocations["followSystem"].Count);
        Assert.False(scope.Find(".omni-theme-scope").HasAttribute("data-omni-theme-resolved"));
    }

    [Fact]
    public void A_dark_only_theme_resolves_to_dark_whatever_the_mode_asked()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var darkOnly = OmniThemePresets.All.First(preset => preset.DarkOnly);

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System)
            .Add(component => component.Preset, darkOnly)
            .AddChildContent("Contenu"));

        Assert.Equal("dark", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme-resolved"));
        Assert.Empty(module.Invocations["followSystem"]);
    }

    [Fact]
    public void A_scope_without_a_snapshot_key_writes_nothing_and_takes_over_nothing()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var preset = OmniThemePresets.All.Single(entry => entry.Name == "Halo");

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.Preset, preset)
            .AddChildContent("Contenu"));

        Assert.Empty(module.Invocations["snapshot"]);
        Assert.False(scope.Find(".omni-theme-scope").HasAttribute("data-omni-theme-snapshot"));
        Assert.False((bool)Assert.Single(module.Invocations["apply"]).Arguments[4]!);
    }

    [Fact]
    public void The_snapshot_holds_the_attributes_and_both_halves_the_scope_paints()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var halo = OmniThemePresets.All.Single(entry => entry.Name == "Halo");
        var braise = OmniThemePalettes.All.Single(entry => entry.Name == "Braise");
        var font = OmniThemeFonts.All[^1];

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System)
            .Add(component => component.Preset, halo)
            .Add(component => component.Palette, braise)
            .Add(component => component.Font, font)
            .Add(component => component.Density, OmniDensity.Spacious)
            .Add(component => component.BackdropMotion, false)
            .Add(component => component.SnapshotKey, "site:theme")
            .AddChildContent("Contenu"));

        Assert.Equal("site:theme", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme-snapshot"));
        var apply = Assert.Single(module.Invocations["apply"]);
        var snapshot = Assert.Single(module.Invocations["snapshot"]).Arguments;
        Assert.Equal("site:theme", snapshot[1]);
        Assert.Equal(OmniThemeScope.SnapshotVersion, snapshot[2]);
        Assert.Equal("system", snapshot[3]);
        Assert.Equal("spacious", snapshot[4]);
        Assert.False((bool)snapshot[5]!);

        // The halves are the very tokens the scope painted: theme shape, palette colours and font.
        var light = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(snapshot[6]);
        var dark = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(snapshot[7]);
        Assert.Equal((IReadOnlyDictionary<string, string>)apply.Arguments[1]!, light);
        Assert.Equal((IReadOnlyDictionary<string, string>)apply.Arguments[2]!, dark);
        Assert.Equal(braise.Dark["--omni-color-accent"], dark["--omni-color-accent"]);
        Assert.Equal(font.Family, light["--omni-font-family"]);

        // The scope that keeps the snapshot takes over from the boot copy in the call that paints it.
        Assert.True((bool)apply.Arguments[4]!);
    }

    [Fact]
    public void The_snapshot_is_written_again_only_when_the_look_changes()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.SnapshotKey, "site:theme")
            .AddChildContent("Contenu"));

        var first = Assert.Single(module.Invocations["snapshot"]).Arguments;
        Assert.Equal("light", first[3]);
        Assert.Null(first[6]);
        Assert.Null(first[7]);

        scope.Render(parameters => parameters.Add(component => component.Appearance, OmniAppearance.Light));
        Assert.Single(module.Invocations["snapshot"]);

        scope.Render(parameters => parameters.Add(component => component.Density, OmniDensity.Compact));
        Assert.Equal(2, module.Invocations["snapshot"].Count);
        Assert.Equal("compact", module.Invocations["snapshot"][1].Arguments[4]);

        scope.Render(parameters => parameters.Add(component => component.Palette, OmniThemePalettes.All[1]));
        Assert.Equal(3, module.Invocations["snapshot"].Count);
        Assert.Same(OmniThemePalettes.All[1].Light, module.Invocations["snapshot"][2].Arguments[6]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_snapshot_key_is_no_key(string key)
    {
        var module = JSInterop.SetupModule(ThemeModule);

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.SnapshotKey, key)
            .AddChildContent("Contenu"));

        Assert.Empty(module.Invocations["snapshot"]);
        Assert.False(scope.Find(".omni-theme-scope").HasAttribute("data-omni-theme-snapshot"));
    }

    /// <summary>
    /// The boot script and the scope share a contract the compiler cannot see: the snapshot version, the
    /// attribute that names the scope, the hand-over entry point and the root marker the stylesheet paints.
    /// </summary>
    [Fact]
    public void The_boot_script_reads_the_contract_the_scope_writes()
    {
        var wwwroot = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot");
        var boot = File.ReadAllText(Path.Combine(wwwroot, "omni-boot.js"));
        var theme = File.ReadAllText(Path.Combine(wwwroot, "omni-theme.js"));

        var version = Regex.Match(boot, @"var snapshotVersion = (\d+);", RegexOptions.None, TimeSpan.FromSeconds(1));
        Assert.True(version.Success);
        Assert.Equal(OmniThemeScope.SnapshotVersion, int.Parse(version.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("data.themeSnapshotKey", boot, StringComparison.Ordinal);
        Assert.Contains("'[data-omni-theme-snapshot]'", boot, StringComparison.Ordinal);
        Assert.Contains("handOverTheme: handOverTheme", boot, StringComparison.Ordinal);
        Assert.Contains("boot.handOverTheme()", theme, StringComparison.Ordinal);
        foreach (var attribute in new[] { "data-omni-theme", "data-omni-theme-resolved", "data-omni-density", "data-omni-backdrop-motion", "data-omni-theme-boot" })
        {
            Assert.Contains($"setOnRoot('{attribute}'", boot, StringComparison.Ordinal);
        }

        Assert.Contains(":root[data-omni-theme-boot]", StylesheetSource.Read(), StringComparison.Ordinal);
    }
}
