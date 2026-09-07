using System.Net;
using System.Text.Json;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Exercises the state behind the theme customizer.
/// </summary>
/// <remarks>
/// Everything the customizer page does goes through this type, and every one of its operations ends
/// in a call to the browser. Rendering the page proves the markup builds; only driving the state
/// directly proves that switching mode carries the palette across, that the export writes the
/// tokens actually moved, and that a half-written entry in the browser store cannot take the site
/// down.
/// </remarks>
public sealed class ShowcaseThemeStateTests
{
    private const string Css = """
        :root {
            --omni-color-accent: #2563eb;
            --omni-color-surface: #ffffff;
            --omni-radius-medium: 0.5rem;
        }
        """;

    [Fact]
    public async Task Initialize_ReadsTheCatalogueAndPushesTheShippedTheme()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, state.Tokens.Count);
        Assert.Empty(state.Overrides);
        Assert.Equal("omniShowcaseTheme.apply", js.Calls[^1].Identifier);
    }

    [Fact]
    public async Task Initialize_ReplaysWhatTheBrowserKept()
    {
        var js = new RecordingJsRuntime
        {
            Stored = JsonSerializer.Serialize(new Dictionary<string, string> { ["--omni-color-accent"] = "#ff0000" })
        };
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("#ff0000", state.Overrides["--omni-color-accent"]);
    }

    [Fact]
    public async Task Initialize_SurvivesAHalfWrittenEntry()
    {
        var js = new RecordingJsRuntime { Stored = "{ this is not json" };
        var state = StateOver(js);

        await state.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Empty(state.Overrides);
        Assert.Equal(3, state.Tokens.Count);
    }

    [Fact]
    public async Task Set_KeepsOnlyTheTokensMovedAwayFromTheShippedValue()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");

        await state.SetAsync(accent, "#ff0000", TestContext.Current.CancellationToken);
        Assert.Equal("#ff0000", state.ValueOf(accent));

        await state.SetAsync(accent, accent.DefaultValue, TestContext.Current.CancellationToken);
        Assert.Empty(state.Overrides);
        Assert.Equal("#2563eb", state.ValueOf(accent));
    }

    [Fact]
    public async Task Set_DropsThePaletteBecauseTheThemeIsNoLongerTheOneItDescribes()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.ApplyAsync(Palette(), TestContext.Current.CancellationToken);
        Assert.NotNull(state.Preset);

        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#123456", TestContext.Current.CancellationToken);

        Assert.Null(state.Preset);
    }

    [Fact]
    public async Task SetMode_CarriesTheAppliedPaletteToItsOtherHalf()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.ApplyAsync(Palette(), TestContext.Current.CancellationToken);
        Assert.Equal("#111111", state.Overrides["--omni-color-surface"]);

        await state.SetModeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);

        Assert.Equal(ThemeMode.Dark, state.Mode);
        Assert.Equal("#000000", state.Overrides["--omni-color-surface"]);
        Assert.Equal("dark", js.Calls[^1].Arguments[1]);
    }

    [Fact]
    public async Task SetMode_LeavesAFreelyEditedThemeAlone()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#abcdef", TestContext.Current.CancellationToken);

        await state.SetModeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);

        Assert.Equal("#abcdef", state.Overrides["--omni-color-accent"]);
    }

    [Fact]
    public async Task Reset_ReturnsToTheShippedTheme()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.ApplyAsync(Palette(), TestContext.Current.CancellationToken);

        await state.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Empty(state.Overrides);
        Assert.Null(state.Preset);
    }

    [Fact]
    public async Task Export_WritesOnlyTheTokensMoved()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        Assert.Contains("No token changed", state.ExportCss(), StringComparison.Ordinal);

        var accent = state.Tokens.Single(token => token.Name == "--omni-color-accent");
        await state.SetAsync(accent, "#ff0000", TestContext.Current.CancellationToken);
        var css = state.ExportCss();

        Assert.Contains(":root {", css, StringComparison.Ordinal);
        Assert.Contains("--omni-color-accent: #ff0000;", css, StringComparison.Ordinal);
        Assert.DoesNotContain("--omni-color-surface", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Export_ScopesTheDarkThemeToItsOwnSelector()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.SetModeAsync(ThemeMode.Dark, TestContext.Current.CancellationToken);
        await state.ApplyAsync(Palette(), TestContext.Current.CancellationToken);

        var css = state.ExportCss();

        Assert.Contains("[data-omni-theme=\"dark\"] {", css, StringComparison.Ordinal);
        Assert.DoesNotContain(":root {", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EveryChange_IsAnnouncedToOpenEditors()
    {
        var js = new RecordingJsRuntime();
        var state = StateOver(js);
        var announced = 0;
        state.Changed += () => announced++;

        await state.InitializeAsync(TestContext.Current.CancellationToken);
        await state.ApplyAsync(Palette(), TestContext.Current.CancellationToken);
        await state.ResetAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, announced);
    }

    private static ThemeState StateOver(RecordingJsRuntime js)
    {
        var http = new HttpClient(new StubHandler(Css)) { BaseAddress = new Uri("https://localhost/") };
        return new ThemeState(new ThemeTokenReader(http), js);
    }

    private static ThemePreset Palette() => new(
        "Essai",
        "Palette de test.",
        new Dictionary<string, string> { ["--omni-color-surface"] = "#111111" },
        new Dictionary<string, string> { ["--omni-color-surface"] = "#000000" },
        "Tests",
        null);

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public string? Stored { get; set; }

        public List<(string Identifier, object?[] Arguments)> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add((identifier, args ?? []));
            return identifier is "omniShowcaseTheme.load" && Stored is TValue stored
                ? ValueTask.FromResult(stored)
                : ValueTask.FromResult<TValue>(default!);
        }
    }
}
