using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Layout and theming at their edges: scripts that load after their component went or on a lost circuit,
/// a column span out of range, a header logo, a settings tile shared by two controls, a stack that stops
/// scrolling, the canvas field of a theme repainted or stopped, a font without a theme, and the appearance
/// settings and window resets.
/// </summary>
public sealed class LayoutThemingEdgeTests : OmniBunitContext
{
    [Fact]
    public async Task BootSplashGoneWhileItsScriptAnswers_ReleasesIt_AndALostCircuitIsQuiet()
    {
        var runtime = new ManualJSRuntime { HoldImports = true, Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.Answers["hideBootSplash"] = true;
        Services.AddSingleton<IJSRuntime>(runtime);
        var hidden = new List<bool>();
        var splash = Render<OmniBootSplash>(parameters => parameters.Add(component => component.OnHidden, value => hidden.Add(value)));

        await splash.Instance.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(["hideBootSplash"], runtime.Module.Calls);
        Assert.Empty(hidden);
    }

    [Fact]
    public async Task BootSplash_OnALostCircuit_IsQuiet_AndReleasesQuietly()
    {
        var runtime = new ManualJSRuntime { Module = new RecordingModule { DisposeFailure = new JSDisconnectedException("perdu") } };
        runtime.Module.Answers["hideBootSplash"] = true;
        Services.AddSingleton<IJSRuntime>(runtime);
        var hidden = new List<bool>();
        var splash = Render<OmniBootSplash>(parameters => parameters.Add(component => component.OnHidden, value => hidden.Add(value)));
        Assert.Equal([true], hidden);
        await splash.Instance.DisposeAsync();

        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }

    [Fact]
    public void BootSplash_LostBeforeItsSplashWasHidden_IsQuiet()
    {
        var lost = new ManualJSRuntime();
        lost.Module.CallFailures["hideBootSplash"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(lost);

        Render<OmniBootSplash>();

        Assert.Equal(["hideBootSplash"], lost.Module.Calls);
    }

    [Fact]
    public void Column_RefusesASpanAboveTwelve() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniColumn>(parameters => parameters.Add(component => component.LargeSpan, 13)));

    [Fact]
    public void Header_DrawsItsLogo()
    {
        var header = Render<OmniHeader>(parameters => parameters.Add(component => component.Brand, "OE").Add(component => component.BrandLogo, "/logo.svg"));

        Assert.NotEmpty(header.FindAll("img"));
    }

    [Fact]
    public async Task ScriptsOfMainStackAndFieldset_OnALostCircuit_AreQuiet()
    {
        var runtime = new ManualJSRuntime();
        foreach (var call in new[] { "watchScrolling", "unwatchScrolling", "configureScrollOverflow", "disposeFieldsetToggle" })
        {
            runtime.Module.CallFailures[call] = new JSDisconnectedException("perdu");
        }

        Services.AddSingleton<IJSRuntime>(runtime);
        var main = Render<OmniMain>(parameters => parameters.Add(component => component.Scrollable, true).Add(component => component.AutoHideScrollbar, true).AddChildContent("page"));
        var stack = Render<OmniStack>(parameters => parameters.Add(component => component.Overflow, OmniStackOverflow.Scroll).AddChildContent("a"));
        var fieldset = Render<OmniFieldset>(parameters => parameters.Add(component => component.Collapsible, true).Add(component => component.Legend, "Options"));

        await main.Instance.DisposeAsync();
        await fieldset.Instance.DisposeAsync();
        await stack.Instance.DisposeAsync();

        Assert.Contains("watchScrolling", runtime.Module.Calls);
        Assert.Contains("configureScrollOverflow", runtime.Module.Calls);
    }

    [Fact]
    public void Stack_ThatStopsScrolling_ReleasesItsChevrons()
    {
        var module = JSInterop.SetupModule(OmniModules.Focus);
        module.Mode = JSRuntimeMode.Loose;
        var stack = Render<OmniStack>(parameters => parameters.Add(component => component.Overflow, OmniStackOverflow.Scroll).AddChildContent("a"));

        stack.Render(parameters => parameters.Add(component => component.Overflow, OmniStackOverflow.None));

        Assert.Single(module.Invocations["disposeScrollOverflow"]);
    }

    [Fact]
    public void SettingsTile_SharedByTwoControls_NamesTheFirst_AndIgnoresTheOtherLeaving()
    {
        var flag = false;
        var tile = Render<OmniSettingsTile>(parameters => parameters
            .Add(component => component.Title, "Nom")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniCheckBox<bool>>(0);
                builder.AddComponentParameter(1, nameof(OmniCheckBox<bool>.Id), "premier");
                builder.AddComponentParameter(2, nameof(OmniCheckBox<bool>.ValueExpression), (System.Linq.Expressions.Expression<Func<bool>>)(() => flag));
                builder.CloseComponent();
                builder.OpenComponent<OmniCheckBox<bool>>(3);
                builder.AddComponentParameter(4, nameof(OmniCheckBox<bool>.Id), "second");
                builder.AddComponentParameter(5, nameof(OmniCheckBox<bool>.ValueExpression), (System.Linq.Expressions.Expression<Func<bool>>)(() => flag));
                builder.CloseComponent();
            }));

