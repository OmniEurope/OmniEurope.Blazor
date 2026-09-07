using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using OmniEurope.Blazor.HybridSmoke.Resources;

namespace OmniEurope.Blazor.HybridSmoke;

public partial class HybridSmoke
{
    // Set by eng/Test-HybridHost.ps1. The CI runner never exposes the WebView2 debugging port, so
    // the host cannot be driven from outside: when this variable is present it clicks its own
    // button, reads its own output and publishes the result on standard output.
    private const string SelfTestVariable = "HYBRIDSMOKE_SELFTEST";

    [Inject]
    private IStringLocalizer<HybridSmokeStrings> Text { get; set; } = default!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private int Count { get; set; }

    private Task IncrementAsync()
    {
        Count++;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await using var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omniInterop.js");
        await module.InvokeVoidAsync(
            "setDocumentMetadata",
            System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName,
            Text["WindowTitle"].Value);

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SelfTestVariable)))
        {
            await RunSelfTestAsync();
        }
    }

    private async Task RunSelfTestAsync()
    {
        try
        {
            await using var selfTest = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./hybrid-smoke.js");
            var result = await selfTest.InvokeAsync<SelfTestResult>("runSelfTest", "#hybrid-action", "#hybrid-count", "1", 10_000);
            SmokeTrace.Write($"selftest language=\"{result.Language}\" title=\"{result.Title}\" observed=\"{result.Observed}\" errors={result.Errors.Length}");
            foreach (var error in result.Errors)
            {
                SmokeTrace.Write($"selftest-error {error}");
            }
        }
        catch (Exception exception)
        {
            SmokeTrace.Write($"selftest-failed {exception.Message}");
        }
        finally
        {
            SmokeTrace.Write("selftest-done");
        }
    }

    private sealed record SelfTestResult(string Language, string Title, string Observed, string[] Errors);
}
