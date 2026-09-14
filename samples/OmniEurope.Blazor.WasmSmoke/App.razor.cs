using Microsoft.JSInterop;

namespace OmniEurope.Blazor.WasmSmoke;

public partial class App
{
    // Enough rows for both virtualised views to render a window instead of the whole set.
    private const int RowCount = 1000;

    [Inject]
    private IStringLocalizer<Resources.WasmSmokeStrings> Text { get; set; } = default!;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Inject]
    private OmniOverlayService Overlays { get; set; } = default!;

    private IReadOnlyList<SmokeRow> Rows { get; set; } = Array.Empty<SmokeRow>();

    private int _count;

    protected override void OnInitialized()
    {
        Rows = Enumerable.Range(1, RowCount)
            .Select(index => new SmokeRow(Text["RowName", index].Value, index))
            .ToArray();
    }

    private Task IncrementAsync()
    {
        _count = (_count + 1) % 11;
        if (_count == 1)
        {
            // The first click also exercises the overlay service under the strict CSP: a toast and a
            // modal dialog, both rendered by the host from the injected service.
            Overlays.Notify(Text["Notified"], OmniNotificationSeverity.Success);
            Overlays.OpenDialog(new OmniDialogRequest(
                Text["DialogTitle"],
                builder => builder.AddContent(0, Text["DialogBody"].Value)));
        }

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

public sealed record SmokeRow(string Name, int Value);