        Assert.Contains("for=\"premier\"", tile.Markup, StringComparison.Ordinal);
        tile.Render(parameters => parameters.AddChildContent(builder =>
        {
            builder.OpenComponent<OmniCheckBox<bool>>(0);
            builder.AddComponentParameter(1, nameof(OmniCheckBox<bool>.Id), "premier");
            builder.AddComponentParameter(2, nameof(OmniCheckBox<bool>.ValueExpression), (System.Linq.Expressions.Expression<Func<bool>>)(() => flag));
            builder.CloseComponent();
        }));

        Assert.Contains("for=\"premier\"", tile.Markup, StringComparison.Ordinal);
    }

    // ---- theming ----------------------------------------------------------------------------------

    private static OmniThemePreset BlackHole => OmniThemePresets.All.Single(theme => theme.Name == "Trou noir");

    [Fact]
    public void CanvasField_DrawnRepaintedWithNewTokens_AndKeptWhenTheMotionIsTheSame()
    {
        var module = JSInterop.SetupModule(OmniModules.BlackHole);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<bool>("start", _ => true).SetResult(true);
        var scope = Render<OmniThemeScope>(parameters => parameters.Add(component => component.Preset, BlackHole).AddChildContent("page"));
        scope.WaitForAssertion(() => Assert.Contains("omni-theme-scope--canvas", scope.Find(".omni-theme-scope").ClassList));

        scope.Render(parameters => parameters.Add(component => component.Palette, OmniThemePalettes.All[1]));

        scope.WaitForAssertion(() => Assert.NotEmpty(module.Invocations["refresh"]));
        Assert.Single(module.Invocations["start"]);
    }

    [Fact]
    public void FontWithoutATheme_SetsTheTextFonts()
    {
        var module = JSInterop.SetupModule(OmniModules.Theme);
        module.Mode = JSRuntimeMode.Loose;
        var font = OmniThemeFonts.All[1];

        Render<OmniThemeScope>(parameters => parameters.Add(component => component.Font, font).AddChildContent("page"));

        var light = (IReadOnlyDictionary<string, string>)Assert.Single(module.Invocations["apply"]).Arguments[1]!;
        Assert.Equal(font.Family, light["--omni-font-family"]);
        Assert.Equal(font.Family, light["--omni-heading-font-family"]);
    }

    [Fact]
    public async Task ThemeScope_ReleasedOnALostCircuit_IsQuiet()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.Answers["start"] = true;
        runtime.Module.CallFailures["stop"] = new JSDisconnectedException("perdu");
        runtime.Module.CallFailures["clear"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var scope = Render<OmniThemeScope>(parameters => parameters.Add(component => component.Preset, BlackHole).AddChildContent("page"));

        await scope.Instance.DisposeAsync();

        Assert.Contains("stop", runtime.Module.Calls);
        Assert.Contains("clear", runtime.Module.Calls);
    }

    [Fact]
    public void WindowControls_OfferTheTrayWhenTheHostListens()
    {
        var tray = 0;
        var controls = Render<OmniWindowControls>(parameters => parameters.Add(component => component.OnMinimizeToTray, () => tray++));

        controls.FindAll("button")[0].Click();

        Assert.Equal(1, tray);
    }

    [Fact]
    public void AppearanceSettings_WithTheControlSize_SummarisesIt_AndTheWindowOpenedByTheHostFollowsIt()
    {
        var densities = new List<OmniDensity>();
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.ControlSizeLevelChanged, _ => { })
            .Add(component => component.DensityChanged, density => densities.Add(density))
            .Add(component => component.WindowOpen, true));

        settings.Render(parameters => parameters.Add(component => component.WindowOpen, true));
        settings.FindAll(".omni-select-bar__item").First(item => item.TextContent.Contains("Compact", StringComparison.OrdinalIgnoreCase)).Click();

        Assert.Equal([OmniDensity.Compact], densities);
    }

    [Fact]
    public void AppearanceWindow_ResetsThemePaletteAndFont_AndPicksAPalette()
    {
        OmniThemePreset? preset = OmniThemePresets.All[1];
        OmniThemePalette? palette = OmniThemePalettes.All[2];
        OmniThemeFont? font = OmniThemeFonts.All[2];
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, preset)
            .Add(component => component.PresetChanged, next => preset = next)
            .Add(component => component.Palette, palette)
            .Add(component => component.PaletteChanged, next => palette = next)
            .Add(component => component.Font, font)
            .Add(component => component.FontChanged, next => font = next)
            .Add(component => component.ControlSizeLevelChanged, _ => { }));

        var resets = window.FindAll("button").Where(button => button.TextContent.Trim() == "Défaut").ToList();
        foreach (var reset in resets)
        {
            reset.Click();
        }

        Assert.Null(preset);
        Assert.Null(palette);
        Assert.Null(font);
    }

    [Fact]
    public void ScopeOfAPresetWhoseCanvasIsNone_DrawsNoCanvas()
    {
        var preset = new OmniThemePreset("Sans fond", "Aucun champ", new Dictionary<string, string>(), new Dictionary<string, string>())
        {
            Shape = new Dictionary<string, string> { ["--omni-scope-canvas"] = "none" }
        };

        var scope = Render<OmniThemeScope>(parameters => parameters.Add(component => component.Preset, preset).AddChildContent("page"));

        Assert.Empty(scope.FindAll("canvas"));
        Assert.DoesNotContain("omni-theme-scope--canvas", scope.Find(".omni-theme-scope").ClassList);
    }
}
