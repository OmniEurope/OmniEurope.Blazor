using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Demos;
using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

public partial class Customizer : IDisposable
{
    /// <summary>
    /// The gallery entries the preview stacks, in the order of the reference mockup's preview. They
    /// are the gallery's own demonstrations, so the preview and the gallery cannot drift apart.
    /// </summary>
    private static readonly string[] StageKeys =
    [
        "chargement", "boutons", "etats", "badges", "alertes", "surfaces", "reglages-onglets",
        "formulaires", "formulaire-vertical", "notifications", "grilles-maquette", "coquille-application"
    ];

    /// <summary>One section of the preview per entry, each with the density it may take of its own.</summary>
    private IReadOnlyList<StageSection> Sections { get; } = [.. StageKeys.Select(key => new StageSection(DemoCatalog.Resolve(key)))];

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private ThemeState Theme { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private string? Notice { get; set; }

    private OmniAlertSeverity NoticeSeverity { get; set; } = OmniAlertSeverity.Success;

    private bool ForceHover { get; set; }

    private IReadOnlyList<OmniOption<string>> ThemeOptions { get; } =
        [.. OmniThemePresets.All.Select(theme => new OmniOption<string>(theme.Name, theme.Name))];

    private IReadOnlyList<OmniOption<string>> PaletteOptions { get; } =
        [.. OmniThemePalettes.All.Select(palette => new OmniOption<string>(palette.Name, palette.Name))];

    private IReadOnlyList<OmniOption<ThemeMode>> ModeOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<OmniDensity>> DensityOptions { get; set; } = [];

    private IReadOnlyList<OmniOption<string>> SectionDensityOptions { get; set; } = [];

    /// <summary>The pairs measured on the half previewed; the system mode shows the light one.</summary>
    private IReadOnlyList<ContrastResult> Contrasts =>
        ContrastAudit.Measure(Theme.Mode is ThemeMode.Dark ? Theme.Dark : Theme.Light, Theme.Tokens);

    private int Failures => Contrasts.Count(result => !result.Passes);

    protected override void OnInitialized()
    {
        ModeOptions =
        [
            new(ThemeMode.Light, Text["WorkshopModeLight"]),
            new(ThemeMode.Dark, Text["WorkshopModeDark"]),
            new(ThemeMode.System, Text["WorkshopModeSystem"])
        ];
        DensityOptions =
        [
            new(OmniDensity.Compact, Text["WorkshopDensityCompact"]),
            new(OmniDensity.Comfortable, Text["WorkshopDensityComfortable"]),
            new(OmniDensity.Spacious, Text["WorkshopDensitySpacious"])
        ];
        SectionDensityOptions =
        [
            new(string.Empty, Text["WorkshopDensityInherited"]),
            new("compact", Text["WorkshopDensityCompact"]),
            new("comfortable", Text["WorkshopDensityComfortable"]),
            new("spacious", Text["WorkshopDensitySpacious"])
        ];
        Theme.Changed += OnThemeChanged;
    }

    public void Dispose() => Theme.Changed -= OnThemeChanged;

    private static string FieldId(ThemeToken token) => "token" + token.Name.Replace("--", "-", StringComparison.Ordinal);

    /// <summary>Whether a colour input can show the value: it only takes six hexadecimal digits.</summary>
    private static bool IsHex(string value) => value.Length == 7 && value[0] == '#' && value[1..].All(Uri.IsHexDigit);

    private static string FormatRatio(ContrastResult result) =>
        result.Ratio is { } ratio
            ? string.Format(CultureInfo.CurrentCulture, "{0:0.00} / {1:0.0}", ratio, result.Pair.Minimum)
            : "?";

    private string SwatchValue(string name)
    {
        var half = Theme.Mode is ThemeMode.Dark ? Theme.Dark : Theme.Light;
        return half.TryGetValue(name, out var value)
            ? value
            : Theme.Tokens.FirstOrDefault(token => token.Name == name)?.DefaultValue ?? string.Empty;
    }

    private Task SelectThemeAsync(string? name) =>
        OmniThemePresets.All.FirstOrDefault(theme => theme.Name == name) is { } theme
            ? Theme.SelectThemeAsync(theme)
            : Task.CompletedTask;

    private Task SelectPaletteAsync(string? name) =>
        OmniThemePalettes.All.FirstOrDefault(palette => palette.Name == name) is { } palette
            ? Theme.SelectPaletteAsync(palette)
            : Task.CompletedTask;

    private Task SetModeAsync(ThemeMode mode) => Theme.SetModeAsync(mode);

    private Task SetDensityAsync(OmniDensity density) => Theme.SetDensityAsync(density);

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
