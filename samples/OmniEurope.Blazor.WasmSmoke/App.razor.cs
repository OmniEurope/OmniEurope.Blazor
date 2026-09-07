using Microsoft.JSInterop;

namespace OmniEurope.Blazor.WasmSmoke;

public partial class App
{
    [Inject]
    private IStringLocalizer<Resources.WasmSmokeStrings> Text { get; set; } = default!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    private int _count;

    private Task IncrementAsync()
    {
        _count = (_count + 1) % 11;
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
            Text["PageTitle"].Value);
    }
}
