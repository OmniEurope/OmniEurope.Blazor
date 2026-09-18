using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class SettingsTabsDemo
{
    private static readonly string[] Keys = ["general", "display", "notifications"];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private SettingsTabsDemoModel Model { get; } = new();

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
}
