using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>The showcase theme state over the package appearance state, with its browser played by test doubles.</summary>
internal static class ShowcaseThemeStateSetup
{
    /// <summary>The showcase state over a package appearance state whose preference lives in <paramref name="browser"/>.</summary>
    public static (ThemeState State, OmniAppearanceState Appearance) Create(ShowcaseThemeJs js, string css, PreferenceBrowser? browser = null)
    {
        var preferences = new OmniPreferences(
            browser ?? new PreferenceBrowser(),
            null,
            new OmniPreferencesOptions { SyncInterval = Timeout.InfiniteTimeSpan },
            NullLogger<OmniPreferences>.Instance,
            TimeProvider.System);
        var appearance = new OmniAppearanceState(preferences, js, NullLogger<OmniAppearanceState>.Instance);
        var http = new HttpClient(new StylesheetResponder(css)) { BaseAddress = new Uri("https://localhost/") };
        return (new ThemeState(new ThemeTokenReader(http), js, appearance), appearance);
    }
}

/// <summary>The showcase theme script: records every call and answers the kept look to <c>omniShowcaseTheme.load</c>.</summary>
internal sealed class ShowcaseThemeJs : ForwardingJSRuntime
{
    public string? Stored { get; set; }

    public List<(string Identifier, object?[] Arguments)> Calls { get; } = [];

    public override ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add((identifier, args ?? []));
        return identifier is "omniShowcaseTheme.load" && Stored is TValue stored
            ? ValueTask.FromResult(stored)
            : ValueTask.FromResult<TValue>(default!);
    }
}
