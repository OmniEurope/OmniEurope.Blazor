using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using OmniEurope.Blazor.HybridSmoke.Resources;

namespace OmniEurope.Blazor.HybridSmoke;

public partial class HybridSmoke
{
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
    }
}
