using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

public partial class Customizer : IDisposable
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private ThemeState Theme { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private string? Notice { get; set; }

    private OmniAlertSeverity NoticeSeverity { get; set; } = OmniAlertSeverity.Success;

    protected override void OnInitialized() => Theme.Changed += OnThemeChanged;

    public void Dispose() => Theme.Changed -= OnThemeChanged;

    private static string FieldId(ThemeToken token) => "token" + token.Name.Replace("--", "-", StringComparison.Ordinal);

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private async Task ApplyAsync(ThemeToken token, ChangeEventArgs args)
    {
        var value = args.Value?.ToString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            await Theme.SetAsync(token, value.Trim()).ConfigureAwait(false);
        }
    }

    private async Task CopyAsync()
    {
        var copied = await Js.InvokeAsync<bool>("omniShowcaseTheme.copy", Theme.ExportCss()).ConfigureAwait(false);
        Report(copied, "CustomizeCopied", "CustomizeCopyFailed");
    }

    private async Task DownloadAsync()
    {
        var saved = await Js.InvokeAsync<bool>("omniShowcaseTheme.download", "omnieurope-theme.css", Theme.ExportCss())
            .ConfigureAwait(false);
        Report(saved, "CustomizeDownloaded", "CustomizeDownloadFailed");
    }

    private void Report(bool succeeded, string successKey, string failureKey)
    {
        NoticeSeverity = succeeded ? OmniAlertSeverity.Success : OmniAlertSeverity.Warning;
        Notice = Text[succeeded ? successKey : failureKey];
    }
}
