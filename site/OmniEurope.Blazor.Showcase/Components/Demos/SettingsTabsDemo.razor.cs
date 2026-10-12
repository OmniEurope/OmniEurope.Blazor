using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SettingsTabsDemo : IDisposable
{

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private SettingsTabsDemoModel Model { get; } = new();

    // The danger zone asks through the package's confirmation dialog, drawn by the demo's own host.
    private OmniOverlayService Overlays { get; } = new();

    private string? Tab { get; set; } = "general";

    private IReadOnlyList<OmniOption<string>> Languages { get; set; } = [];

    private IReadOnlyList<OmniOption<string>> Modes { get; set; } = [];

    protected override void OnInitialized()
    {
        // The language names are written in their own language, as a language picker shows them.
        Languages = [new("fr", Text["SettingsLanguageFrench"]), new("en", Text["SettingsLanguageEnglish"])];
        Modes =
        [
            new("system", Text["SettingsModeSystem"]),
            new("light", Text["SettingsModeLight"]),
            new("dark", Text["SettingsModeDark"])
        ];
    }

    // Demonstration only: nothing is deleted, the delay shows the busy button.
    private async Task DeleteProjectAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1.5));
        Overlays.Notify(Text["SettingsProjectDeleted"], OmniSeverity.Success);
    }

    // Demonstration only: the reset fails on purpose, to show OnConfirmError.
    private async Task ResetHistoryAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(0.8));
        throw new InvalidOperationException(Text["SettingsResetFailed"]);
    }

    private void ReportResetFailure(Exception failure) => Overlays.Notify(failure.Message, OmniSeverity.Danger);

    public void Dispose()
    {
        Overlays.Dispose();
        GC.SuppressFinalize(this);
    }
}
